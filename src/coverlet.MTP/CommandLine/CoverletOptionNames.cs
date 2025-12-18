// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Coverlet.MTP.CommandLine;

/// <summary>
/// Centralized constants for Coverlet command-line option names.
/// Ensures consistency across all command-line providers.
/// </summary>
internal static class CoverletOptionNames
{
  public const string Coverage = "coverlet-coverage";
  public const string Formats = "formats";
  public const string Exclude = "exclude";
  public const string Include = "include";
  public const string ExcludeByFile = "exclude-by-file";
  public const string IncludeDirectory = "include-directory";
  public const string ExcludeByAttribute = "exclude-by-attribute";
  public const string IncludeTestAssembly = "include-test-assembly";
  public const string SingleHit = "single-hit";
  public const string SkipAutoProps = "skipautoprops";
  public const string DoesNotReturnAttribute = "does-not-return-attribute";
  public const string ExcludeAssembliesWithoutSources = "exclude-assemblies-without-sources";
  public const string SourceMappingFile = "source-mapping-file";
}
