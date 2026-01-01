// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using coverlet.Extension;
using Coverlet.MTP.Diagnostics;
using Microsoft.Testing.Platform.Configurations;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.TestHostControllers;
using Microsoft.Testing.Platform.Logging;

namespace Coverlet.MTP;

internal sealed class CoverletExtensionEnvironmentVariableProvider : ITestHostEnvironmentVariableProvider
{
  private readonly ILogger _logger;

  public CoverletExtensionEnvironmentVariableProvider(
    IConfiguration configuration,
    Microsoft.Testing.Platform.CommandLine.ICommandLineOptions commandLineOptions,
    ILoggerFactory loggerFactory)
  {
    _logger = loggerFactory.CreateLogger<CoverletExtensionEnvironmentVariableProvider>();

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
