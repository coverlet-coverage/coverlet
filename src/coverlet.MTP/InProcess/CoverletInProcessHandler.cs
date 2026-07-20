// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Diagnostics;
using Coverlet.MTP.EnvironmentVariables;
using Coverlet.Core.Instrumentation;
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
  public string Version => typeof(CoverletExtension).Assembly.GetName().Version?.ToString() ?? "1.0.0";
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

  public CoverletInProcessHandler(ILogger logger)
  {
    _logger = logger;
    // You may want to initialize other fields here as needed
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

      // Pre-create the registry bag before any instrumented assembly is loaded.
      AppDomain.CurrentDomain.SetData(
          ModuleTrackerTemplate.ModuleTrackerRegistryKey,
          new ConcurrentBag<EventHandler>());
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

    FlushCoverageData();

    return Task.CompletedTask;
  }

  /// <summary>
  /// Iterates through all loaded assemblies and calls UnloadModule on any instrumented tracker
  /// classes to flush hit data to files.
  /// </summary>
  private void FlushCoverageData()
  {
    int flushedCount = 0;

    var registeredHandlers = (ConcurrentBag<EventHandler>?)AppDomain.CurrentDomain.GetData(ModuleTrackerTemplate.ModuleTrackerRegistryKey);
    if (registeredHandlers is null)
      return;

    foreach (EventHandler handler in registeredHandlers)
    {
      string assemblyName = handler.Method?.DeclaringType?.Assembly?.GetName().Name ?? "(unknown)";
      try
      {
        _logger.LogDebug($"[Coverlet.MTP.InProcess] Flushing coverage for '{assemblyName}'");
        handler.Invoke(this, EventArgs.Empty);
        flushedCount++;
        _logger.LogDebug($"[Coverlet.MTP.InProcess] Successfully flushed coverage for '{assemblyName}'");
      }
      catch (Exception ex)
      {
        _logger.LogError($"[Coverlet.MTP.InProcess] Failed to flush coverage for '{assemblyName}': {ex}");
        if (_enableExceptionLog)
        {
          throw new InvalidOperationException($"[Coverlet.MTP.InProcess] Failed to flush coverage for '{assemblyName}'", ex);
        }
      }
    }

    _logger.LogDebug($"[Coverlet.MTP.InProcess] Flushed {flushedCount} instrumented assemblies");
  }
}
