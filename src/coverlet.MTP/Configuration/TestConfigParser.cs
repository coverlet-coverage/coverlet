// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Coverlet.Core.Abstractions;
using Microsoft.Extensions.Configuration;

namespace Coverlet.MTP.Configuration;

/// <summary>
/// Parser for Microsoft Testing Platform testconfig.json format.
/// See: https://learn.microsoft.com/en-us/dotnet/core/testing/microsoft-testing-platform-config
/// </summary>
internal static class TestConfigParser
{
  /// <summary>
  /// The filename suffix for app-specific testconfig.json files.
  /// </summary>
  public const string TestConfigFileNameSuffix = ".testconfig.json";

  /// <summary>
  /// The generic testconfig.json filename.
  /// </summary>
  public const string GenericTestConfigFileName = "testconfig.json";

  /// <summary>
  /// The section name for platform options in testconfig.json.
  /// </summary>
  private const string PlatformOptionsSectionName = "platformOptions";

  /// <summary>
  /// Parse settings from testconfig.json if it exists.
  /// </summary>
  /// <param name="testModulePath">Path to the test assembly</param>
  /// <param name="fileSystem">File system abstraction for reading files</param>
  /// <returns>Parsed settings if testconfig.json exists and contains Coverlet section, null otherwise</returns>
  public static CoverletMTPSettings? Parse(string testModulePath, IFileSystem fileSystem)
  {
    string? configPath = FindTestConfigFile(testModulePath, fileSystem);
    if (configPath is null)
    {
      return null;
    }

    return ParseFromFile(configPath, testModulePath, fileSystem);
  }

  /// <summary>
  /// Finds the testconfig.json file for the given test module.
  /// Priority: [appname].testconfig.json > testconfig.json
  /// </summary>
  /// <param name="testModulePath">Path to the test assembly</param>
  /// <param name="fileSystem">File system abstraction</param>
  /// <returns>Path to the testconfig.json file if found, null otherwise</returns>
  public static string? FindTestConfigFile(string testModulePath, IFileSystem fileSystem)
  {
    string? directory = Path.GetDirectoryName(testModulePath);
    if (string.IsNullOrEmpty(directory))
    {
      return null;
    }

    string appName = Path.GetFileNameWithoutExtension(testModulePath);

    // Priority 1: [appname].testconfig.json
    string specificConfig = Path.Combine(directory, $"{appName}{TestConfigFileNameSuffix}");
    if (fileSystem.Exists(specificConfig))
    {
      return specificConfig;
    }

    // Priority 2: testconfig.json (generic)
    string genericConfig = Path.Combine(directory, GenericTestConfigFileName);
    return fileSystem.Exists(genericConfig) ? genericConfig : null;
  }

  /// <summary>
  /// Parse settings from a specific testconfig.json file.
  /// </summary>
  /// <param name="configPath">Path to the testconfig.json file</param>
  /// <param name="testModulePath">Path to the test assembly</param>
  /// <param name="fileSystem">File system abstraction</param>
  /// <returns>Parsed settings if file contains Coverlet section, null otherwise</returns>
  private static CoverletMTPSettings? ParseFromFile(string configPath, string testModulePath, IFileSystem fileSystem)
  {
    string jsonContent = fileSystem.ReadAllText(configPath);

    var configBuilder = new ConfigurationBuilder();
    using var stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonContent));
    configBuilder.AddJsonStream(stream);
    IConfiguration configuration = configBuilder.Build();

    // testconfig.json uses platformOptions:Coverlet section
    IConfigurationSection platformOptions = configuration.GetSection(PlatformOptionsSectionName);
    IConfigurationSection coverletSection = platformOptions.GetSection(CoverletMTPConstants.ConfigSectionName);

    // Check if the Coverlet section exists and has any values
    if (!coverletSection.Exists())
    {
      return null;
    }

    // Create a wrapper configuration that exposes Coverlet section at root level
    // This allows us to reuse CoverletMTPSettingsParser
    IConfiguration wrapperConfiguration = new ConfigurationBuilder()
      .AddInMemoryCollection(BuildCoverletSectionDictionary(coverletSection))
      .Build();

    return CoverletMTPSettingsParser.Parse(wrapperConfiguration, testModulePath);
  }

  /// <summary>
  /// Builds a dictionary of configuration values from the Coverlet section,
  /// restructured to match the expected format for CoverletMTPSettingsParser.
  /// </summary>
  /// <param name="coverletSection">The Coverlet section from platformOptions</param>
  /// <returns>Dictionary of configuration key-value pairs</returns>
  private static IEnumerable<KeyValuePair<string, string?>> BuildCoverletSectionDictionary(IConfigurationSection coverletSection)
  {
    // Map testconfig.json keys (lowercase) to CoverletMTPConstants keys (PascalCase)
    // testconfig.json format uses lowercase keys per MTP convention
    var keyMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
      ["include"] = CoverletMTPConstants.IncludeKey,
      ["includeDirectory"] = CoverletMTPConstants.IncludeDirectoryKey,
      ["exclude"] = CoverletMTPConstants.ExcludeKey,
      ["excludeByFile"] = CoverletMTPConstants.ExcludeByFileKey,
      ["excludeByAttribute"] = CoverletMTPConstants.ExcludeByAttributeKey,
      ["mergeWith"] = CoverletMTPConstants.MergeWithKey,
      ["useSourceLink"] = CoverletMTPConstants.UseSourceLinkKey,
      ["singleHit"] = CoverletMTPConstants.SingleHitKey,
      ["includeTestAssembly"] = CoverletMTPConstants.IncludeTestAssemblyKey,
      ["format"] = CoverletMTPConstants.FormatKey,
      ["skipAutoProps"] = CoverletMTPConstants.SkipAutoPropsKey,
      ["doesNotReturnAttribute"] = CoverletMTPConstants.DoesNotReturnAttributeKey,
      ["deterministicReport"] = CoverletMTPConstants.DeterministicReportKey,
      ["excludeAssembliesWithoutSources"] = CoverletMTPConstants.ExcludeAssembliesWithoutSourcesKey,
      ["disableManagedInstrumentationRestore"] = CoverletMTPConstants.DisableManagedInstrumentationRestoreKey
    };

    foreach (IConfigurationSection child in coverletSection.GetChildren())
    {
      string key = child.Key;
      string? value = child.Value;

      // Map the key if a mapping exists, otherwise use the original key
      if (keyMappings.TryGetValue(key, out string? mappedKey))
      {
        key = mappedKey;
      }

      // Build the full configuration path: Coverlet:KeyName
      yield return new KeyValuePair<string, string?>($"{CoverletMTPConstants.ConfigSectionName}:{key}", value);
    }
  }
}
