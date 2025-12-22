// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Testing.Platform.Extensions.CommandLine;

namespace Coverlet.MTP.CommandLine;

/// <summary>
/// Centralized definitions for all Coverlet command line options.
/// This ensures consistency and avoids duplication across providers.
/// </summary>
internal static class CoverletCommandLineOptionDefinitions
{
  public static readonly string[] SupportedFormats = ["json", "lcov", "opencover", "cobertura", "teamcity"];

  public static IReadOnlyCollection<CommandLineOption> GetAllOptions()
  {
    return
    [
      new CommandLineOption(CoverletOptionNames.Coverage, "Enable code coverage data collection.", ArgumentArity.Zero, isHidden: false),
      new CommandLineOption(CoverletOptionNames.Formats, "Output format(s) for coverage report (json, lcov, opencover, cobertura).", ArgumentArity.OneOrMore, isHidden: false),
      new CommandLineOption(CoverletOptionNames.Output, "Output path for coverage files.", ArgumentArity.ExactlyOne, isHidden: false),
      new CommandLineOption(CoverletOptionNames.Include, "Include assemblies matching filters (e.g., [Assembly]Type).", ArgumentArity.OneOrMore, isHidden: false),
      new CommandLineOption(CoverletOptionNames.IncludeDirectory, "Include additional directories for instrumentation.", ArgumentArity.OneOrMore, isHidden: false),
      new CommandLineOption(CoverletOptionNames.Exclude, "Exclude assemblies matching filters (e.g., [Assembly]Type).", ArgumentArity.OneOrMore, isHidden: false),
      new CommandLineOption(CoverletOptionNames.ExcludeByFile, "Exclude source files matching glob patterns.", ArgumentArity.OneOrMore, isHidden: false),
      new CommandLineOption(CoverletOptionNames.ExcludeByAttribute, "Exclude methods/classes decorated with attributes.", ArgumentArity.OneOrMore, isHidden: false),
      new CommandLineOption(CoverletOptionNames.IncludeTestAssembly, "Include test assembly in coverage.", ArgumentArity.Zero, isHidden: false),
      new CommandLineOption(CoverletOptionNames.SingleHit, "Limit the number of hits to one for each location.", ArgumentArity.Zero, isHidden: false),
      new CommandLineOption(CoverletOptionNames.SkipAutoProps, "Skip auto-implemented properties.", ArgumentArity.Zero, isHidden: false),
      new CommandLineOption(CoverletOptionNames.DoesNotReturnAttribute, "Attributes that mark methods as not returning.", ArgumentArity.ZeroOrMore, isHidden: false),
      new CommandLineOption(CoverletOptionNames.ExcludeAssembliesWithoutSources, "Exclude assemblies without source code.", ArgumentArity.ZeroOrOne, isHidden: false),
      new CommandLineOption(CoverletOptionNames.SourceMappingFile, "Output path for SourceRootsMappings file.", ArgumentArity.ZeroOrOne, isHidden: false)
    ];
  }
}
