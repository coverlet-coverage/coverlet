// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Coverlet.Collector.Utilities
{
  internal static class CoverletConstants
  {
    public const string FriendlyName = "XPlat code coverage";
    public const string DefaultUri = @"datacollector://Microsoft/CoverletCodeCoverage/1.0";
    public const string DataCollectorName = "CoverletCoverageDataCollector";
    public const string DefaultReportFormat = "cobertura";
    public const string DefaultFileName = "coverage";
    public const string IncludeFiltersElementName = "Include";
    public const string IncludeDirectoriesElementName = "IncludeDirectory";
    public const string ExcludeFiltersElementName = "Exclude";
    public const string ExcludeSourceFilesElementName = "ExcludeByFile";
    public const string ExcludeAttributesElementName = "ExcludeByAttribute";
    public const string MergeWithElementName = "MergeWith";
    public const string UseSourceLinkElementName = "UseSourceLink";
    public const string SingleHitElementName = "SingleHit";
    public const string IncludeTestAssemblyElementName = "IncludeTestAssembly";
    public const string TestSourcesPropertyName = "TestSources";
    public const string ReportFormatElementName = "Format";
    // extending the default exclude filters to include Microsoft.VisualStudio.TestPlatform.* as well, to cover the case of users using older versions of test platform with newer versions of test framework
    public static readonly string[] DefaultExcludeFilters =
    {
        "[coverlet.*]*",
        "[xunit.*]*",
        "[NUnit3.*]*",
        "[Microsoft.Testing.*]*",
        "[Microsoft.Testplatform.*]*",
        "[Microsoft.VisualStudio.TestPlatform.*]*"
    };
    public const string InProcDataCollectorName = "CoverletInProcDataCollector";
    public const string SkipAutoProps = "SkipAutoProps";
    public const string DoesNotReturnAttributesElementName = "DoesNotReturnAttribute";
    public const string DeterministicReport = "DeterministicReport";
    public const string ExcludeAssembliesWithoutSources = "ExcludeAssembliesWithoutSources";
    public const string DisableManagedInstrumentationRestore = "DisableManagedInstrumentationRestore";

  }
}
