// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;

namespace Coverlet.MTP.Configuration;

/// <summary>
/// Parser for coverlet.MTP settings from IConfiguration
/// </summary>
internal class CoverletMTPSettingsParser
{
  /// <summary>
  /// Parse settings from IConfiguration
  /// </summary>
  /// <param name="configuration">Configuration source</param>
  /// <param name="testModule">Test module path</param>
  /// <returns>Parsed settings</returns>
  public static CoverletMTPSettings Parse(IConfiguration? configuration, string testModule)
  {
    var settings = new CoverletMTPSettings
    {
      TestModule = testModule
    };

    if (configuration is null)
    {
      return settings;
    }

    IConfigurationSection section = configuration.GetSection(CoverletMTPConstants.ConfigSectionName);

    settings.IncludeFilters = ParseArrayValue(section, CoverletMTPConstants.IncludeKey);
    settings.IncludeDirectories = ParseArrayValue(section, CoverletMTPConstants.IncludeDirectoryKey);
    settings.ExcludeFilters = ParseExcludeFilters(section);
    settings.ExcludeSourceFiles = ParseArrayValue(section, CoverletMTPConstants.ExcludeByFileKey);
    settings.ExcludeAttributes = ParseArrayValue(section, CoverletMTPConstants.ExcludeByAttributeKey);
    settings.MergeWith = section[CoverletMTPConstants.MergeWithKey];
    settings.UseSourceLink = ParseBoolValue(section, CoverletMTPConstants.UseSourceLinkKey);
    settings.SingleHit = ParseBoolValue(section, CoverletMTPConstants.SingleHitKey);
    settings.IncludeTestAssembly = ParseBoolValue(section, CoverletMTPConstants.IncludeTestAssemblyKey);
    settings.SkipAutoProps = ParseBoolValue(section, CoverletMTPConstants.SkipAutoPropsKey);
    settings.DoesNotReturnAttributes = ParseArrayValue(section, CoverletMTPConstants.DoesNotReturnAttributeKey);
    settings.DeterministicReport = ParseBoolValue(section, CoverletMTPConstants.DeterministicReportKey);
    settings.ExcludeAssembliesWithoutSources = section[CoverletMTPConstants.ExcludeAssembliesWithoutSourcesKey] ?? "MissingAll";
    settings.DisableManagedInstrumentationRestore = ParseBoolValue(section, CoverletMTPConstants.DisableManagedInstrumentationRestoreKey);
    settings.ReportFormats = ParseReportFormats(section);

    return settings;
  }

  private static string[] ParseReportFormats(IConfigurationSection section)
  {
    string[] formats = ParseArrayValue(section, CoverletMTPConstants.FormatKey);
    return formats.Length == 0 ? [CoverletMTPConstants.DefaultReportFormat] : formats;
  }

  private static string[] ParseExcludeFilters(IConfigurationSection section)
  {
    string[] filters = ParseArrayValue(section, CoverletMTPConstants.ExcludeKey);
    // Always include default exclude filter
    return [CoverletMTPConstants.DefaultExcludeFilter, .. filters];
  }

  private static string[] ParseArrayValue(IConfigurationSection section, string key)
  {
    string? value = section[key];
    return string.IsNullOrWhiteSpace(value)
      ? []
      : [.. value!
        .Split(',', (char)StringSplitOptions.RemoveEmptyEntries)
        .Select(v => v.Trim())
        .Where(v => !string.IsNullOrWhiteSpace(v))];
  }

  private static bool ParseBoolValue(IConfigurationSection section, string key)
  {
    return bool.TryParse(section[key], out bool result) && result;
  }
}
