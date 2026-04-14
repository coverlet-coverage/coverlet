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
  private readonly CoverletMTPSettings? _configFileSettings;

  // Default exclusions for user convenience, to avoid common noise in coverage reports. These are merged with any user-specified exclusions.
  private static readonly string[] s_defaultExcludeFilters =
  [
    "[coverlet.*]*",
    "[xunit.*]*",
    "[NUnit3.*]*",
    "[nunit.*]*",
    "[Microsoft.Testing.*]*",
    "[Microsoft.Testplatform.*]*",
    "[Microsoft.VisualStudio.TestPlatform.*]*"

  ];

  private static readonly string[] s_defaultExcludeByAttributes =
  [
    "ExcludeFromCodeCoverage",
    "ExcludeFromCodeCoverageAttribute",
    "GeneratedCodeAttribute",
    "CompilerGeneratedAttribute"
  ];

  /// <summary>
  /// Initializes a new instance of CoverageConfiguration using only command-line options.
  /// </summary>
  public CoverageConfiguration(ICommandLineOptions commandLineOptions, ILogger? logger = null)
    : this(commandLineOptions, configFileSettings: null, logger)
  {
  }

  /// <summary>
  /// Initializes a new instance of CoverageConfiguration with both command-line options and config file settings.
  /// Configuration precedence (highest to lowest):
  /// 1. Explicit command-line options
  /// 2. Configuration file settings (coverlet.mtp.appsettings.json)
  /// 3. Built-in defaults
  /// </summary>
  public CoverageConfiguration(ICommandLineOptions commandLineOptions, CoverletMTPSettings? configFileSettings, ILogger? logger = null)
  {
    _commandLineOptions = commandLineOptions;
    _configFileSettings = configFileSettings;
    _logger = logger;
  }

  public bool IsCoverageEnabled =>
    _commandLineOptions.IsOptionSet(CoverletOptionNames.Coverage);

  public string[] GetOutputFormats()
  {
    // Priority 1: Explicit command-line option
    if (_commandLineOptions.TryGetOptionArgumentList(
      CoverletOptionNames.Formats,
      out string[]? formats))
    {
      LogOptionValue(CoverletOptionNames.Formats, formats, isExplicit: true);
      return formats;
    }

    // Priority 2: Configuration file setting
    if (_configFileSettings?.ReportFormats is { Length: > 0 } configFormats)
    {
      LogOptionValue(CoverletOptionNames.Formats, configFormats, isExplicit: false, source: "config file");
      return configFormats;
    }

    // Priority 3: Built-in default
    string[] defaultFormats = ["json", "cobertura"];
    LogOptionValue(CoverletOptionNames.Formats, defaultFormats, isExplicit: false);
    return defaultFormats;
  }

  public string[] GetIncludeFilters()
  {
    // Priority 1: Explicit command-line option
    if (_commandLineOptions.TryGetOptionArgumentList(
      CoverletOptionNames.Include,
      out string[]? filters))
    {
      LogOptionValue(CoverletOptionNames.Include, filters, isExplicit: true);
      return filters;
    }

    // Priority 2: Configuration file setting
    if (_configFileSettings?.IncludeFilters is { Length: > 0 } configFilters)
    {
      LogOptionValue(CoverletOptionNames.Include, configFilters, isExplicit: false, source: "config file");
      return configFilters;
    }

    return [];
  }

  public string[] GetExcludeFilters()
  {
    // Priority 1: Explicit command-line option
    if (_commandLineOptions.TryGetOptionArgumentList(
      CoverletOptionNames.Exclude,
      out string[]? filters))
    {
      // Merge explicit exclusions with defaults
      string[] merged = [.. s_defaultExcludeFilters.Concat(filters).Distinct()];
      LogOptionValue(CoverletOptionNames.Exclude, merged, isExplicit: true);
      return merged;
    }

    // Priority 2: Configuration file setting
    // Config file filters already include [coverlet.*]* prepended by CoverletMTPSettingsParser
    if (_configFileSettings?.ExcludeFilters is { Length: > 0 } configFilters)
    {
      LogOptionValue(CoverletOptionNames.Exclude, configFilters, isExplicit: false, source: "config file");
      return configFilters;
    }

    // Priority 3: Built-in defaults
    LogOptionValue(CoverletOptionNames.Exclude, s_defaultExcludeFilters, isExplicit: false);
    return s_defaultExcludeFilters;
  }

  public string[] GetExcludeByFileFilters()
  {
    // Priority 1: Explicit command-line option
    if (_commandLineOptions.TryGetOptionArgumentList(
      CoverletOptionNames.ExcludeByFile,
      out string[]? filters))
    {
      LogOptionValue(CoverletOptionNames.ExcludeByFile, filters, isExplicit: true);
      return filters;
    }

    // Priority 2: Configuration file setting
    if (_configFileSettings?.ExcludeSourceFiles is { Length: > 0 } configFilters)
    {
      LogOptionValue(CoverletOptionNames.ExcludeByFile, configFilters, isExplicit: false, source: "config file");
      return configFilters;
    }

    return [];
  }

  public string[] GetExcludeByAttributeFilters()
  {
    // Priority 1: Explicit command-line option
    if (_commandLineOptions.TryGetOptionArgumentList(
      CoverletOptionNames.ExcludeByAttribute,
      out string[]? filters))
    {
      // Merge explicit exclusions with defaults
      string[] merged = [.. s_defaultExcludeByAttributes.Concat(filters).Distinct()];
      LogOptionValue(CoverletOptionNames.ExcludeByAttribute, merged, isExplicit: true);
      return merged;
    }

    // Priority 2: Configuration file setting
    // Check if explicitly set in config file (even if empty)
    if (_configFileSettings?.ExcludeByAttributeExplicitlySet == true)
    {
      // Config file explicitly set this value - use it without merging defaults
      // This allows empty string in config to suppress defaults
      string[] configFilters = _configFileSettings.ExcludeAttributes;
      LogOptionValue(CoverletOptionNames.ExcludeByAttribute, configFilters, isExplicit: false, source: "config file");
      return configFilters;
    }

    // Priority 3: Built-in defaults
    LogOptionValue(CoverletOptionNames.ExcludeByAttribute, s_defaultExcludeByAttributes, isExplicit: false);
    return s_defaultExcludeByAttributes;
  }

  public string GetExcludeAssembliesWithoutSources()
  {
    // Priority 1: Explicit command-line option
    if (_commandLineOptions.TryGetOptionArgumentList(
      CoverletOptionNames.ExcludeAssembliesWithoutSources,
      out string[]? options))
    {
      LogOptionValue(CoverletOptionNames.ExcludeAssembliesWithoutSources, options, isExplicit: true);
      return options[0];
    }

    // Priority 2: Configuration file setting
    if (!string.IsNullOrWhiteSpace(_configFileSettings?.ExcludeAssembliesWithoutSources))
    {
      LogOptionValue(CoverletOptionNames.ExcludeAssembliesWithoutSources, [_configFileSettings!.ExcludeAssembliesWithoutSources], isExplicit: false, source: "config file");
      return _configFileSettings.ExcludeAssembliesWithoutSources;
    }

    return "None";
  }

  public string[] GetIncludeDirectories()
  {
    // Priority 1: Explicit command-line option
    if (_commandLineOptions.TryGetOptionArgumentList(
      CoverletOptionNames.IncludeDirectory,
      out string[]? directories))
    {
      LogOptionValue(CoverletOptionNames.IncludeDirectory, directories, isExplicit: true);
      return directories;
    }

    // Priority 2: Configuration file setting
    if (_configFileSettings?.IncludeDirectories is { Length: > 0 } configDirs)
    {
      LogOptionValue(CoverletOptionNames.IncludeDirectory, configDirs, isExplicit: false, source: "config file");
      return configDirs;
    }

    return [];
  }

  public bool UseSingleHit =>
    GetBoolOptionWithDefault(CoverletOptionNames.SingleHit, _configFileSettings?.SingleHit ?? false);

  public bool IncludeTestAssembly =>
    GetBoolOptionWithDefault(CoverletOptionNames.IncludeTestAssembly, _configFileSettings?.IncludeTestAssembly ?? false);

  public bool SkipAutoProps =>
    GetBoolOptionWithDefault(CoverletOptionNames.SkipAutoProps, _configFileSettings?.SkipAutoProps ?? false);

  public string[] GetDoesNotReturnAttributes()
  {
    // Priority 1: Explicit command-line option
    if (_commandLineOptions.TryGetOptionArgumentList(
      CoverletOptionNames.DoesNotReturnAttribute,
      out string[]? attributes))
    {
      LogOptionValue(CoverletOptionNames.DoesNotReturnAttribute, attributes, isExplicit: true);
      return attributes;
    }

    // Priority 2: Configuration file setting
    if (_configFileSettings?.DoesNotReturnAttributes is { Length: > 0 } configAttrs)
    {
      LogOptionValue(CoverletOptionNames.DoesNotReturnAttribute, configAttrs, isExplicit: false, source: "config file");
      return configAttrs;
    }

    return [];
  }

  /// <summary>
  /// Gets the file prefix for coverage report filenames.
  /// Returns null if the prefix is empty or whitespace.
  /// </summary>
  public string? GetFilePrefix()
  {
    // Priority 1: Explicit command-line option
    if (_commandLineOptions.TryGetOptionArgumentList(
      CoverletOptionNames.FilePrefix,
      out string[]? prefix) && prefix.Length > 0)
    {
      string? rawPrefix = prefix[0];
      string? trimmedPrefix = rawPrefix?.Trim();
      if (string.IsNullOrWhiteSpace(trimmedPrefix))
      {
        return null;
      }

      LogOptionValue(CoverletOptionNames.FilePrefix, [trimmedPrefix!], isExplicit: true);
      return trimmedPrefix;
    }

    // Note: File prefix is not typically set via config file
    return null;
  }

  /// <summary>
  /// Gets whether to generate deterministic reports.
  /// </summary>
  public bool DeterministicReport =>
    GetBoolOptionWithDefault("coverlet-deterministic-report", _configFileSettings?.DeterministicReport ?? false);

  /// <summary>
  /// Gets the test assembly path.
  /// </summary>
  public static string GetTestAssemblyPath()
  {
    // In test scenarios, the entry assembly is always the test assembly
    string? path = System.Reflection.Assembly.GetEntryAssembly()?.Location;

    return string.IsNullOrEmpty(path)
      ? throw new InvalidOperationException(
        "Unable to determine test assembly path. Entry assembly location is not available.")
      : path!;
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
    _logger.LogInformation($"Output Formats: {string.Join(", ", GetOutputFormats())}");
    _logger.LogInformation($"Include Filters: {FormatArrayForLog(GetIncludeFilters())}");
    _logger.LogInformation($"Exclude Filters: {FormatArrayForLog(GetExcludeFilters())}");
    _logger.LogInformation($"Exclude by File: {FormatArrayForLog(GetExcludeByFileFilters())}");
    _logger.LogInformation($"Exclude by Attribute: {FormatArrayForLog(GetExcludeByAttributeFilters())}");
    _logger.LogInformation($"Include Directories: {FormatArrayForLog(GetIncludeDirectories())}");
    _logger.LogInformation($"Single Hit: {UseSingleHit}");
    _logger.LogInformation($"Include Test Assembly: {IncludeTestAssembly}");
    _logger.LogInformation($"Skip Auto Properties: {SkipAutoProps}");
    _logger.LogInformation($"Exclude Assemblies Without Sources: {GetExcludeAssembliesWithoutSources()}");
    //_logger.LogInformation($"Output directory for source mapping file: {GetOutputSourceMappingDirectory()}");
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

    // Check if config file has a non-default value
    if (defaultValue)
    {
      _logger?.LogDebug($"[Config file] {optionName}: {defaultValue}");
    }
    else
    {
      _logger?.LogDebug($"[Default] {optionName}: {defaultValue}");
    }

    return defaultValue;
  }

  private void LogOptionValue(string optionName, string[] values, bool isExplicit, string? source = null)
  {
    string prefix = isExplicit ? "[Explicitly set]" : (source is not null ? $"[{source}]" : "[Default]");
    string valuesStr = values.Length == 0 ? "(empty)" : string.Join(", ", values);
    _logger?.LogDebug($"{prefix} {optionName}: {valuesStr}");
  }

  private static string FormatArrayForLog(string[] values)
  {
    return values.Length == 0 ? "(none)" : string.Join(", ", values);
  }
}
