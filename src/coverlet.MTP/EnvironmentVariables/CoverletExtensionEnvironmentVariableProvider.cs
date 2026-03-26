// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Coverlet.MTP.CommandLine;
using Coverlet.MTP.Diagnostics;
using Coverlet.MTP;
using Microsoft.Testing.Platform.Configurations;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.TestHostControllers;
using Microsoft.Testing.Platform.Logging;
using Coverlet.MTP.EnvironmentVariables;

namespace coverlet.MTP.EnvironmentVariables;

internal sealed class CoverletExtensionEnvironmentVariableProvider : ITestHostEnvironmentVariableProvider
{
  private readonly ILogger _logger;
  private readonly Microsoft.Testing.Platform.CommandLine.ICommandLineOptions _commandLineOptions;

  public CoverletExtensionEnvironmentVariableProvider(
    IConfiguration configuration,
    Microsoft.Testing.Platform.CommandLine.ICommandLineOptions commandLineOptions,
    ILoggerFactory loggerFactory)
  {
    _logger = loggerFactory.CreateLogger<CoverletExtensionEnvironmentVariableProvider>();
    _commandLineOptions = commandLineOptions;

    // Handle debugger attachment for this component
    DebugHelper.HandleDebuggerAttachment(nameof(CoverletExtensionEnvironmentVariableProvider));
  }

  public string Uid => nameof(CoverletExtensionEnvironmentVariableProvider);
  public string Version => typeof(CoverletExtension).Assembly.GetName().Version?.ToString() ?? "1.0.0";
  public string DisplayName => "Coverlet Environment Variable Provider";
  public string Description => "Provides environment variables for Coverlet coverage collection";

  public Task<bool> IsEnabledAsync() => Task.FromResult(true);

  public Task UpdateAsync(IEnvironmentVariables environmentVariables)
  {
    // Check if --coverlet flag was provided
    bool coverageEnabled = _commandLineOptions.IsOptionSet(CoverletOptionNames.Coverage);

    if (coverageEnabled)
    {
      // Tell the test host that coverage is enabled
      environmentVariables.SetVariable(
        new EnvironmentVariable(
          CoverletMtpEnvironmentVariables.CoverageEnabled,
          "true",
          isSecret: false,
          isLocked: true));

      _logger.LogDebug($"[CoverletEnvVarProvider] Set {CoverletMtpEnvironmentVariables.CoverageEnabled}=true");
    }

    // Propagate tracker log setting to test host process
    if (DebugHelper.IsTrackerLogEnabled)
    {
      environmentVariables.SetVariable(
        new EnvironmentVariable(
          CoverletMtpDebugConstants.EnableTrackerLog,
          "1",
          isSecret: false,
          isLocked: false));

      _logger.LogDebug("Enabled tracker logging in test host");
    }

    // Propagate exception logging setting
    if (DebugHelper.IsExceptionLogEnabled)
    {
      environmentVariables.SetVariable(
        new EnvironmentVariable(
          CoverletMtpDebugConstants.ExceptionLogEnabled,
          "1",
          isSecret: false,
          isLocked: false));
    }

    return Task.CompletedTask;
  }

  public Task<ValidationResult> ValidateTestHostEnvironmentVariablesAsync(
    IReadOnlyEnvironmentVariables environmentVariables)
  {
    return ValidationResult.ValidTask;
  }
}
