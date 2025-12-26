// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Coverlet.MTP.Configuration;

/// <summary>
/// Coverlet MTP settings (equivalent to coverlet.collector's CoverletSettings)
/// </summary>
public class CoverletMTPSettings
{
  public string? TestModule { get; set; }
  public string[] ReportFormats { get; set; } = ["cobertura"];
  public string[] IncludeFilters { get; set; } = [];
  public string[] IncludeDirectories { get; set; } = [];
  public string[] ExcludeFilters { get; set; } = ["[coverlet.*]*"];
  public string[] ExcludeSourceFiles { get; set; } = [];
  public string[] ExcludeAttributes { get; set; } = [];
  public string? MergeWith { get; set; }
  public bool UseSourceLink { get; set; }
  public bool SingleHit { get; set; }
  public bool IncludeTestAssembly { get; set; }
  public bool SkipAutoProps { get; set; }
  public string[] DoesNotReturnAttributes { get; set; } = [];
  public bool DeterministicReport { get; set; }
  public string ExcludeAssembliesWithoutSources { get; set; } = "MissingAll";
  public bool DisableManagedInstrumentationRestore { get; set; }

  public override string ToString()
  {
    var builder = new StringBuilder();
    builder.AppendFormat("TestModule: '{0}', ", TestModule);
    builder.AppendFormat("IncludeFilters: '{0}', ", string.Join(",", IncludeFilters));
    builder.AppendFormat("IncludeDirectories: '{0}', ", string.Join(",", IncludeDirectories));
    builder.AppendFormat("ExcludeFilters: '{0}', ", string.Join(",", ExcludeFilters));
    builder.AppendFormat("ExcludeSourceFiles: '{0}', ", string.Join(",", ExcludeSourceFiles));
    builder.AppendFormat("ExcludeAttributes: '{0}', ", string.Join(",", ExcludeAttributes));
    builder.AppendFormat("MergeWith: '{0}', ", MergeWith);
    builder.AppendFormat("UseSourceLink: '{0}', ", UseSourceLink);
    builder.AppendFormat("SingleHit: '{0}', ", SingleHit);
    builder.AppendFormat("IncludeTestAssembly: '{0}', ", IncludeTestAssembly);
    builder.AppendFormat("SkipAutoProps: '{0}', ", SkipAutoProps);
    builder.AppendFormat("DoesNotReturnAttributes: '{0}', ", string.Join(",", DoesNotReturnAttributes));
    builder.AppendFormat("DeterministicReport: '{0}', ", DeterministicReport);
    builder.AppendFormat("ExcludeAssembliesWithoutSources: '{0}'", ExcludeAssembliesWithoutSources);
    return builder.ToString();
  }
}
