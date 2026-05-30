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
  private readonly Coverlet.Core.Abstractions.IProcessAssemblyHelper _processAssemblyHelper;
  /// <summary>Full path to the test module (.dll), used to locate its deps.json for dynamic exclude discovery.</summary>
  private readonly string? _testModulePath;

  // Permanent baseline: coverlet assemblies must always be excluded regardless of source.
  // All other test-framework assemblies are discovered dynamically at runtime via IProcessAssemblyHelper.
  private static readonly string[] s_baselineExcludeFilters = ["[coverlet.*]*", "[Microsoft.VisualStudio.TestPlatform.*]*", "[testhost*]*"];

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
    : this(commandLineOptions, configFileSettings: null, testModulePath: null, logger)
  {
  }

  /// <summary>
  /// Initializes a new instance of CoverageConfiguration with both command-line options and config file settings.
  /// Configuration precedence (highest to lowest):
  /// 1. Explicit command-line options
  /// 2. Configuration file settings (coverlet.mtp.appsettings.json)
  /// 3. Dynamic defaults derived from the test module's deps.json (only when no config file)
  /// </summary>
  public CoverageConfiguration(
    ICommandLineOptions commandLineOptions,
    CoverletMTPSettings? configFileSettings,
    string? testModulePath,
    ILogger? logger = null,
    Coverlet.Core.Abstractions.IProcessAssemblyHelper? processAssemblyHelper = null)
  {
    _commandLineOptions = commandLineOptions;
    _configFileSettings = configFileSettings;
    _testModulePath = testModulePath;
    _logger = logger;
    _processAssemblyHelper = processAssemblyHelper ?? new Coverlet.Core.Helpers.ProcessAssemblyHelper();
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

    // Priority 2: Configuration file - authoritative when present
    if (_configFileSettings?.IsFromConfigFile == true)
    {
      // Config file is authoritative - use exactly what user specified
      LogOptionValue(CoverletOptionNames.Formats, _configFileSettings.ReportFormats, isExplicit: false, source: "config file");
      return _configFileSettings.ReportFormats;
    }

    // Priority 3: Built-in default - only when no config file
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

    // Priority 2: Configuration file - authoritative when present
    if (_configFileSettings?.IsFromConfigFile == true)
    {
      LogOptionValue(CoverletOptionNames.Include, _configFileSettings.IncludeFilters, isExplicit: false, source: "config file");
      return _configFileSettings.IncludeFilters;
    }

    // Priority 3: Default - only when no config file
    return [];
  }

  public string[] GetExcludeFilters()
  {
    // Priority 1: Explicit command-line option
    if (_commandLineOptions.TryGetOptionArgumentList(
      CoverletOptionNames.Exclude,
      out string[]? filters))
    {
      // Merge explicit exclusions with the permanent baseline only — dynamic list is not
      // applied here because the user is taking explicit control of the filter set.
      string[] merged = [.. s_baselineExcludeFilters.Concat(filters).Distinct()];
      LogOptionValue(CoverletOptionNames.Exclude, merged, isExplicit: true);
      return merged;
    }

    // Priority 2: Configuration file - authoritative when present.
    // Config file filters already include [coverlet.*]* prepended by CoverletMTPSettingsParser.
    // Dynamic defaults are intentionally skipped when a config file is in use.
    if (_configFileSettings?.IsFromConfigFile == true)
    {
      LogOptionValue(CoverletOptionNames.Exclude, _configFileSettings.ExcludeFilters, isExplicit: false, source: "config file");
      return _configFileSettings.ExcludeFilters;
    }

    // Priority 3: Dynamic defaults derived from the assemblies already loaded into this
    // process. Because only loaded assemblies are inspected, transitive dependencies that
    // have not yet been resolved are never included in the list.
    string[] dynamicDefaults = BuildDynamicExcludeFilters();
    LogOptionValue(CoverletOptionNames.Exclude, dynamicDefaults, isExplicit: false);
    return dynamicDefaults;
  }

  /// <summary>
  /// Builds exclude-filter defaults by merging two complementary sources, then combining
  /// with <see cref="s_baselineExcludeFilters"/>:
  /// <list type="bullet">
  ///   <item>
  ///     <description>
  ///       <b>deps.json</b> — reads <c>*.deps.json</c> next to the test module and converts
  ///       every non-project <c>RuntimeLibrary</c> entry into a filter pattern.  Covers
  ///       test-host infrastructure (e.g. <c>xunit.v3.mtp-v2</c>, <c>testhost</c>,
  ///       <c>Microsoft.TestPlatform.*</c>) that is only loaded inside the test-host process,
  ///       not in the controller process where instrumentation runs.
  ///     </description>
  ///   </item>
  ///   <item>
  ///     <description>
  ///       <b>AppDomain</b> — enumerates assemblies already loaded into this (controller)
  ///       process.  Covers build-extension assemblies such as
  ///       <c>Microsoft.Testing.Extensions.MSBuild</c> that are loaded by MTP during the
  ///       build-driven test run and are therefore held open when instrumentation starts,
  ///       but are not declared as <c>RuntimeLibrary</c> entries in deps.json.
  ///     </description>
  ///   </item>
  /// </list>
  /// </summary>
  private string[] BuildDynamicExcludeFilters()
  {
    if (string.IsNullOrWhiteSpace(_testModulePath))
    {
      return s_baselineExcludeFilters;
    }

    string simpleTestName = Path.GetFileNameWithoutExtension(_testModulePath);
    string? moduleDirectory = Path.GetDirectoryName(_testModulePath);

    if (string.IsNullOrWhiteSpace(moduleDirectory))
    {
      return s_baselineExcludeFilters;
    }

    try
    {
      // Source 1: runtime package entries from deps.json (test-host-side packages).
      IReadOnlyList<string> depsNames = _processAssemblyHelper.GetDepsJsonAssemblyNames(moduleDirectory, simpleTestName);

      // Source 2: assemblies already loaded in the controller process (build-extension assemblies
      // such as Microsoft.Testing.Extensions.MSBuild that are held open before instrumentation).
      IReadOnlyList<string> loadedNames = _processAssemblyHelper.GetLoadedAssemblyNames(simpleTestName);

      IEnumerable<string> dynamicFilters = depsNames.Concat(loadedNames)
        .Select(Coverlet.Core.Helpers.ProcessAssemblyHelper.ToExcludeFilter);

      IEnumerable<string> merged = s_baselineExcludeFilters
        .Concat(dynamicFilters)
        .Distinct(StringComparer.OrdinalIgnoreCase);

      return [.. Coverlet.Core.Helpers.ProcessAssemblyHelper.PruneRedundantFilters(merged)];
    }
    catch (Exception ex)
    {
      // Dynamic discovery is best-effort; fall back to the permanent baseline so coverage
      // collection is never completely broken by an unexpected error.
      _logger?.LogWarning($"Dynamic assembly discovery failed; falling back to baseline exclude filters. Reason: {ex.Message}");
      return s_baselineExcludeFilters;
    }
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

    // Priority 2: Configuration file - authoritative when present
    if (_configFileSettings?.IsFromConfigFile == true)
    {
      LogOptionValue(CoverletOptionNames.ExcludeByFile, _configFileSettings.ExcludeSourceFiles, isExplicit: false, source: "config file");
      return _configFileSettings.ExcludeSourceFiles;
    }

    // Priority 3: Default - only when no config file
    return [];
  }

  public string[] GetExcludeByAttributeFilters()
  {
    // Priority 1: Explicit command-line option
    if (_commandLineOptions.TryGetOptionArgumentList(
      CoverletOptionNames.ExcludeByAttribute,
      out string[]? filters))
    {
      // Merge explicit exclusions with defaults for CLI convenience
      string[] merged = [.. s_defaultExcludeByAttributes.Concat(filters).Distinct()];
      LogOptionValue(CoverletOptionNames.ExcludeByAttribute, merged, isExplicit: true);
      return merged;
    }

    // Priority 2: Configuration file - authoritative when present
    if (_configFileSettings?.IsFromConfigFile == true)
    {
      // Config file is authoritative - use exactly what user specified, no defaults merged
      LogOptionValue(CoverletOptionNames.ExcludeByAttribute, _configFileSettings.ExcludeAttributes, isExplicit: false, source: "config file");
      return _configFileSettings.ExcludeAttributes;
    }

    // Priority 3: Built-in defaults - only when no config file
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

    // Priority 2: Configuration file - authoritative when present
    if (_configFileSettings?.IsFromConfigFile == true)
    {
      LogOptionValue(CoverletOptionNames.ExcludeAssembliesWithoutSources, [_configFileSettings.ExcludeAssembliesWithoutSources], isExplicit: false, source: "config file");
      return _configFileSettings.ExcludeAssembliesWithoutSources;
    }

    // Priority 3: Default - only when no config file
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

    // Priority 2: Configuration file - authoritative when present
    if (_configFileSettings?.IsFromConfigFile == true)
    {
      LogOptionValue(CoverletOptionNames.IncludeDirectory, _configFileSettings.IncludeDirectories, isExplicit: false, source: "config file");
      return _configFileSettings.IncludeDirectories;
    }

    // Priority 3: Default - only when no config file
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

    // Priority 2: Configuration file - authoritative when present
    if (_configFileSettings?.IsFromConfigFile == true)
    {
      LogOptionValue(CoverletOptionNames.DoesNotReturnAttribute, _configFileSettings.DoesNotReturnAttributes, isExplicit: false, source: "config file");
      return _configFileSettings.DoesNotReturnAttributes;
    }

    // Priority 3: Default - only when no config file
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
