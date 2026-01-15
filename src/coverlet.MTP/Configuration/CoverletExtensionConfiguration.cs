// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Coverlet.MTP.Configuration;

/// <summary>
/// Configuration options for the Coverlet MTP extension.
/// </summary>
internal class CoverletExtensionConfiguration
{
  public string[] formats { get; set; } = ["json"];

  // Coverage parameters
  public string[]? IncludeFilters { get; set; }
  public string[]? IncludeDirectories { get; set; }
  public string[]? ExcludeFilters { get; set; }
  public string[]? ExcludedSourceFiles { get; set; }
  public string[]? ExcludeAttributes { get; set; }
  public bool IncludeTestAssembly { get; set; }
  public bool SingleHit { get; set; }
  public string? MergeWith { get; set; }
  public bool UseSourceLink { get; set; }
  public string[]? DoesNotReturnAttributes { get; set; }
  public bool SkipAutoProps { get; set; }
  public bool DeterministicReport { get; set; }
  public string? ExcludeAssembliesWithoutSources { get; set; }
  public bool DisableManagedInstrumentationRestore { get; set; }
}
