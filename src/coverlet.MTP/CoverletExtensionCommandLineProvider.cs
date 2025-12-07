// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.CommandLine;

namespace coverlet.Extension
{

  internal sealed class CoverletExtensionCommandLineProvider : ICommandLineOptionsProvider
  {
    private readonly IExtension _extension;

    public CoverletExtensionCommandLineProvider(IExtension extension)
    {
      _extension = extension;
    }

    public Task<bool> IsEnabledAsync()
    {
      return _extension.IsEnabledAsync();
    }

    public string Uid => _extension.Uid;

    public string Version => _extension.Version;

    public string DisplayName => _extension.DisplayName;

    public string Description => _extension.Description;
    internal static readonly string[] s_sourceArray = new[] { "json", "lcov", "opencover", "cobertura", "teamcity" };

    public IReadOnlyCollection<CommandLineOption> GetCommandLineOptions()
    {
      // Microsoft.Testing.Platform.Extensions.CommandLine does not a default value for LineOptions
      // Default value can be handled in validation

      // see https://learn.microsoft.com/en-us/dotnet/api/system.commandline.argumentarity?view=system-commandline
      //      ExactlyOne            - An arity that must have exactly one value.
      //      MaximumNumberOfValues - Gets the maximum number of values allowed for an argument.
      //      MinimumNumberOfValues - Gets the minimum number of values required for an argument.
      //      OneOrMore             - An arity that must have at least one value.
      //      Zero                  - An arity that does not allow any values.
      //      ZeroOrMore            - An arity that may have multiple values.
      //      ZeroOrOne             - An arity that may have one value, but no more than one.

      return
          [
              new CommandLineOption(name: "formats", description: "Specifies the output formats for the coverage report (e.g., 'json', 'lcov').", arity: ArgumentArity.OneOrMore, isHidden: false),
              new CommandLineOption(name: "exclude", description: "Filter expressions to exclude specific modules and types.", arity: ArgumentArity.OneOrMore, isHidden: false),
              new CommandLineOption(name: "include", description: "Filter expressions to include only specific modules and type", arity: ArgumentArity.OneOrMore, isHidden: false),
              new CommandLineOption(name: "exclude-by-file", description: "Glob patterns specifying source files to exclude.", arity: ArgumentArity.OneOrMore, isHidden: false),
              new CommandLineOption(name: "include-directory", description: "Include directories containing additional assemblies to be instrumented.", arity: ArgumentArity.OneOrMore, isHidden: false),
              new CommandLineOption(name: "exclude-by-attribute", description: "Attributes to exclude from code coverage.", arity: ArgumentArity.OneOrMore, isHidden: false),
              new CommandLineOption(name: "include-test-assembly", description: "Specifies whether to report code coverage of the test assembly.", arity: ArgumentArity.Zero, isHidden: false),
              new CommandLineOption(name: "single-hit", description: "Specifies whether to limit code coverage hit reporting to a single hit for each location", arity: ArgumentArity.Zero, isHidden: false),
              new CommandLineOption(name: "skipautoprops", description: "Neither track nor record auto-implemented properties.", arity: ArgumentArity.Zero, isHidden: false),
              new CommandLineOption(name: "does-not-return-attribute", description: "Attributes that mark methods that do not return", arity: ArgumentArity.ZeroOrMore, isHidden: false),
              new CommandLineOption(name: "exclude-assemblies-without-sources", description: "Specifies behavior of heuristic to ignore assemblies with missing source documents.", arity: ArgumentArity.ZeroOrOne, isHidden: false),
              new CommandLineOption(name: "source-mapping-file", description: "Specifies the path to a SourceRootsMappings file.", arity: ArgumentArity.ZeroOrOne, isHidden: false)
          ];
    }

    public Task<ValidationResult> ValidateOptionArgumentsAsync(CommandLineOption commandOption, string[] arguments)
    {
      if (commandOption.Name == "formats" )
      {
        // When no arguments are provided, validation should pass (default "json" will be used)
        if (arguments.Length == 0 || arguments.Any(string.IsNullOrWhiteSpace))
        {
          return ValidationResult.ValidTask;
        }
        // Validate provided formats
        foreach (string format in arguments)
        {
          if (!s_sourceArray.Contains(format))
          {
            return Task.FromResult(ValidationResult.Invalid($"The value '{format}' is not a valid option for '{commandOption.Name}'."));
          }
        }
        return ValidationResult.ValidTask;
      }
      if (commandOption.Name == "exclude-assemblies-without-sources")
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
}
