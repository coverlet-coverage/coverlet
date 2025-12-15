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
  public const string CoverageOptionName = "coverage";
  public const string CoverageOutputFormatOptionName = "coverage-output-format";
  public const string CoverageOutputOptionName = "coverage-output";
  public const string CoverageIncludeOptionName = "coverage-include";
  public const string CoverageExcludeOptionName = "coverage-exclude";
  public const string CoverageExcludeByFileOptionName = "coverage-exclude-by-file";
  public const string CoverageExcludeByAttributeOptionName = "coverage-exclude-by-attribute";
  public const string CoverageIncludeDirectoryOptionName = "coverage-include-directory";
  public const string CoverageSingleHitOptionName = "coverage-single-hit";
  public const string CoverageIncludeTestAssemblyOptionName = "coverage-include-test-assembly";
  public const string CoverageSkipAutoPropsOptionName = "coverage-skip-auto-props";
  public const string CoverageDoesNotReturnAttributeOptionName = "coverage-does-not-return-attribute";
  public const string CoverageExcludeAssembliesWithoutSourcesOptionName = "coverage-exclude-assemblies-without-sources";

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

      new(CoverageOutputFormatOptionName,
          "Output format(s) for coverage report (json, lcov, opencover, cobertura). Multiple formats can be specified comma-separated.",
          ArgumentArity.ExactlyOne, isHidden: false),

      new(CoverageOutputOptionName,
          "Output path for coverage files",
          ArgumentArity.ExactlyOne, isHidden: false),

      new(CoverageIncludeOptionName,
          "Include assemblies matching filters (e.g., [Assembly]Type)",
          ArgumentArity.OneOrMore, isHidden: false),

      new(CoverageExcludeOptionName,
          "Exclude assemblies matching filters (e.g., [Assembly]Type)",
          ArgumentArity.OneOrMore, isHidden: false),

      new(CoverageExcludeByFileOptionName,
          "Exclude source files matching glob patterns",
          ArgumentArity.OneOrMore, isHidden: false),

      new(CoverageExcludeByAttributeOptionName,
          "Exclude methods/classes decorated with attributes",
          ArgumentArity.OneOrMore, isHidden: false),

      new(CoverageIncludeDirectoryOptionName,
          "Include directories for instrumentation",
          ArgumentArity.OneOrMore, isHidden: false),

      new(CoverageSingleHitOptionName,
          "Limit the number of hits to one for each location",
          ArgumentArity.Zero, isHidden: false),

      new(CoverageIncludeTestAssemblyOptionName,
          "Include test assembly in coverage",
          ArgumentArity.Zero, isHidden: false),

      new(CoverageSkipAutoPropsOptionName,
          "Skip auto-implemented properties",
          ArgumentArity.Zero, isHidden: false),

      new(CoverageDoesNotReturnAttributeOptionName,
          "Attributes that mark methods as not returning",
          ArgumentArity.OneOrMore, isHidden: false),

      new(CoverageExcludeAssembliesWithoutSourcesOptionName,
          "Exclude assemblies without source code",
          ArgumentArity.Zero, isHidden: false),
    ];
  }

  public Task<ValidationResult> ValidateCommandLineOptionsAsync(ICommandLineOptions commandLineOptions)
  {
    // Coverage is opt-in via --coverage flag
    if (!commandLineOptions.IsOptionSet(CoverageOptionName))
    {
      return ValidationResult.ValidTask;
    }

    // Validate output format if specified
    if (commandLineOptions.TryGetOptionArgumentList(CoverageOutputFormatOptionName, out string[]? formats))
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
