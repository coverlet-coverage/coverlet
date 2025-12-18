// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.CommandLine;

namespace Coverlet.MTP.CommandLine;

/// <summary>
/// Provides command-line options for Coverlet code coverage.
/// Compatible with Microsoft.Testing.Platform V2.0.2
/// </summary>
internal sealed class CoverletCommandLineOptionsProvider : ICommandLineOptionsProvider
{
  /// <summary>
  /// The command-line option name that enables coverage collection.
  /// </summary>
  public const string CoverageOptionName = "coverlet-coverage";

  public const string FormatsOptionName = "formats";
  public const string ExcludeOptionName = "exclude";
  public const string IncludeOptionName = "include";
  public const string ExcludeByFileOptionName = "exclude-by-file";
  public const string IncludeDirectoryOptionName = "include-directory";
  public const string ExcludeByAttributeOptionName = "exclude-by-attribute";
  public const string IncludeTestAssemblyOptionName = "include-test-assembly";
  public const string SingleHitOptionName = "single-hit";
  public const string SkipAutoPropsOptionName = "skipautoprops";
  public const string DoesNotReturnAttributeOptionName = "does-not-return-attribute";
  public const string ExcludeAssembliesWithoutSourcesOptionName = "exclude-assemblies-without-sources";
  public const string SourceMappingFileOptionName = "source-mapping-file";

  // IExtension members (V2.0.2 still requires these)
  public string Uid => nameof(CoverletCommandLineOptionsProvider);

  public string Version => "1.0.0";

  public string DisplayName => "Coverlet Code Coverage";

  public string Description => "Enables code coverage collection using Coverlet instrumentation";

  public Task<bool> IsEnabledAsync() => Task.FromResult(true);

  // ICommandLineOptionsProvider members
  public IReadOnlyCollection<CommandLineOption> GetCommandLineOptions()
  {
    return
    [
      new(CoverageOptionName, "Enable code coverage data collection", ArgumentArity.Zero, isHidden: false),

      new(FormatsOptionName,
          "Output format(s) for coverage report (json, lcov, opencover, cobertura). Multiple formats can be specified comma-separated.",
          ArgumentArity.ExactlyOne, isHidden: false),

      //new(OutputOptionName,
      //    "Output path for coverage files",
      //    ArgumentArity.ExactlyOne, isHidden: false),

      new(IncludeOptionName,
          "Include assemblies matching filters (e.g., [Assembly]Type)",
          ArgumentArity.OneOrMore, isHidden: false),

      new(ExcludeOptionName,
          "Exclude assemblies matching filters (e.g., [Assembly]Type)",
          ArgumentArity.OneOrMore, isHidden: false),

      new(ExcludeByFileOptionName,
          "Exclude source files matching glob patterns",
          ArgumentArity.OneOrMore, isHidden: false),

      new(ExcludeByAttributeOptionName,
          "Exclude methods/classes decorated with attributes",
          ArgumentArity.OneOrMore, isHidden: false),

      new(IncludeDirectoryOptionName,
          "Include directories for instrumentation",
          ArgumentArity.OneOrMore, isHidden: false),

      new(SingleHitOptionName,
          "Limit the number of hits to one for each location",
          ArgumentArity.Zero, isHidden: false),

      new(IncludeTestAssemblyOptionName,
          "Include test assembly in coverage",
          ArgumentArity.Zero, isHidden: false),

      new(SkipAutoPropsOptionName,
          "Skip auto-implemented properties",
          ArgumentArity.Zero, isHidden: false),

      new(DoesNotReturnAttributeOptionName,
          "Attributes that mark methods as not returning",
          ArgumentArity.OneOrMore, isHidden: false),

      new(ExcludeAssembliesWithoutSourcesOptionName,
          "Exclude assemblies without source code",
          ArgumentArity.Zero, isHidden: false),
    ];
  }

  public Task<ValidationResult> ValidateCommandLineOptionsAsync(ICommandLineOptions commandLineOptions)
  {
    // Coverage is opt-in via --coverlet-coverage flag
    if (!commandLineOptions.IsOptionSet(CoverageOptionName))
    {
      return ValidationResult.ValidTask;
    }

    // Validate output format if specified
    if (commandLineOptions.TryGetOptionArgumentList(FormatsOptionName, out string[]? formats))
    {
      string[] validFormats = ["json", "lcov", "opencover", "cobertura"];
      string[] providedFormats = formats[0].Split(',');

      foreach (string format in providedFormats)
      {
        if (!validFormats.Contains(format.Trim(), StringComparer.OrdinalIgnoreCase))
        {
          return ValidationResult.InvalidTask($"Invalid coverage output format: '{format}'. Valid formats: {string.Join(", ", validFormats)}");
        }
      }
    }

    return ValidationResult.ValidTask;
  }

  public Task<ValidationResult> ValidateOptionArgumentsAsync(CommandLineOption commandOption, string[] arguments)
  {
    return ValidationResult.ValidTask;
  }
}
