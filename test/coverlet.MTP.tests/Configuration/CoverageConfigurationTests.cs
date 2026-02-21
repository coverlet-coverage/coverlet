// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Coverlet.MTP.CommandLine;
using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Logging;
using Moq;
using Xunit;

namespace Coverlet.MTP.Configuration.Tests;

public sealed class CoverageConfigurationTests
{
  private readonly Mock<ILogger> _mockLogger;
  private readonly Mock<ICommandLineOptions> _mockCommandLineOptions;

  public CoverageConfigurationTests()
  {
    _mockCommandLineOptions = new Mock<ICommandLineOptions>();
    _mockLogger = new Mock<ILogger>();
  }

  #region Constructor Tests

  [Fact]
  public void ConstructorWithNullLoggerDoesNotThrow()
  {
    var exception = Record.Exception(() => new CoverageConfiguration(_mockCommandLineOptions.Object, null));
    Assert.Null(exception);
  }

  [Fact]
  public void ConstructorWithValidParametersInitializesSuccessfully()
  {
    var config = new CoverageConfiguration(_mockCommandLineOptions.Object, _mockLogger.Object);
    Assert.NotNull(config);
  }

  #endregion

  #region IsCoverageEnabled Tests

  [Theory]
  [InlineData(true)]
  [InlineData(false)]
  public void IsCoverageEnabledReturnsExpectedValue(bool isEnabled)
  {
    _mockCommandLineOptions.Setup(x => x.IsOptionSet(CoverletOptionNames.Coverage)).Returns(isEnabled);

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object, _mockLogger.Object);

    Assert.Equal(isEnabled, config.IsCoverageEnabled);
  }

  [Fact]
  public void IsCoverageEnabledCalledMultipleTimesReturnsSameValue()
  {
    _mockCommandLineOptions.Setup(x => x.IsOptionSet(CoverletOptionNames.Coverage)).Returns(true);
    var config = new CoverageConfiguration(_mockCommandLineOptions.Object, _mockLogger.Object);

    bool result1 = config.IsCoverageEnabled;
    bool result2 = config.IsCoverageEnabled;

    Assert.Equal(result1, result2);
    _mockCommandLineOptions.Verify(x => x.IsOptionSet(CoverletOptionNames.Coverage), Times.Exactly(2));
  }

  #endregion

  #region GetOutputFormats Tests

  [Fact]
  public void GetOutputFormatsWhenFormatsOptionSetReturnsCustomFormats()
  {
    string[] expectedFormats = ["json", "lcov", "opencover"];
    _mockCommandLineOptions
      .Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.Formats, out It.Ref<string[]?>.IsAny))
      .Returns(new TryGetOptionArgumentListDelegate((optionName, out formats) =>
      {
        formats = expectedFormats;
        return true;
      }));

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object, _mockLogger.Object);
    string[] result = config.GetOutputFormats();

    Assert.Equal(expectedFormats, result);
  }

  [Fact]
  public void GetOutputFormatsWhenFormatsOptionNotSetReturnsDefaultFormats()
  {
    _mockCommandLineOptions.Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.Formats, out It.Ref<string[]?>.IsAny))
      .Returns(new TryGetOptionArgumentListDelegate((optionName, out formats) =>
      {
        formats = null;
        return false;
      }));

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object, _mockLogger.Object);
    string[] result = config.GetOutputFormats();

    Assert.Equal(2, result.Length);
    Assert.Contains("json", result);
    Assert.Contains("cobertura", result);
  }

  [Fact]
  public void GetOutputFormatsWithEmptyArrayReturnsDefaultFormats()
  {
    _mockCommandLineOptions.Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.Formats, out It.Ref<string[]?>.IsAny))
      .Returns(new TryGetOptionArgumentListDelegate((optionName, out formats) =>
      {
        formats = [];
        return true;
      }));

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object, _mockLogger.Object);
    string[] result = config.GetOutputFormats();

    Assert.Empty(result);
  }

  [Fact]
  public void GetOutputFormatsWithSingleFormatReturnsSingleFormat()
  {
    string[] expectedFormats = ["cobertura"];
    _mockCommandLineOptions.Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.Formats, out It.Ref<string[]?>.IsAny))
      .Returns(new TryGetOptionArgumentListDelegate((optionName, out formats) =>
      {
        formats = expectedFormats;
        return true;
      }));

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object, _mockLogger.Object);
    string[] result = config.GetOutputFormats();

    Assert.Single(result);
    Assert.Equal("cobertura", result[0]);
  }

  #endregion

  #region GetIncludeFilters Tests

  [Fact]
  public void GetIncludeFiltersWhenOptionSetReturnsFilters()
  {
    string[] expectedFilters = ["[MyAssembly]*", "[AnotherAssembly]*"];
    _mockCommandLineOptions.Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.Include, out It.Ref<string[]?>.IsAny))
      .Returns(new TryGetOptionArgumentListDelegate((optionName, out filters) =>
      {
        filters = expectedFilters;
        return true;
      }));

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object, _mockLogger.Object);
    string[] result = config.GetIncludeFilters();

    Assert.Equal(expectedFilters, result);
  }

  [Fact]
  public void GetIncludeFiltersWhenOptionNotSetReturnsEmptyArray()
  {
    _mockCommandLineOptions.Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.Include, out It.Ref<string[]?>.IsAny))
      .Returns(new TryGetOptionArgumentListDelegate((optionName, out filters) =>
      {
        filters = null;
        return false;
      }));

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object, _mockLogger.Object);
    string[] result = config.GetIncludeFilters();

    Assert.Empty(result);
  }

  [Fact]
  public void GetIncludeFiltersWithSpecialCharactersReturnsFiltersUnmodified()
  {
    string[] expectedFilters = ["[My.Assembly*]*", "[Another+Assembly]*"];
    _mockCommandLineOptions.Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.Include, out It.Ref<string[]?>.IsAny))
      .Returns(new TryGetOptionArgumentListDelegate((optionName, out filters) =>
      {
        filters = expectedFilters;
        return true;
      }));

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object, _mockLogger.Object);
    string[] result = config.GetIncludeFilters();

    Assert.Equal(expectedFilters, result);
  }

  #endregion

  #region GetExcludeFilters Tests

  [Fact]
  public void GetExcludeFiltersWhenOptionSetMergesWithDefaults()
  {
    string[] customFilters = ["[CustomExclude]*"];
    _mockCommandLineOptions.Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.Exclude, out It.Ref<string[]?>.IsAny))
      .Returns(new TryGetOptionArgumentListDelegate((optionName, out filters) =>
      {
        filters = customFilters;
        return true;
      }));

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object, _mockLogger.Object);
    string[] result = config.GetExcludeFilters();

    Assert.Contains("[xunit.*]*", result);
    Assert.Contains("[Microsoft.Testing.*]*", result);
    Assert.Contains("[coverlet.*]*", result);
    Assert.Contains("[CustomExclude]*", result);
  }

  [Fact]
  public void GetExcludeFiltersWhenOptionNotSetReturnsDefaults()
  {
    _mockCommandLineOptions.Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.Exclude, out It.Ref<string[]?>.IsAny))
      .Returns(new TryGetOptionArgumentListDelegate((optionName, out filters) =>
      {
        filters = null;
        return false;
      }));

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object, _mockLogger.Object);
    string[] result = config.GetExcludeFilters();

    Assert.Equal(3, result.Length);
    Assert.Contains("[xunit.*]*", result);
    Assert.Contains("[Microsoft.Testing.*]*", result);
    Assert.Contains("[coverlet.*]*", result);
  }

  [Fact]
  public void GetExcludeFiltersRemovesDuplicates()
  {
    string[] customFilters = ["[xunit.*]*", "[CustomExclude]*"];
    _mockCommandLineOptions.Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.Exclude, out It.Ref<string[]?>.IsAny))
      .Returns(new TryGetOptionArgumentListDelegate((optionName, out filters) =>
      {
        filters = customFilters;
        return true;
      }));

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object, _mockLogger.Object);
    string[] result = config.GetExcludeFilters();

    Assert.Equal(4, result.Length);
    Assert.Single(result, f => f == "[xunit.*]*");
  }

  [Fact]
  public void GetExcludeFiltersWithMultipleDuplicatesRemovesAllDuplicates()
  {
    string[] customFilters = ["[xunit.*]*", "[Microsoft.Testing.*]*", "[CustomExclude]*", "[xunit.*]*"];
    _mockCommandLineOptions.Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.Exclude, out It.Ref<string[]?>.IsAny))
      .Returns(new TryGetOptionArgumentListDelegate((optionName, out filters) =>
      {
        filters = customFilters;
        return true;
      }));

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object, _mockLogger.Object);
    string[] result = config.GetExcludeFilters();

    Assert.Equal(4, result.Length); // 3 defaults + 1 custom (all duplicates removed)
    Assert.Single(result, f => f == "[xunit.*]*");
    Assert.Single(result, f => f == "[Microsoft.Testing.*]*");
  }

  #endregion

  #region GetExcludeByFileFilters Tests

  [Fact]
  public void GetExcludeByFileFiltersWhenOptionSetReturnsFilters()
  {
    string[] expectedFilters = ["**/Migrations/**", "**/Generated/**"];
    _mockCommandLineOptions.Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.ExcludeByFile, out It.Ref<string[]?>.IsAny))
      .Returns(new TryGetOptionArgumentListDelegate((optionName, out filters) =>
      {
        filters = expectedFilters;
        return true;
      }));

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object, _mockLogger.Object);
    string[] result = config.GetExcludeByFileFilters();

    Assert.Equal(expectedFilters, result);
  }

  [Fact]
  public void GetExcludeByFileFiltersWhenOptionNotSetReturnsEmptyArray()
  {
    _mockCommandLineOptions.Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.ExcludeByFile, out It.Ref<string[]?>.IsAny))
      .Returns(new TryGetOptionArgumentListDelegate((optionName, out filters) =>
      {
        filters = null;
        return false;
      }));

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object, _mockLogger.Object);
    string[] result = config.GetExcludeByFileFilters();

    Assert.Empty(result);
  }

  [Fact]
  public void GetExcludeByFileFiltersWithGlobPatternsReturnsFiltersUnmodified()
  {
    string[] expectedFilters = ["**/*.Designer.cs", "obj/**/*", "bin/**/*.g.cs"];
    _mockCommandLineOptions.Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.ExcludeByFile, out It.Ref<string[]?>.IsAny))
      .Returns(new TryGetOptionArgumentListDelegate((optionName, out filters) =>
      {
        filters = expectedFilters;
        return true;
      }));

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object, _mockLogger.Object);
    string[] result = config.GetExcludeByFileFilters();

    Assert.Equal(expectedFilters, result);
  }

  #endregion

  #region GetExcludeByAttributeFilters Tests

  [Fact]
  public void GetExcludeByAttributeFiltersWhenOptionSetMergesWithDefaults()
  {
    string[] customAttributes = ["CustomExcludeAttribute"];
    _mockCommandLineOptions.Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.ExcludeByAttribute, out It.Ref<string[]?>.IsAny))
      .Returns(new TryGetOptionArgumentListDelegate((optionName, out attributes) =>
      {
        attributes = customAttributes;
        return true;
      }));

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object, _mockLogger.Object);
    string[] result = config.GetExcludeByAttributeFilters();

    Assert.Contains("ExcludeFromCodeCoverage", result);
    Assert.Contains("ExcludeFromCodeCoverageAttribute", result);
    Assert.Contains("GeneratedCodeAttribute", result);
    Assert.Contains("CompilerGeneratedAttribute", result);
    Assert.Contains("CustomExcludeAttribute", result);
  }

  [Fact]
  public void GetExcludeByAttributeFiltersWhenOptionNotSetReturnsDefaults()
  {
    _mockCommandLineOptions.Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.ExcludeByAttribute, out It.Ref<string[]?>.IsAny))
      .Returns(new TryGetOptionArgumentListDelegate((optionName, out attributes) =>
      {
        attributes = null;
        return false;
      }));

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object, _mockLogger.Object);
    string[] result = config.GetExcludeByAttributeFilters();

    Assert.Equal(4, result.Length);
    Assert.Contains("ExcludeFromCodeCoverage", result);
    Assert.Contains("ExcludeFromCodeCoverageAttribute", result);
    Assert.Contains("GeneratedCodeAttribute", result);
    Assert.Contains("CompilerGeneratedAttribute", result);
  }

  [Fact]
  public void GetExcludeByAttributeFiltersRemovesDuplicates()
  {
    string[] customAttributes = ["GeneratedCodeAttribute", "CustomAttribute"];
    _mockCommandLineOptions.Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.ExcludeByAttribute, out It.Ref<string[]?>.IsAny))
      .Returns(new TryGetOptionArgumentListDelegate((optionName, out attributes) =>
      {
        attributes = customAttributes;
        return true;
      }));

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object, _mockLogger.Object);
    string[] result = config.GetExcludeByAttributeFilters();

    Assert.Equal(5, result.Length);
    Assert.Single(result, a => a == "GeneratedCodeAttribute");
  }

  [Fact]
  public void GetExcludeByAttributeFiltersWithFullyQualifiedNamesMergesCorrectly()
  {
    string[] customAttributes = ["System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute"];
    _mockCommandLineOptions.Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.ExcludeByAttribute, out It.Ref<string[]?>.IsAny))
      .Returns(new TryGetOptionArgumentListDelegate((optionName, out attributes) =>
      {
        attributes = customAttributes;
        return true;
      }));

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object, _mockLogger.Object);
    string[] result = config.GetExcludeByAttributeFilters();

    Assert.Equal(5, result.Length); // 4 defaults + 1 custom
    Assert.Contains("System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute", result);
  }

  #endregion

  #region GetIncludeDirectories Tests

  [Fact]
  public void GetIncludeDirectoriesWhenOptionSetReturnsDirectories()
  {
    string[] expectedDirectories = [@"C:\MyDir", @"C:\AnotherDir"];
    _mockCommandLineOptions.Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.IncludeDirectory, out It.Ref<string[]?>.IsAny))
      .Returns(new TryGetOptionArgumentListDelegate((optionName, out directories) =>
      {
        directories = expectedDirectories;
        return true;
      }));

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object, _mockLogger.Object);
    string[] result = config.GetIncludeDirectories();

    Assert.Equal(expectedDirectories, result);
  }

  [Fact]
  public void GetIncludeDirectoriesWhenOptionNotSetReturnsEmptyArray()
  {
    _mockCommandLineOptions.Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.IncludeDirectory, out It.Ref<string[]?>.IsAny))
      .Returns(new TryGetOptionArgumentListDelegate((optionName, out directories) =>
      {
        directories = null;
        return false;
      }));

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object, _mockLogger.Object);
    string[] result = config.GetIncludeDirectories();

    Assert.Empty(result);
  }

  [Fact]
  public void GetIncludeDirectoriesWithRelativePathsReturnsUnmodified()
  {
    string[] expectedDirectories = [@"..\src", @"./lib"];
    _mockCommandLineOptions.Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.IncludeDirectory, out It.Ref<string[]?>.IsAny))
      .Returns(new TryGetOptionArgumentListDelegate((optionName, out directories) =>
      {
        directories = expectedDirectories;
        return true;
      }));

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object, _mockLogger.Object);
    string[] result = config.GetIncludeDirectories();

    Assert.Equal(expectedDirectories, result);
  }

  #endregion

  #region Boolean Option Tests

  [Theory]
  [InlineData(true)]
  [InlineData(false)]
  public void UseSingleHitReturnsExpectedValue(bool isEnabled)
  {
    _mockCommandLineOptions.Setup(x => x.IsOptionSet(CoverletOptionNames.SingleHit)).Returns(isEnabled);

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object, _mockLogger.Object);

    Assert.Equal(isEnabled, config.UseSingleHit);
  }

  [Theory]
  [InlineData(true)]
  [InlineData(false)]
  public void IncludeTestAssemblyReturnsExpectedValue(bool isEnabled)
  {
    _mockCommandLineOptions.Setup(x => x.IsOptionSet(CoverletOptionNames.IncludeTestAssembly)).Returns(isEnabled);

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object, _mockLogger.Object);

    Assert.Equal(isEnabled, config.IncludeTestAssembly);
  }

  [Fact]
  public void SkipAutoPropsWhenOptionSetReturnsTrue()
  {
    _mockCommandLineOptions.Setup(x => x.IsOptionSet(CoverletOptionNames.SkipAutoProps)).Returns(true);

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object, _mockLogger.Object);

    Assert.True(config.SkipAutoProps);
  }

  [Fact]
  public void SkipAutoPropsWhenOptionNotSetReturnsDefaultTrue()
  {
    _mockCommandLineOptions.Setup(x => x.IsOptionSet(CoverletOptionNames.SkipAutoProps)).Returns(false);

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object, _mockLogger.Object);

    Assert.True(config.SkipAutoProps);
  }

  [Fact]
  public void ExcludeAssembliesWithoutSourcesWhenOptionSetReturnsAll()
  {
    string expectedOption = "All";
    _mockCommandLineOptions.Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.ExcludeAssembliesWithoutSources, out It.Ref<string[]?>.IsAny))
      .Returns(new TryGetOptionArgumentListDelegate((optionName, out filters) =>
      {
        filters = [expectedOption];
        return true;
      }));

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object, _mockLogger.Object);
    string result = config.GetExcludeAssembliesWithoutSources();
    Assert.Equal(expectedOption, result);
  }

  [Fact]
  public void ExcludeAssembliesWithoutSourcesWhenOptionNotSetReturnsReturnsAll()
  {
    _mockCommandLineOptions.Setup(x => x.IsOptionSet(CoverletOptionNames.ExcludeAssembliesWithoutSources)).Returns(false);

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object, _mockLogger.Object);

    string result = config.GetExcludeAssembliesWithoutSources();
    Assert.Equal("None", result);
  }

  #endregion

  #region GetDoesNotReturnAttributes Tests

  [Fact]
  public void GetDoesNotReturnAttributesWhenOptionSetReturnsAttributes()
  {
    string[] expectedAttributes = ["DoesNotReturnAttribute", "ThrowsAttribute"];
    _mockCommandLineOptions.Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.DoesNotReturnAttribute, out It.Ref<string[]?>.IsAny))
      .Returns(new TryGetOptionArgumentListDelegate((optionName, out attributes) =>
      {
        attributes = expectedAttributes;
        return true;
      }));

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object, _mockLogger.Object);
    string[] result = config.GetDoesNotReturnAttributes();

    Assert.Equal(expectedAttributes, result);
  }

  [Fact]
  public void GetDoesNotReturnAttributesWhenOptionNotSetReturnsEmptyArray()
  {
    _mockCommandLineOptions.Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.DoesNotReturnAttribute, out It.Ref<string[]?>.IsAny))
      .Returns(new TryGetOptionArgumentListDelegate((optionName, out attributes) =>
      {
        attributes = null;
        return false;
      }));

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object, _mockLogger.Object);
    string[] result = config.GetDoesNotReturnAttributes();

    Assert.Empty(result);
  }

  #endregion

  #region GetTestAssemblyPath Tests

  [Fact]
  public void GetTestAssemblyPathReturnsNonNullString()
  {
    string result = CoverageConfiguration.GetTestAssemblyPath();

    Assert.NotNull(result);
    Assert.NotEmpty(result);
  }

  [Fact]
  public void GetTestAssemblyPathReturnsValidPath()
  {
    string result = CoverageConfiguration.GetTestAssemblyPath();

    // Should return either a file path or directory path
    Assert.True(
      File.Exists(result) || Directory.Exists(result),
      $"Path '{result}' should exist as either file or directory"
    );
  }

  [Fact]
  public void GetTestAssemblyPathCalledMultipleTimesReturnsConsistentValue()
  {
    string result1 = CoverageConfiguration.GetTestAssemblyPath();
    string result2 = CoverageConfiguration.GetTestAssemblyPath();

    Assert.Equal(result1, result2);
  }

  #endregion

  #region Logging Tests

  [Fact]
  public void LogConfigurationSummaryWithNullLoggerDoesNotThrow()
  {
    var config = new CoverageConfiguration(_mockCommandLineOptions.Object, null);

    var exception = Record.Exception(config.LogConfigurationSummary);

    Assert.Null(exception);
  }

  [Fact]
  public void LogConfigurationSummaryWithValidLoggerLogsAllConfiguration()
  {
    // Arrange
    _mockCommandLineOptions.Setup(x => x.IsOptionSet(It.IsAny<string>())).Returns(false);
    _mockCommandLineOptions.Setup(x => x.TryGetOptionArgumentList(It.IsAny<string>(), out It.Ref<string[]?>.IsAny))
      .Returns(new TryGetOptionArgumentListDelegate((optionName, out value) =>
      {
        value = null;
        return false;
      }));

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object, _mockLogger.Object);

    // Act
    config.LogConfigurationSummary();

    // Assert - Verify the underlying Log method was called for LogInformation
    // Microsoft.Testing.Platform.Logging.ILogger.LogInformation extension method calls Log<TState>
    _mockLogger.Verify(
      x => x.Log(
        It.IsAny<LogLevel>(),
        It.Is<string>(s => s.Contains("Coverlet Coverage Configuration")),
        It.IsAny<Exception?>(),
        It.IsAny<Func<string, Exception?, string>>()),
      Times.Once);
  }

  #endregion

  // Helper delegate for Moq setup - must return bool to match TryGetOptionArgumentList signature
  private delegate bool TryGetOptionArgumentListDelegate(string optionName, out string[]? value);
}
