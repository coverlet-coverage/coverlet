// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Coverlet.MTP.CommandLine;
using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Logging;

namespace Coverlet.MTP.Configuration;

internal sealed class CoverageConfiguration
{
  private readonly ICommandLineOptions _commandLineOptions;
  private readonly ILogger? _logger;

  // Default exclusions matching coverlet.collector behavior
  // See: https://github.com/coverlet-coverage/coverlet/blob/master/Documentation/VSTestIntegration.md
  private static readonly string[] s_defaultExcludeFilters = new[]
  {
    "[xunit.*]*",
    "[Microsoft.Testing.*]*",
    "[coverlet.*]*"
  };

  private static readonly string[] s_defaultExcludeByAttributes = new[]
  {
    "ExcludeFromCodeCoverage",
    "ExcludeFromCodeCoverageAttribute",
    "GeneratedCodeAttribute",
    "CompilerGeneratedAttribute"
  };

  public CoverageConfiguration(ICommandLineOptions commandLineOptions, ILogger? logger = null)
  {
    _commandLineOptions = commandLineOptions;
    _logger = logger;
  }

  public bool IsCoverageEnabled =>
    _commandLineOptions.IsOptionSet(CoverletOptionNames.Coverage);

  public string[] GetOutputFormats()
  {
    if (_commandLineOptions.TryGetOptionArgumentList(
      CoverletOptionNames.Formats,
      out string[]? formats))
    {
      LogOptionValue(CoverletOptionNames.Formats, formats, isExplicit: true);
      return formats[0].Split(',')
        .Select(f => f.Trim())
        .ToArray();
    }

    string[] defaultFormats = new[] { "json", "cobertura" };
    LogOptionValue(CoverletOptionNames.Formats, defaultFormats, isExplicit: false);
    return defaultFormats;
  }

  public string GetOutputDirectory()
  {
    if (_commandLineOptions.TryGetOptionArgumentList(
      CoverletOptionNames.Output,
      out string[]? outputPath))
    {
      LogOptionValue(CoverletOptionNames.Output, outputPath, isExplicit: true);
      return outputPath[0];
    }

    // Default: TestResults folder next to test assembly
    string testDir = Path.GetDirectoryName(GetTestAssemblyPath()) ?? AppContext.BaseDirectory;
    string defaultPath = Path.Combine(testDir, "TestResults");
    LogOptionValue(CoverletOptionNames.Output, new[] { defaultPath }, isExplicit: false);
    return defaultPath;
  }

  public string[] GetIncludeFilters()
  {
    if (_commandLineOptions.TryGetOptionArgumentList(
      CoverletOptionNames.Include,
      out string[]? filters))
    {
      LogOptionValue(CoverletOptionNames.Include, filters, isExplicit: true);
      return filters;
    }

    return Array.Empty<string>();
  }

  public string[] GetExcludeFilters()
  {
    if (_commandLineOptions.TryGetOptionArgumentList(
      CoverletOptionNames.Exclude,
      out string[]? filters))
    {
      // Merge explicit exclusions with defaults
      string[] merged = s_defaultExcludeFilters.Concat(filters).Distinct().ToArray();
      LogOptionValue(CoverletOptionNames.Exclude, merged, isExplicit: true);
      return merged;
    }

    LogOptionValue(CoverletOptionNames.Exclude, s_defaultExcludeFilters, isExplicit: false);
    return s_defaultExcludeFilters;
  }

  public string[] GetExcludeByFileFilters()
  {
    if (_commandLineOptions.TryGetOptionArgumentList(
      CoverletOptionNames.ExcludeByFile,
      out string[]? filters))
    {
      LogOptionValue(CoverletOptionNames.ExcludeByFile, filters, isExplicit: true);
      return filters;
    }

    return Array.Empty<string>();
  }

  public string[] GetExcludeByAttributeFilters()
  {
    if (_commandLineOptions.TryGetOptionArgumentList(
      CoverletOptionNames.ExcludeByAttribute,
      out string[]? filters))
    {
      // Merge explicit exclusions with defaults
      string[] merged = s_defaultExcludeByAttributes.Concat(filters).Distinct().ToArray();
      LogOptionValue(CoverletOptionNames.ExcludeByAttribute, merged, isExplicit: true);
      return merged;
    }

    LogOptionValue(CoverletOptionNames.ExcludeByAttribute, s_defaultExcludeByAttributes, isExplicit: false);
    return s_defaultExcludeByAttributes;
  }

  public string[] GetIncludeDirectories()
  {
    if (_commandLineOptions.TryGetOptionArgumentList(
      CoverletOptionNames.IncludeDirectory,
      out string[]? directories))
    {
      LogOptionValue(CoverletOptionNames.IncludeDirectory, directories, isExplicit: true);
      return directories;
    }

    return Array.Empty<string>();
  }

  public bool UseSingleHit =>
    _commandLineOptions.IsOptionSet(CoverletOptionNames.SingleHit);

  public bool IncludeTestAssembly =>
    _commandLineOptions.IsOptionSet(CoverletOptionNames.IncludeTestAssembly);

  public bool SkipAutoProps =>
    GetBoolOptionWithDefault(CoverletOptionNames.SkipAutoProps, defaultValue: true);

  public string[] GetDoesNotReturnAttributes()
  {
    if (_commandLineOptions.TryGetOptionArgumentList(
      CoverletOptionNames.DoesNotReturnAttribute,
      out string[]? attributes))
    {
      LogOptionValue(CoverletOptionNames.DoesNotReturnAttribute, attributes, isExplicit: true);
      return attributes;
    }

    return Array.Empty<string>();
  }

  public bool ExcludeAssembliesWithoutSources =>
    GetBoolOptionWithDefault(CoverletOptionNames.ExcludeAssembliesWithoutSources, defaultValue: true);

  public bool UseSourceLink =>
    _commandLineOptions.IsOptionSet(CoverletOptionNames.SourceLink);

  public string? GetMergeWith()
  {
    if (_commandLineOptions.TryGetOptionArgumentList(
      CoverletOptionNames.MergeWith,
      out string[]? mergeWith))
    {
      LogOptionValue(CoverletOptionNames.MergeWith, mergeWith, isExplicit: true);
      return mergeWith[0];
    }

    return null;
  }

  /// <summary>
  /// Gets the test assembly path using multiple fallback strategies.
  /// </summary>
  public static string GetTestAssemblyPath()
  {
    // Try multiple methods to get the test assembly path
    // 1. Entry assembly (most reliable for test scenarios)
    string? path = System.Reflection.Assembly.GetEntryAssembly()?.Location;
    if (!string.IsNullOrEmpty(path) && File.Exists(path))
    {
      return path!;
    }

    // 2. Current process main module
    path = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
    if (!string.IsNullOrEmpty(path) && File.Exists(path))
    {
      return path!;
    }

    // 3. Base directory + first command line argument
    string[] args = Environment.GetCommandLineArgs();
    if (args.Length > 0)
    {
      string fullPath = Path.GetFullPath(args[0]);
      if (File.Exists(fullPath))
      {
        return fullPath;
      }
    }

    // 4. Fallback to base directory
    return AppContext.BaseDirectory;
  }

  /// <summary>
  /// Logs all active coverage configuration settings for diagnostics.
  /// </summary>
  public void LogConfigurationSummary()
  {
    if (_logger is null)
    {
      return;
    }

    _logger.LogInformation("=== Coverlet Coverage Configuration ===");
    _logger.LogInformation($"Test Assembly: {GetTestAssemblyPath()}");
    _logger.LogInformation($"Output Directory: {GetOutputDirectory()}");
    _logger.LogInformation($"Output Formats: {string.Join(", ", GetOutputFormats())}");
    _logger.LogInformation($"Include Filters: {FormatArrayForLog(GetIncludeFilters())}");
    _logger.LogInformation($"Exclude Filters: {FormatArrayForLog(GetExcludeFilters())}");
    _logger.LogInformation($"Exclude by File: {FormatArrayForLog(GetExcludeByFileFilters())}");
    _logger.LogInformation($"Exclude by Attribute: {FormatArrayForLog(GetExcludeByAttributeFilters())}");
    _logger.LogInformation($"Include Directories: {FormatArrayForLog(GetIncludeDirectories())}");
    _logger.LogInformation($"Single Hit: {UseSingleHit}");
    _logger.LogInformation($"Include Test Assembly: {IncludeTestAssembly}");
    _logger.LogInformation($"Skip Auto Properties: {SkipAutoProps}");
    _logger.LogInformation($"Exclude Assemblies Without Sources: {ExcludeAssembliesWithoutSources}");
    _logger.LogInformation($"Use Source Link: {UseSourceLink}");
    _logger.LogInformation($"Merge With: {GetMergeWith() ?? "(none)"}");
    _logger.LogInformation("========================================");
  }

  private bool GetBoolOptionWithDefault(string optionName, bool defaultValue)
  {
    bool isSet = _commandLineOptions.IsOptionSet(optionName);
    if (isSet)
    {
      _logger?.LogDebug($"[Explicitly set] {optionName}: true");
      return true;
    }

    _logger?.LogDebug($"[Default] {optionName}: {defaultValue}");
    return defaultValue;
  }

  private void LogOptionValue(string optionName, string[] values, bool isExplicit)
  {
    string prefix = isExplicit ? "[Explicitly set]" : "[Default]";
    string valuesStr = values.Length == 0 ? "(empty)" : string.Join(", ", values);
    _logger?.LogDebug($"{prefix} {optionName}: {valuesStr}");
  }

  private static string FormatArrayForLog(string[] values)
  {
    return values.Length == 0 ? "(none)" : string.Join(", ", values);
  }
}
