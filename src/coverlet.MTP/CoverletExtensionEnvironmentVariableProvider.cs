// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using coverlet.Extension;
using Microsoft.Testing.Platform.Configurations;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.TestHostControllers;
using Microsoft.Testing.Platform.Logging;

namespace Microsoft.Testing.Extensions.Diagnostics;

#pragma warning disable CS9113 // Parameter is unread.
internal sealed class CoverletExtensionEnvironmentVariableProvider(IConfiguration configuration, Platform.CommandLine.ICommandLineOptions commandLineOptions, ILoggerFactory loggerFactory) : ITestHostEnvironmentVariableProvider
#pragma warning restore CS9113 // Parameter is unread.
{
  //private readonly coverlet.Extension.ICommandLineOptions _commandLineOptions = commandLineOptions;
  //private readonly CoverletExtensionConfiguration? _coverletExtensionConfiguration;
  private readonly CoverletExtension _extension = new();
  private readonly IConfiguration _configuration = configuration;
  //private readonly Platform.CommandLine.ICommandLineOptions _commandLineOptions;
  //private readonly Platform.Logging.ILoggerFactory _loggerFactory = loggerFactory;
  //private readonly Platform.CommandLine.ICommandLineOptions? _commandLineOptions;

  //private readonly ILogger _logger = loggerFactory.CreateLogger<CoverletExtensionEnvironmentVariableProvider>();
  public string Uid => nameof(CoverletExtensionEnvironmentVariableProvider);

  public string Version => _extension.Version;

  public string DisplayName => _extension.DisplayName;

  public string Description => _extension.Description;

  public Task<bool> IsEnabledAsync() => Task.FromResult(true);

  public Task UpdateAsync(IEnvironmentVariables environmentVariables)
  {
    //environmentVariables.SetVariable(
    //    new(_CoverletExtensionConfiguration.PipeNameKey, _CoverletExtensionConfiguration.PipeNameValue, false, true));
    //environmentVariables.SetVariable(
    //    new(CoverletExtensionConfiguration.MutexNameSuffix, _CoverletExtensionConfiguration.MutexSuffix, false, true));
    return Task.CompletedTask;
  }

  public Task<ValidationResult> ValidateTestHostEnvironmentVariablesAsync(IReadOnlyEnvironmentVariables environmentVariables)
  {

    // No problem found
    return ValidationResult.ValidTask;
  }
}
