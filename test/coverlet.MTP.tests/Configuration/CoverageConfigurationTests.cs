// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Coverlet.MTP.CommandLine;
using Coverlet.MTP.Configuration;
using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Logging;
using Moq;
using Xunit;

namespace Coverlet.MTP.UnitTests.Configuration;

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
  public void Constructor_WithNullLogger_DoesNotThrow()
  {
    var exception = Record.Exception(() => new CoverageConfiguration(_mockCommandLineOptions.Object, null));
    Assert.Null(exception);
  }

  [Fact]
  public void Constructor_WithValidParameters_InitializesSuccessfully()
  {
    var config = new CoverageConfiguration(_mockCommandLineOptions.Object, _mockLogger.Object);
    Assert.NotNull(config);
  }

  #endregion

  #region IsCoverageEnabled Tests

  [Theory]
  [InlineData(true)]
  [InlineData(false)]
  public void IsCoverageEnabled_ReturnsExpectedValue(bool isEnabled)
  {
    _mockCommandLineOptions.Setup(x => x.IsOptionSet(CoverletOptionNames.Coverage)).Returns(isEnabled);

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object, _mockLogger.Object);

    Assert.Equal(isEnabled, config.IsCoverageEnabled);
  }

  [Fact]
  public void IsCoverageEnabled_CalledMultipleTimes_ReturnsSameValue()
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
  public void GetOutputFormats_WhenFormatsOptionSet_ReturnsCustomFormats()
  {
    string[] expectedFormats = ["json", "lcov", "opencover"];
    _mockCommandLineOptions
      .Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.Formats, out It.Ref<string[]?>.IsAny))
      .Returns(new TryGetOptionArgumentListDelegate((string optionName, out string[]? formats) =>
      {
        formats = expectedFormats;
        return true;
      }));

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object, _mockLogger.Object);
    string[] result = config.GetOutputFormats();

    Assert.Equal(expectedFormats, result);
  }

  [Fact]
  public void GetOutputFormats_WhenFormatsOptionNotSet_ReturnsDefaultFormats()
  {
    _mockCommandLineOptions.Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.Formats, out It.Ref<string[]?>.IsAny))
      .Returns(new TryGetOptionArgumentListDelegate((string optionName, out string[]? formats) =>
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
  public void GetOutputFormats_WithEmptyArray_ReturnsDefaultFormats()
  {
    _mockCommandLineOptions.Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.Formats, out It.Ref<string[]?>.IsAny))
      .Returns(new TryGetOptionArgumentListDelegate((string optionName, out string[]? formats) =>
      {
        formats = Array.Empty<string>();
        return true;
      }));

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object, _mockLogger.Object);
    string[] result = config.GetOutputFormats();

    Assert.Empty(result);
  }

  [Fact]
  public void GetOutputFormats_WithSingleFormat_ReturnsSingleFormat()
  {
    string[] expectedFormats = ["cobertura"];
    _mockCommandLineOptions.Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.Formats, out It.Ref<string[]?>.IsAny))
      .Returns(new TryGetOptionArgumentListDelegate((string optionName, out string[]? formats) =>
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
  public void GetIncludeFilters_WhenOptionSet_ReturnsFilters()
  {
    string[] expectedFilters = ["[MyAssembly]*", "[AnotherAssembly]*"];
    _mockCommandLineOptions.Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.Include, out It.Ref<string[]?>.IsAny))
      .Returns(new TryGetOptionArgumentListDelegate((string optionName, out string[]? filters) =>
      {
        filters = expectedFilters;
        return true;
      }));

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object, _mockLogger.Object);
    string[] result = config.GetIncludeFilters();

    Assert.Equal(expectedFilters, result);
  }

  [Fact]
  public void GetIncludeFilters_WhenOptionNotSet_ReturnsEmptyArray()
  {
    _mockCommandLineOptions.Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.Include, out It.Ref<string[]?>.IsAny))
      .Returns(new TryGetOptionArgumentListDelegate((string optionName, out string[]? filters) =>
      {
        filters = null;
        return false;
      }));

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object, _mockLogger.Object);
    string[] result = config.GetIncludeFilters();

    Assert.Empty(result);
  }

  [Fact]
  public void GetIncludeFilters_WithSpecialCharacters_ReturnsFiltersUnmodified()
  {
    string[] expectedFilters = ["[My.Assembly*]*", "[Another+Assembly]*"];
    _mockCommandLineOptions.Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.Include, out It.Ref<string[]?>.IsAny))
      .Returns(new TryGetOptionArgumentListDelegate((string optionName, out string[]? filters) =>
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
  public void GetExcludeFilters_WhenOptionSet_MergesWithDefaults()
  {
    string[] customFilters = ["[CustomExclude]*"];
    _mockCommandLineOptions.Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.Exclude, out It.Ref<string[]?>.IsAny))
      .Returns(new TryGetOptionArgumentListDelegate((string optionName, out string[]? filters) =>
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
  public void GetExcludeFilters_WhenOptionNotSet_ReturnsDefaults()
  {
    _mockCommandLineOptions.Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.Exclude, out It.Ref<string[]?>.IsAny))
      .Returns(new TryGetOptionArgumentListDelegate((string optionName, out string[]? filters) =>
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
  public void GetExcludeFilters_RemovesDuplicates()
  {
    string[] customFilters = ["[xunit.*]*", "[CustomExclude]*"];
    _mockCommandLineOptions.Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.Exclude, out It.Ref<string[]?>.IsAny))
      .Returns(new TryGetOptionArgumentListDelegate((string optionName, out string[]? filters) =>
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
  public void GetExcludeFilters_WithMultipleDuplicates_RemovesAllDuplicates()
  {
    string[] customFilters = ["[xunit.*]*", "[Microsoft.Testing.*]*", "[CustomExclude]*", "[xunit.*]*"];
    _mockCommandLineOptions.Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.Exclude, out It.Ref<string[]?>.IsAny))
      .Returns(new TryGetOptionArgumentListDelegate((string optionName, out string[]? filters) =>
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
  public void GetExcludeByFileFilters_WhenOptionSet_ReturnsFilters()
  {
    string[] expectedFilters = ["**/Migrations/**", "**/Generated/**"];
    _mockCommandLineOptions.Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.ExcludeByFile, out It.Ref<string[]?>.IsAny))
      .Returns(new TryGetOptionArgumentListDelegate((string optionName, out string[]? filters) =>
      {
        filters = expectedFilters;
        return true;
      }));

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object, _mockLogger.Object);
    string[] result = config.GetExcludeByFileFilters();

    Assert.Equal(expectedFilters, result);
  }

  [Fact]
  public void GetExcludeByFileFilters_WhenOptionNotSet_ReturnsEmptyArray()
  {
    _mockCommandLineOptions.Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.ExcludeByFile, out It.Ref<string[]?>.IsAny))
      .Returns(new TryGetOptionArgumentListDelegate((string optionName, out string[]? filters) =>
      {
        filters = null;
        return false;
      }));

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object, _mockLogger.Object);
    string[] result = config.GetExcludeByFileFilters();

    Assert.Empty(result);
  }

  [Fact]
  public void GetExcludeByFileFilters_WithGlobPatterns_ReturnsFiltersUnmodified()
  {
    string[] expectedFilters = ["**/*.Designer.cs", "obj/**/*", "bin/**/*.g.cs"];
    _mockCommandLineOptions.Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.ExcludeByFile, out It.Ref<string[]?>.IsAny))
      .Returns(new TryGetOptionArgumentListDelegate((string optionName, out string[]? filters) =>
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
  public void GetExcludeByAttributeFilters_WhenOptionSet_MergesWithDefaults()
  {
    string[] customAttributes = ["CustomExcludeAttribute"];
    _mockCommandLineOptions.Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.ExcludeByAttribute, out It.Ref<string[]?>.IsAny))
      .Returns(new TryGetOptionArgumentListDelegate((string optionName, out string[]? attributes) =>
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
  public void GetExcludeByAttributeFilters_WhenOptionNotSet_ReturnsDefaults()
  {
    _mockCommandLineOptions.Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.ExcludeByAttribute, out It.Ref<string[]?>.IsAny))
      .Returns(new TryGetOptionArgumentListDelegate((string optionName, out string[]? attributes) =>
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
  public void GetExcludeByAttributeFilters_RemovesDuplicates()
  {
    string[] customAttributes = ["GeneratedCodeAttribute", "CustomAttribute"];
    _mockCommandLineOptions.Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.ExcludeByAttribute, out It.Ref<string[]?>.IsAny))
      .Returns(new TryGetOptionArgumentListDelegate((string optionName, out string[]? attributes) =>
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
  public void GetExcludeByAttributeFilters_WithFullyQualifiedNames_MergesCorrectly()
  {
    string[] customAttributes = ["System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute"];
    _mockCommandLineOptions.Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.ExcludeByAttribute, out It.Ref<string[]?>.IsAny))
      .Returns(new TryGetOptionArgumentListDelegate((string optionName, out string[]? attributes) =>
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
  public void GetIncludeDirectories_WhenOptionSet_ReturnsDirectories()
  {
    string[] expectedDirectories = [@"C:\MyDir", @"C:\AnotherDir"];
    _mockCommandLineOptions.Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.IncludeDirectory, out It.Ref<string[]?>.IsAny))
      .Returns(new TryGetOptionArgumentListDelegate((string optionName, out string[]? directories) =>
      {
        directories = expectedDirectories;
        return true;
      }));

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object, _mockLogger.Object);
    string[] result = config.GetIncludeDirectories();

    Assert.Equal(expectedDirectories, result);
  }

  [Fact]
  public void GetIncludeDirectories_WhenOptionNotSet_ReturnsEmptyArray()
  {
    _mockCommandLineOptions.Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.IncludeDirectory, out It.Ref<string[]?>.IsAny))
      .Returns(new TryGetOptionArgumentListDelegate((string optionName, out string[]? directories) =>
      {
        directories = null;
        return false;
      }));

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object, _mockLogger.Object);
    string[] result = config.GetIncludeDirectories();

    Assert.Empty(result);
  }

  [Fact]
  public void GetIncludeDirectories_WithRelativePaths_ReturnsUnmodified()
  {
    string[] expectedDirectories = [@"..\src", @"./lib"];
    _mockCommandLineOptions.Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.IncludeDirectory, out It.Ref<string[]?>.IsAny))
      .Returns(new TryGetOptionArgumentListDelegate((string optionName, out string[]? directories) =>
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
  public void UseSingleHit_ReturnsExpectedValue(bool isEnabled)
  {
    _mockCommandLineOptions.Setup(x => x.IsOptionSet(CoverletOptionNames.SingleHit)).Returns(isEnabled);

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object, _mockLogger.Object);

    Assert.Equal(isEnabled, config.UseSingleHit);
  }

  [Theory]
  [InlineData(true)]
  [InlineData(false)]
  public void IncludeTestAssembly_ReturnsExpectedValue(bool isEnabled)
  {
    _mockCommandLineOptions.Setup(x => x.IsOptionSet(CoverletOptionNames.IncludeTestAssembly)).Returns(isEnabled);

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object, _mockLogger.Object);

    Assert.Equal(isEnabled, config.IncludeTestAssembly);
  }

  [Fact]
  public void SkipAutoProps_WhenOptionSet_ReturnsTrue()
  {
    _mockCommandLineOptions.Setup(x => x.IsOptionSet(CoverletOptionNames.SkipAutoProps)).Returns(true);

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object, _mockLogger.Object);

    Assert.True(config.SkipAutoProps);
  }

  [Fact]
  public void SkipAutoProps_WhenOptionNotSet_ReturnsDefaultTrue()
  {
    _mockCommandLineOptions.Setup(x => x.IsOptionSet(CoverletOptionNames.SkipAutoProps)).Returns(false);

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object, _mockLogger.Object);

    Assert.True(config.SkipAutoProps);
  }

  [Fact]
  public void ExcludeAssembliesWithoutSources_WhenOptionSet_ReturnsTrue()
  {
    _mockCommandLineOptions.Setup(x => x.IsOptionSet(CoverletOptionNames.ExcludeAssembliesWithoutSources)).Returns(true);

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object, _mockLogger.Object);

    Assert.True(config.ExcludeAssembliesWithoutSources);
  }

  [Fact]
  public void ExcludeAssembliesWithoutSources_WhenOptionNotSet_ReturnsDefaultTrue()
  {
    _mockCommandLineOptions.Setup(x => x.IsOptionSet(CoverletOptionNames.ExcludeAssembliesWithoutSources)).Returns(false);

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object, _mockLogger.Object);

    Assert.True(config.ExcludeAssembliesWithoutSources);
  }

  #endregion

  #region GetDoesNotReturnAttributes Tests

  [Fact]
  public void GetDoesNotReturnAttributes_WhenOptionSet_ReturnsAttributes()
  {
    string[] expectedAttributes = ["DoesNotReturnAttribute", "ThrowsAttribute"];
    _mockCommandLineOptions.Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.DoesNotReturnAttribute, out It.Ref<string[]?>.IsAny))
      .Returns(new TryGetOptionArgumentListDelegate((string optionName, out string[]? attributes) =>
      {
        attributes = expectedAttributes;
        return true;
      }));

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object, _mockLogger.Object);
    string[] result = config.GetDoesNotReturnAttributes();

    Assert.Equal(expectedAttributes, result);
  }

  [Fact]
  public void GetDoesNotReturnAttributes_WhenOptionNotSet_ReturnsEmptyArray()
  {
    _mockCommandLineOptions.Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.DoesNotReturnAttribute, out It.Ref<string[]?>.IsAny))
      .Returns(new TryGetOptionArgumentListDelegate((string optionName, out string[]? attributes) =>
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
  public void GetTestAssemblyPath_ReturnsNonNullString()
  {
    string result = CoverageConfiguration.GetTestAssemblyPath();

    Assert.NotNull(result);
    Assert.NotEmpty(result);
  }

  [Fact]
  public void GetTestAssemblyPath_ReturnsValidPath()
  {
    string result = CoverageConfiguration.GetTestAssemblyPath();

    // Should return either a file path or directory path
    Assert.True(
      File.Exists(result) || Directory.Exists(result),
      $"Path '{result}' should exist as either file or directory"
    );
  }

  [Fact]
  public void GetTestAssemblyPath_CalledMultipleTimes_ReturnsConsistentValue()
  {
    string result1 = CoverageConfiguration.GetTestAssemblyPath();
    string result2 = CoverageConfiguration.GetTestAssemblyPath();

    Assert.Equal(result1, result2);
  }

  #endregion

  #region Logging Tests

  [Fact]
  public void LogConfigurationSummary_WithNullLogger_DoesNotThrow()
  {
    var config = new CoverageConfiguration(_mockCommandLineOptions.Object, null);

    var exception = Record.Exception(() => config.LogConfigurationSummary());

    Assert.Null(exception);
  }

  #endregion

  // Helper delegate for Moq setup - must return bool to match TryGetOptionArgumentList signature
  private delegate bool TryGetOptionArgumentListDelegate(string optionName, out string[]? value);
}
