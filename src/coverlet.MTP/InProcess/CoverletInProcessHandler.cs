// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Reflection;
using Coverlet.Core.Instrumentation;
using Coverlet.MTP.Constants;
using Microsoft.Testing.Platform.Extensions.TestHost;
using Microsoft.Testing.Platform.Logging;
using Microsoft.Testing.Platform.Services;

namespace Coverlet.MTP.InProcess;

/// <summary>
/// In-process handler that runs inside the test host process.
/// Responsible for flushing coverage hit data when tests complete.
/// Similar to CoverletInProcDataCollector from coverlet.collector.
/// </summary>
internal sealed class CoverletInProcessHandler : ITestSessionLifetimeHandler
{
  private readonly ILogger _logger;
  private readonly bool _coverageEnabled;
  private readonly string? _coverageIdentifier;
  private readonly bool _enableExceptionLog;

  public string Uid => "Coverlet.MTP.InProcess";
  public string Version => "1.0.0";
  public string DisplayName => "Coverlet In-Process Handler";
  public string Description => "Flushes coverage hit data when test session ends";

  public CoverletInProcessHandler(ILoggerFactory loggerFactory)
  {
    _logger = loggerFactory.CreateLogger<CoverletInProcessHandler>();

    // Handle debugger attachment (before reading other env vars)
    if (Environment.GetEnvironmentVariable(CoverletMtpEnvironmentVariables.InProcessDebug) == "1")
    {
      Debugger.Launch();
      Debugger.Break();
    }

    // Read environment variables set by CoverletExtensionCollector (out-of-process)
    _coverageEnabled = Environment.GetEnvironmentVariable(CoverletMtpEnvironmentVariables.CoverageEnabled) == "true";
    _coverageIdentifier = Environment.GetEnvironmentVariable(CoverletMtpEnvironmentVariables.CoverageIdentifier);
    _enableExceptionLog = Environment.GetEnvironmentVariable(CoverletMtpEnvironmentVariables.InProcessExceptionLog) == "1";

    _logger.LogDebug($"[Coverlet.MTP.InProcess] Initialized - CoverageEnabled={_coverageEnabled}, Identifier={_coverageIdentifier ?? "(null)"}");
  }

  public Task<bool> IsEnabledAsync() => Task.FromResult(_coverageEnabled);

  /// <summary>
  /// Called when test session starts inside the test host.
  /// </summary>
  Task ITestSessionLifetimeHandler.OnTestSessionStartingAsync(ITestSessionContext testSessionContext)
  {
    if (_coverageEnabled)
    {
      _logger.LogDebug($"[Coverlet.MTP.InProcess] Test session starting: {testSessionContext.SessionUid}");
    }
    return Task.CompletedTask;
  }

  /// <summary>
  /// Called when test session ends inside the test host.
  /// Flushes all coverage hit data from instrumented modules.
  /// </summary>
  Task ITestSessionLifetimeHandler.OnTestSessionFinishingAsync(ITestSessionContext testSessionContext)
  {
    if (!_coverageEnabled)
    {
      return Task.CompletedTask;
    }

    _logger.LogDebug($"[Coverlet.MTP.InProcess] Test session finishing: {testSessionContext.SessionUid}, flushing coverage data");

    try
    {
      FlushCoverageData();
    }
    catch (Exception ex)
    {
      if (_enableExceptionLog)
      {
        _logger.LogError($"[Coverlet.MTP.InProcess] Failed to flush coverage data: {ex}");
      }
    }

    return Task.CompletedTask;
  }

  /// <summary>
  /// Iterates through all loaded assemblies and calls UnloadModule on any
  /// instrumented tracker classes to flush hit data to files.
  /// </summary>
  private void FlushCoverageData()
  {
    int flushedCount = 0;

    foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
    {
      Type? trackerType = GetInstrumentationTrackerType(assembly);
      if (trackerType == null)
        continue;

      try
      {
        _logger.LogDebug($"[Coverlet.MTP.InProcess] Flushing coverage for '{assembly.GetName().Name}'");

        // Call ModuleTrackerTemplate.UnloadModule to flush hit data
        MethodInfo? unloadMethod = trackerType.GetMethod(
          nameof(ModuleTrackerTemplate.UnloadModule),
          [typeof(object), typeof(EventArgs)]);

        if (unloadMethod != null)
        {
          unloadMethod.Invoke(null, [this, EventArgs.Empty]);

          // Prevent double-flush on process exit
          FieldInfo? flushField = trackerType.GetField("FlushHitFile", BindingFlags.Static | BindingFlags.Public);
          flushField?.SetValue(null, false);

          flushedCount++;
          _logger.LogDebug($"[Coverlet.MTP.InProcess] Successfully flushed coverage for '{assembly.GetName().Name}'");
        }
      }
      catch (Exception ex)
      {
        if (_enableExceptionLog)
        {
          _logger.LogWarning($"[Coverlet.MTP.InProcess] Failed to flush coverage for '{assembly.GetName().Name}': {ex.Message}");
        }
      }
    }

    _logger.LogDebug($"[Coverlet.MTP.InProcess] Flushed {flushedCount} instrumented assemblies");
  }

  /// <summary>
  /// Finds the injected tracker type in an assembly.
  /// Tracker types are in namespace "Coverlet.Core.Instrumentation.Tracker"
  /// with name pattern "{AssemblyName}_*".
  /// </summary>
  private Type? GetInstrumentationTrackerType(Assembly assembly)
  {
    try
    {
      string? assemblyName = assembly.GetName().Name;
      if (string.IsNullOrEmpty(assemblyName))
        return null;

      foreach (Type type in assembly.GetTypes())
      {
        if (type.Namespace == "Coverlet.Core.Instrumentation.Tracker" &&
            type.Name.StartsWith(assemblyName + "_", StringComparison.Ordinal))
        {
          return type;
        }
      }

      return null;
    }
    catch (ReflectionTypeLoadException ex)
    {
      if (_enableExceptionLog)
      {
        string errors = string.Join(", ", ex.LoaderExceptions?.Select(e => e?.Message) ?? []);
        _logger.LogDebug($"[Coverlet.MTP.InProcess] ReflectionTypeLoadException for '{assembly.GetName().Name}': {errors}");
      }
      return null;
    }
    catch
    {
      // Silently ignore assemblies that can't be inspected
      return null;
    }
  }

}
