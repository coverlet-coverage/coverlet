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
      new(CoverletOptionNames.Coverage, "Enable code coverage data collection", ArgumentArity.Zero, isHidden: false),

      new(CoverletOptionNames.Formats,
          "Output format(s) for coverage report (json, lcov, opencover, cobertura). Multiple formats can be specified comma-separated.",
          ArgumentArity.OneOrMore, isHidden: false),

      new(CoverletOptionNames.Output,
          "Output path for coverage files",
          ArgumentArity.ExactlyOne, isHidden: false),

      new(CoverletOptionNames.Include,
          "Include assemblies matching filters (e.g., [Assembly]Type)",
          ArgumentArity.OneOrMore, isHidden: false),

      new(CoverletOptionNames.Exclude,
          "Exclude assemblies matching filters (e.g., [Assembly]Type)",
          ArgumentArity.OneOrMore, isHidden: false),

      new(CoverletOptionNames.ExcludeByFile,
          "Exclude source files matching glob patterns",
          ArgumentArity.OneOrMore, isHidden: false),

      new(CoverletOptionNames.ExcludeByAttribute,
          "Exclude methods/classes decorated with attributes",
          ArgumentArity.OneOrMore, isHidden: false),

      new(CoverletOptionNames.IncludeDirectory,
          "Include directories for instrumentation",
          ArgumentArity.OneOrMore, isHidden: false),

      new(CoverletOptionNames.SingleHit,
          "Limit the number of hits to one for each location",
          ArgumentArity.Zero, isHidden: false),

      new(CoverletOptionNames.IncludeTestAssembly,
          "Include test assembly in coverage",
          ArgumentArity.Zero, isHidden: false),

      new(CoverletOptionNames.SkipAutoProps,
          "Skip auto-implemented properties",
          ArgumentArity.Zero, isHidden: false),

      new(CoverletOptionNames.DoesNotReturnAttribute,
          "Attributes that mark methods as not returning",
          ArgumentArity.OneOrMore, isHidden: false),

      new(CoverletOptionNames.ExcludeAssembliesWithoutSources,
          "Exclude assemblies without source code",
          ArgumentArity.Zero, isHidden: false),
    ];
  }

  public Task<ValidationResult> ValidateCommandLineOptionsAsync(ICommandLineOptions commandLineOptions)
  {
    // Coverage is opt-in via --coverlet-coverage flag
    if (!commandLineOptions.IsOptionSet(CoverletOptionNames.Coverage))
    {
      return ValidationResult.ValidTask;
    }

    // Validate output format if specified
    if (commandLineOptions.TryGetOptionArgumentList(CoverletOptionNames.Formats, out string[]? formats))
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
