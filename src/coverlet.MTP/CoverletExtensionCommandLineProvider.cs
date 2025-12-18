// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.CommandLine;

namespace Coverlet.MTP.CommandLine;

internal sealed class CoverletExtensionCommandLineProvider : ICommandLineOptionsProvider
{
  private static readonly string[] s_supportedFormats = ["json", "lcov", "opencover", "cobertura", "teamcity"];

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
  {
    return
    [
      new CommandLineOption(CoverletOptionNames.Coverage, "Enable code coverage collection.", ArgumentArity.Zero, isHidden: false),
      new CommandLineOption(CoverletOptionNames.Formats, "Specifies the output formats for the coverage report (e.g., 'json', 'lcov').", ArgumentArity.OneOrMore, isHidden: false),
      new CommandLineOption(CoverletOptionNames.Exclude, "Filter expressions to exclude specific modules and types.", ArgumentArity.OneOrMore, isHidden: false),
      new CommandLineOption(CoverletOptionNames.Include, "Filter expressions to include only specific modules and type", ArgumentArity.OneOrMore, isHidden: false),
      new CommandLineOption(CoverletOptionNames.ExcludeByFile, "Glob patterns specifying source files to exclude.", ArgumentArity.OneOrMore, isHidden: false),
      new CommandLineOption(CoverletOptionNames.IncludeDirectory, "Include directories containing additional assemblies to be instrumented.", ArgumentArity.OneOrMore, isHidden: false),
      new CommandLineOption(CoverletOptionNames.ExcludeByAttribute, "Attributes to exclude from code coverage.", ArgumentArity.OneOrMore, isHidden: false),
      new CommandLineOption(CoverletOptionNames.IncludeTestAssembly, "Specifies whether to report code coverage of the test assembly.", ArgumentArity.Zero, isHidden: false),
      new CommandLineOption(CoverletOptionNames.SingleHit, "Specifies whether to limit code coverage hit reporting to a single hit for each location", ArgumentArity.Zero, isHidden: false),
      new CommandLineOption(CoverletOptionNames.SkipAutoProps, "Neither track nor record auto-implemented properties.", ArgumentArity.Zero, isHidden: false),
      new CommandLineOption(CoverletOptionNames.DoesNotReturnAttribute, "Attributes that mark methods that do not return", ArgumentArity.ZeroOrMore, isHidden: false),
      new CommandLineOption(CoverletOptionNames.ExcludeAssembliesWithoutSources, "Specifies behavior of heuristic to ignore assemblies with missing source documents.", ArgumentArity.ZeroOrOne, isHidden: false),
      new CommandLineOption(CoverletOptionNames.SourceMappingFile, "Specifies the path to a SourceRootsMappings file.", ArgumentArity.ZeroOrOne, isHidden: false)
    ];
  }

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
        if (!s_supportedFormats.Contains(format))
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

