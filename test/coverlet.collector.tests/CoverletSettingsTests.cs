// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Coverlet.Collector.DataCollection;
using Xunit;

namespace Coverlet.Collector.Tests
{
  public class CoverletSettingsTests
  {
    [Fact]
    public void ToString_ReturnsCorrectFormat()
    {
      // Arrange
      var settings = new CoverletSettings
      {
        TestModule = "TestModule.dll",
        ReportFormats = ["json", "lcov"],
        IncludeFilters = ["[*]*"],
        IncludeDirectories = ["dir1", "dir2"],
        ExcludeFilters = ["[*]ExcludeNamespace.*"],
        ExcludeSourceFiles = ["file1.cs", "file2.cs"],
        ExcludeAttributes = ["ExcludeAttribute"],
        MergeWith = "coverage.json",
        UseSourceLink = true,
        SingleHit = false,
        IncludeTestAssembly = true,
        SkipAutoProps = false,
        DoesNotReturnAttributes = ["DoesNotReturn"],
        DeterministicReport = true,
        ExcludeAssembliesWithoutSources = "true"
      };

      var expectedString = "TestModule: 'TestModule.dll', " +
                           "IncludeFilters: '[*]*', " +
                           "IncludeDirectories: 'dir1,dir2', " +
                           "ExcludeFilters: '[*]ExcludeNamespace.*', " +
                           "ExcludeSourceFiles: 'file1.cs,file2.cs', " +
                           "ExcludeAttributes: 'ExcludeAttribute', " +
                           "MergeWith: 'coverage.json', " +
                           "UseSourceLink: 'True'" +
                           "SingleHit: 'False'" +
                           "IncludeTestAssembly: 'True'" +
                           "SkipAutoProps: 'False'" +
                           "DoesNotReturnAttributes: 'DoesNotReturn'" +
                           "DeterministicReport: 'True'" +
                           "ExcludeAssembliesWithoutSources: 'true'";

      // Act
      var result = settings.ToString();

      // Assert
      Assert.Equal(expectedString, result);
    }
  }
}
