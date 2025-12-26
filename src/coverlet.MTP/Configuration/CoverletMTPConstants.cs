// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Coverlet.MTP.Configuration;

/// <summary>
/// Constants for coverlet.MTP configuration (mirrors coverlet.collector's CoverletConstants)
/// </summary>
internal static class CoverletMTPConstants
{
  public const string ExtensionName = "CoverletMTP";
  public const string DefaultReportFormat = "cobertura";
  public const string DefaultFileName = "coverage";
  public const string DefaultExcludeFilter = "[coverlet.*]*";

  // Configuration keys for IConfiguration binding
  public const string ConfigSectionName = "Coverlet";
  public const string IncludeKey = "Include";
  public const string IncludeDirectoryKey = "IncludeDirectory";
  public const string ExcludeKey = "Exclude";
  public const string ExcludeByFileKey = "ExcludeByFile";
  public const string ExcludeByAttributeKey = "ExcludeByAttribute";
  public const string MergeWithKey = "MergeWith";
  public const string UseSourceLinkKey = "UseSourceLink";
  public const string SingleHitKey = "SingleHit";
  public const string IncludeTestAssemblyKey = "IncludeTestAssembly";
  public const string FormatKey = "Format";
  public const string SkipAutoPropsKey = "SkipAutoProps";
  public const string DoesNotReturnAttributeKey = "DoesNotReturnAttribute";
  public const string DeterministicReportKey = "DeterministicReport";
  public const string ExcludeAssembliesWithoutSourcesKey = "ExcludeAssembliesWithoutSources";
  public const string DisableManagedInstrumentationRestoreKey = "DisableManagedInstrumentationRestore";
}
