// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Coverlet.MTP.CommandLine;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.CommandLine;

namespace coverlet.MTP.CommandLine;

internal sealed class CoverletExtensionCommandLineProvider : ICommandLineOptionsProvider
{
  private readonly IExtension _extension;

  public CoverletExtensionCommandLineProvider(IExtension extension)
  {
    _extension = extension;
  }

  public string Uid => _extension.Uid;
  public string Version => _extension.Version;
  public string DisplayName => _extension.DisplayName;
  public string Description => _extension.Description;

  public Task<bool> IsEnabledAsync() => Task.FromResult(true);

  public IReadOnlyCollection<CommandLineOption> GetCommandLineOptions()
    => CoverletCommandLineOptionDefinitions.GetAllOptions();

  public Task<ValidationResult> ValidateOptionArgumentsAsync(CommandLineOption commandOption, string[] arguments)
  {
    if (commandOption.Name == CoverletOptionNames.Formats)
    {
      if (arguments.Length == 0 || arguments.Any(string.IsNullOrWhiteSpace))
      {
        return ValidationResult.ValidTask;
      }

      foreach (string format in arguments)
      {
        if (!CoverletCommandLineOptionDefinitions.SupportedFormats.Contains(format))
        {
          return Task.FromResult(ValidationResult.Invalid($"The value '{format}' is not a valid option for '{commandOption.Name}'."));
        }
      }
      return ValidationResult.ValidTask;
    }

    if (commandOption.Name == CoverletOptionNames.ExcludeAssembliesWithoutSources)
    {
      if (arguments.Length == 0)
      {
        return Task.FromResult(ValidationResult.Invalid($"At least one value must be specified for '{commandOption.Name}'."));
      }
      if (arguments.Length > 1)
      {
        return Task.FromResult(ValidationResult.Invalid($"Only one value is allowed for '{commandOption.Name}'."));
      }
      if (!arguments[0].Contains("MissingAll") && !arguments[0].Contains("MissingAny") && !arguments[0].Contains("None"))
      {
        return Task.FromResult(ValidationResult.Invalid($"The value '{arguments[0]}' is not a valid option for '{commandOption.Name}'."));
      }
    }
    return ValidationResult.ValidTask;
  }

  public Task<ValidationResult> ValidateCommandLineOptionsAsync(Microsoft.Testing.Platform.CommandLine.ICommandLineOptions commandLineOptions)
  {
    return ValidationResult.ValidTask;
  }
}

