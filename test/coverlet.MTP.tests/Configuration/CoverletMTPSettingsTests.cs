// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace Coverlet.MTP.Configuration.Tests;

public class CoverletMTPSettingsTests
{
  [Fact]
  public void DefaultValuesAreCorrect()
  {
    // Act
    var settings = new CoverletMTPSettings();

    // Assert
    Assert.Null(settings.TestModule);
    Assert.Equal(["cobertura"], settings.ReportFormats);
    Assert.Empty(settings.IncludeFilters);
    Assert.Equal(["[coverlet.*]*"], settings.ExcludeFilters);
    Assert.Empty(settings.ExcludeSourceFiles);
    Assert.Empty(settings.ExcludeAttributes);
    Assert.Null(settings.MergeWith);
    Assert.False(settings.UseSourceLink);
    Assert.False(settings.SingleHit);
    Assert.False(settings.IncludeTestAssembly);
    Assert.False(settings.SkipAutoProps);
    Assert.Empty(settings.DoesNotReturnAttributes);
    Assert.False(settings.DeterministicReport);
    Assert.Equal("MissingAll", settings.ExcludeAssembliesWithoutSources);
    Assert.False(settings.DisableManagedInstrumentationRestore);
  }

  [Fact]
  public void ToStringReturnsCorrectFormat()
  {
    // Arrange
    var settings = new CoverletMTPSettings
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
      ExcludeAssembliesWithoutSources = "MissingAny"
    };

    // Act
    string result = settings.ToString();

    // Assert
    Assert.Contains("TestModule: 'TestModule.dll'", result);
    Assert.Contains("IncludeFilters: '[*]*'", result);
    Assert.Contains("UseSourceLink: 'True'", result);
    Assert.Contains("DeterministicReport: 'True'", result);
  }
}
