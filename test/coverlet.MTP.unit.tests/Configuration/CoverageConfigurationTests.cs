// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Coverlet.MTP.CommandLine;
using Coverlet.MTP.Configuration;
using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Logging;
using Moq;
using Xunit;

namespace Coverlet.MTP.Unit.Tests.Configuration;

public class CoverageConfigurationTests
{
  private readonly Mock<ICommandLineOptions> _mockCommandLineOptions;
  private readonly Mock<ILogger> _mockLogger;

  public CoverageConfigurationTests()
  {
    _mockCommandLineOptions = new Mock<ICommandLineOptions>();
    _mockLogger = new Mock<ILogger>();
  }

  #region IsCoverageEnabled Tests

  [Fact]
  public void IsCoverageEnabled_WhenOptionSet_ReturnsTrue()
  {
    // Arrange
    _mockCommandLineOptions
      .Setup(x => x.IsOptionSet(CoverletOptionNames.Coverage))
      .Returns(true);

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object);

    // Act
    bool result = config.IsCoverageEnabled;

    // Assert
    Assert.True(result);
  }

  [Fact]
  public void IsCoverageEnabled_WhenOptionNotSet_ReturnsFalse()
  {
    // Arrange
    _mockCommandLineOptions
      .Setup(x => x.IsOptionSet(CoverletOptionNames.Coverage))
      .Returns(false);

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object);

    // Act
    bool result = config.IsCoverageEnabled;

    // Assert
    Assert.False(result);
  }

  #endregion

  #region GetOutputFormats Tests

  [Fact]
  public void GetOutputFormats_WhenNotSpecified_ReturnsDefaultFormats()
  {
    // Arrange
    _mockCommandLineOptions
      .Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.Formats, out It.Ref<string[]?>.IsAny))
      .Returns(false);

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object);

    // Act
    string[] result = config.GetOutputFormats();

    // Assert
    Assert.Equal(2, result.Length);
    Assert.Contains("json", result);
    Assert.Contains("cobertura", result);
  }

  #endregion

  #region GetIncludeFilters Tests

  [Fact]
  public void GetIncludeFilters_WhenNotSpecified_ReturnsEmptyArray()
  {
    // Arrange
    _mockCommandLineOptions
      .Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.Include, out It.Ref<string[]?>.IsAny))
      .Returns(false);

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object);

    // Act
    string[] result = config.GetIncludeFilters();

    // Assert
    Assert.Empty(result);
  }

  [Fact]
  public void GetIncludeFilters_WhenSpecified_ReturnsSpecifiedFilters()
  {
    // Arrange
    string[]? filters = new[] { "[MyAssembly]*", "[OtherAssembly]MyType" };
    _mockCommandLineOptions
      .Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.Include, out filters))
      .Returns(true);

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object);

    // Act
    string[] result = config.GetIncludeFilters();

    // Assert
    Assert.Equal(2, result.Length);
    Assert.Equal("[MyAssembly]*", result[0]);
    Assert.Equal("[OtherAssembly]MyType", result[1]);
  }

  #endregion

  #region GetExcludeFilters Tests

  [Fact]
  public void GetExcludeFilters_WhenNotSpecified_ReturnsDefaultExclusions()
  {
    // Arrange
    _mockCommandLineOptions
      .Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.Exclude, out It.Ref<string[]?>.IsAny))
      .Returns(false);

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object);

    // Act
    string[] result = config.GetExcludeFilters();

    // Assert
    Assert.Contains("[xunit.*]*", result);
    Assert.Contains("[Microsoft.Testing.*]*", result);
    Assert.Contains("[coverlet.*]*", result);
  }

  [Fact]
  public void GetExcludeFilters_WhenSpecified_MergesWithDefaults()
  {
    // Arrange
    string[]? filters = new[] { "[MyTests]*" };
    _mockCommandLineOptions
      .Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.Exclude, out filters))
      .Returns(true);

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object);

    // Act
    string[] result = config.GetExcludeFilters();

    // Assert
    Assert.Contains("[MyTests]*", result);
    Assert.Contains("[xunit.*]*", result);
    Assert.Contains("[Microsoft.Testing.*]*", result);
    Assert.Contains("[coverlet.*]*", result);
  }

  [Fact]
  public void GetExcludeFilters_WhenSpecifiedWithDuplicates_ReturnsDistinctValues()
  {
    // Arrange
    string[]? filters = new[] { "[xunit.*]*", "[MyTests]*" };
    _mockCommandLineOptions
      .Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.Exclude, out filters))
      .Returns(true);

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object);

    // Act
    string[] result = config.GetExcludeFilters();

    // Assert
    int xunitCount = result.Count(f => f == "[xunit.*]*");
    Assert.Equal(1, xunitCount);
  }

  #endregion

  #region GetExcludeByFileFilters Tests

  [Fact]
  public void GetExcludeByFileFilters_WhenNotSpecified_ReturnsEmptyArray()
  {
    // Arrange
    _mockCommandLineOptions
      .Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.ExcludeByFile, out It.Ref<string[]?>.IsAny))
      .Returns(false);

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object);

    // Act
    string[] result = config.GetExcludeByFileFilters();

    // Assert
    Assert.Empty(result);
  }

  [Fact]
  public void GetExcludeByFileFilters_WhenSpecified_ReturnsSpecifiedPatterns()
  {
    // Arrange
    string[]? patterns = new[] { "**/Generated/*.cs", "**/obj/**" };
    _mockCommandLineOptions
      .Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.ExcludeByFile, out patterns))
      .Returns(true);

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object);

    // Act
    string[] result = config.GetExcludeByFileFilters();

    // Assert
    Assert.Equal(2, result.Length);
    Assert.Equal("**/Generated/*.cs", result[0]);
    Assert.Equal("**/obj/**", result[1]);
  }

  #endregion

  #region GetExcludeByAttributeFilters Tests

  [Fact]
  public void GetExcludeByAttributeFilters_WhenNotSpecified_ReturnsDefaultAttributes()
  {
    // Arrange
    _mockCommandLineOptions
      .Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.ExcludeByAttribute, out It.Ref<string[]?>.IsAny))
      .Returns(false);

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object);

    // Act
    string[] result = config.GetExcludeByAttributeFilters();

    // Assert
    Assert.Contains("ExcludeFromCodeCoverage", result);
    Assert.Contains("ExcludeFromCodeCoverageAttribute", result);
    Assert.Contains("GeneratedCodeAttribute", result);
    Assert.Contains("CompilerGeneratedAttribute", result);
  }

  [Fact]
  public void GetExcludeByAttributeFilters_WhenSpecified_MergesWithDefaults()
  {
    // Arrange
    string[]? attributes = new[] { "ObsoleteAttribute" };
    _mockCommandLineOptions
      .Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.ExcludeByAttribute, out attributes))
      .Returns(true);

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object);

    // Act
    string[] result = config.GetExcludeByAttributeFilters();

    // Assert
    Assert.Contains("ObsoleteAttribute", result);
    Assert.Contains("ExcludeFromCodeCoverage", result);
    Assert.Contains("GeneratedCodeAttribute", result);
  }

  #endregion

  #region GetIncludeDirectories Tests

  [Fact]
  public void GetIncludeDirectories_WhenNotSpecified_ReturnsEmptyArray()
  {
    // Arrange
    _mockCommandLineOptions
      .Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.IncludeDirectory, out It.Ref<string[]?>.IsAny))
      .Returns(false);

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object);

    // Act
    string[] result = config.GetIncludeDirectories();

    // Assert
    Assert.Empty(result);
  }

  [Fact]
  public void GetIncludeDirectories_WhenSpecified_ReturnsSpecifiedDirectories()
  {
    // Arrange
    string[]? directories = new[] { "/path/to/libs", "/another/path" };
    _mockCommandLineOptions
      .Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.IncludeDirectory, out directories))
      .Returns(true);

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object);

    // Act
    string[] result = config.GetIncludeDirectories();

    // Assert
    Assert.Equal(2, result.Length);
    Assert.Equal("/path/to/libs", result[0]);
    Assert.Equal("/another/path", result[1]);
  }

  #endregion

  #region Boolean Option Tests

  [Fact]
  public void UseSingleHit_WhenOptionSet_ReturnsTrue()
  {
    // Arrange
    _mockCommandLineOptions
      .Setup(x => x.IsOptionSet(CoverletOptionNames.SingleHit))
      .Returns(true);

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object);

    // Act
    bool result = config.UseSingleHit;

    // Assert
    Assert.True(result);
  }

  [Fact]
  public void UseSingleHit_WhenOptionNotSet_ReturnsFalse()
  {
    // Arrange
    _mockCommandLineOptions
      .Setup(x => x.IsOptionSet(CoverletOptionNames.SingleHit))
      .Returns(false);

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object);

    // Act
    bool result = config.UseSingleHit;

    // Assert
    Assert.False(result);
  }

  [Fact]
  public void IncludeTestAssembly_WhenOptionSet_ReturnsTrue()
  {
    // Arrange
    _mockCommandLineOptions
      .Setup(x => x.IsOptionSet(CoverletOptionNames.IncludeTestAssembly))
      .Returns(true);

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object);

    // Act
    bool result = config.IncludeTestAssembly;

    // Assert
    Assert.True(result);
  }

  [Fact]
  public void IncludeTestAssembly_WhenOptionNotSet_ReturnsFalse()
  {
    // Arrange
    _mockCommandLineOptions
      .Setup(x => x.IsOptionSet(CoverletOptionNames.IncludeTestAssembly))
      .Returns(false);

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object);

    // Act
    bool result = config.IncludeTestAssembly;

    // Assert
    Assert.False(result);
  }

  [Fact]
  public void SkipAutoProps_WhenOptionSet_ReturnsTrue()
  {
    // Arrange
    _mockCommandLineOptions
      .Setup(x => x.IsOptionSet(CoverletOptionNames.SkipAutoProps))
      .Returns(true);

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object);

    // Act
    bool result = config.SkipAutoProps;

    // Assert
    Assert.True(result);
  }

  [Fact]
  public void SkipAutoProps_WhenOptionNotSet_ReturnsDefaultTrue()
  {
    // Arrange
    _mockCommandLineOptions
      .Setup(x => x.IsOptionSet(CoverletOptionNames.SkipAutoProps))
      .Returns(false);

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object);

    // Act
    bool result = config.SkipAutoProps;

    // Assert
    Assert.True(result); // Default is true
  }

  [Fact]
  public void ExcludeAssembliesWithoutSources_WhenOptionSet_ReturnsTrue()
  {
    // Arrange
    _mockCommandLineOptions
      .Setup(x => x.IsOptionSet(CoverletOptionNames.ExcludeAssembliesWithoutSources))
      .Returns(true);

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object);

    // Act
    bool result = config.ExcludeAssembliesWithoutSources;

    // Assert
    Assert.True(result);
  }

  [Fact]
  public void ExcludeAssembliesWithoutSources_WhenOptionNotSet_ReturnsDefaultTrue()
  {
    // Arrange
    _mockCommandLineOptions
      .Setup(x => x.IsOptionSet(CoverletOptionNames.ExcludeAssembliesWithoutSources))
      .Returns(false);

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object);

    // Act
    bool result = config.ExcludeAssembliesWithoutSources;

    // Assert
    Assert.True(result); // Default is true
  }

  #endregion

  #region GetDoesNotReturnAttributes Tests

  [Fact]
  public void GetDoesNotReturnAttributes_WhenNotSpecified_ReturnsEmptyArray()
  {
    // Arrange
    _mockCommandLineOptions
      .Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.DoesNotReturnAttribute, out It.Ref<string[]?>.IsAny))
      .Returns(false);

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object);

    // Act
    string[] result = config.GetDoesNotReturnAttributes();

    // Assert
    Assert.Empty(result);
  }

  [Fact]
  public void GetDoesNotReturnAttributes_WhenSpecified_ReturnsSpecifiedAttributes()
  {
    // Arrange
    string[]? attributes = new[] { "DoesNotReturnAttribute", "ContractAnnotationAttribute" };
    _mockCommandLineOptions
      .Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.DoesNotReturnAttribute, out attributes))
      .Returns(true);

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object);

    // Act
    string[] result = config.GetDoesNotReturnAttributes();

    // Assert
    Assert.Equal(2, result.Length);
    Assert.Equal("DoesNotReturnAttribute", result[0]);
    Assert.Equal("ContractAnnotationAttribute", result[1]);
  }

  #endregion

  #region GetTestAssemblyPath Tests

  [Fact]
  public void GetTestAssemblyPath_ReturnsNonEmptyPath()
  {
    // Act
    string result = CoverageConfiguration.GetTestAssemblyPath();

    // Assert
    Assert.False(string.IsNullOrEmpty(result));
  }

  [Fact]
  public void GetTestAssemblyPath_ReturnsExistingPathOrBaseDirectory()
  {
    // Act
    string result = CoverageConfiguration.GetTestAssemblyPath();

    // Assert
    // Should return either a file that exists or a directory that exists
    Assert.True(File.Exists(result) || Directory.Exists(result));
  }

  #endregion

  #region LogConfigurationSummary Tests

  [Fact]
  public void LogConfigurationSummary_WithNullLogger_DoesNotThrow()
  {
    // Arrange
    _mockCommandLineOptions
      .Setup(x => x.TryGetOptionArgumentList(It.IsAny<string>(), out It.Ref<string[]?>.IsAny))
      .Returns(false);

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object, logger: null);

    // Act & Assert - Should not throw
    var exception = Record.Exception(() => config.LogConfigurationSummary());
    Assert.Null(exception);
  }

  #endregion

  #region Constructor Tests

  [Fact]
  public void Constructor_WithCommandLineOptions_CreatesInstance()
  {
    // Act
    var config = new CoverageConfiguration(_mockCommandLineOptions.Object);

    // Assert
    Assert.NotNull(config);
  }

  [Fact]
  public void Constructor_WithCommandLineOptionsAndLogger_CreatesInstance()
  {
    // Act
    var config = new CoverageConfiguration(_mockCommandLineOptions.Object, _mockLogger.Object);

    // Assert
    Assert.NotNull(config);
  }

  #endregion

  #region LogConfigurationSummary Additional Tests

  [Fact]
  public void LogConfigurationSummary_WithLogger_LogsAllConfigurationValues()
  {
    // Arrange
    _mockCommandLineOptions
      .Setup(x => x.TryGetOptionArgumentList(It.IsAny<string>(), out It.Ref<string[]?>.IsAny))
      .Returns(false);
    _mockCommandLineOptions
      .Setup(x => x.IsOptionSet(It.IsAny<string>()))
      .Returns(false);

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object, _mockLogger.Object);

    // Act
    config.LogConfigurationSummary();

    // Assert - verify LogInformation was called for each configuration line
    _mockLogger.Verify(
      x => x.LogInformation(It.Is<string>(s => s.Contains("=== Coverlet Coverage Configuration ==="))),
      Times.Once);
    _mockLogger.Verify(
      x => x.LogInformation(It.Is<string>(s => s.StartsWith("Test Assembly:"))),
      Times.Once);
    _mockLogger.Verify(
      x => x.LogInformation(It.Is<string>(s => s.StartsWith("Output Formats:"))),
      Times.Once);
    _mockLogger.Verify(
      x => x.LogInformation(It.Is<string>(s => s.StartsWith("Include Filters:"))),
      Times.Once);
    _mockLogger.Verify(
      x => x.LogInformation(It.Is<string>(s => s.StartsWith("Exclude Filters:"))),
      Times.Once);
    _mockLogger.Verify(
      x => x.LogInformation(It.Is<string>(s => s.StartsWith("Exclude by File:"))),
      Times.Once);
    _mockLogger.Verify(
      x => x.LogInformation(It.Is<string>(s => s.StartsWith("Exclude by Attribute:"))),
      Times.Once);
    _mockLogger.Verify(
      x => x.LogInformation(It.Is<string>(s => s.StartsWith("Include Directories:"))),
      Times.Once);
    _mockLogger.Verify(
      x => x.LogInformation(It.Is<string>(s => s.StartsWith("Single Hit:"))),
      Times.Once);
    _mockLogger.Verify(
      x => x.LogInformation(It.Is<string>(s => s.StartsWith("Include Test Assembly:"))),
      Times.Once);
    _mockLogger.Verify(
      x => x.LogInformation(It.Is<string>(s => s.StartsWith("Skip Auto Properties:"))),
      Times.Once);
    _mockLogger.Verify(
      x => x.LogInformation(It.Is<string>(s => s.StartsWith("Exclude Assemblies Without Sources:"))),
      Times.Once);
    _mockLogger.Verify(
      x => x.LogInformation(It.Is<string>(s => s.Contains("========================================"))),
      Times.Once);
  }

  [Fact]
  public void LogConfigurationSummary_WithLogger_LogsDefaultFormats()
  {
    // Arrange
    _mockCommandLineOptions
      .Setup(x => x.TryGetOptionArgumentList(It.IsAny<string>(), out It.Ref<string[]?>.IsAny))
      .Returns(false);
    _mockCommandLineOptions
      .Setup(x => x.IsOptionSet(It.IsAny<string>()))
      .Returns(false);

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object, _mockLogger.Object);

    // Act
    config.LogConfigurationSummary();

    // Assert
    _mockLogger.Verify(
      x => x.LogInformation(It.Is<string>(s => s.Contains("json") && s.Contains("cobertura"))),
      Times.Once);
  }

  [Fact]
  public void LogConfigurationSummary_WithEmptyFilters_LogsNone()
  {
    // Arrange
    _mockCommandLineOptions
      .Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.Include, out It.Ref<string[]?>.IsAny))
      .Returns(false);
    _mockCommandLineOptions
      .Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.Exclude, out It.Ref<string[]?>.IsAny))
      .Returns(false);
    _mockCommandLineOptions
      .Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.Formats, out It.Ref<string[]?>.IsAny))
      .Returns(false);
    _mockCommandLineOptions
      .Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.ExcludeByFile, out It.Ref<string[]?>.IsAny))
      .Returns(false);
    _mockCommandLineOptions
      .Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.ExcludeByAttribute, out It.Ref<string[]?>.IsAny))
      .Returns(false);
    _mockCommandLineOptions
      .Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.IncludeDirectory, out It.Ref<string[]?>.IsAny))
      .Returns(false);
    _mockCommandLineOptions
      .Setup(x => x.IsOptionSet(It.IsAny<string>()))
      .Returns(false);

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object, _mockLogger.Object);

    // Act
    config.LogConfigurationSummary();

    // Assert - Include Filters should show "(none)" since no filters specified
    _mockLogger.Verify(
      x => x.LogInformation(It.Is<string>(s => s.StartsWith("Include Filters:") && s.Contains("(none)"))),
      Times.Once);
  }

  [Fact]
  public void LogConfigurationSummary_WithBooleanOptionsSet_LogsTrueValues()
  {
    // Arrange
    _mockCommandLineOptions
      .Setup(x => x.TryGetOptionArgumentList(It.IsAny<string>(), out It.Ref<string[]?>.IsAny))
      .Returns(false);
    _mockCommandLineOptions
      .Setup(x => x.IsOptionSet(CoverletOptionNames.SingleHit))
      .Returns(true);
    _mockCommandLineOptions
      .Setup(x => x.IsOptionSet(CoverletOptionNames.IncludeTestAssembly))
      .Returns(true);

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object, _mockLogger.Object);

    // Act
    config.LogConfigurationSummary();

    // Assert
    _mockLogger.Verify(
      x => x.LogInformation(It.Is<string>(s => s.Contains("Single Hit: True"))),
      Times.Once);
    _mockLogger.Verify(
      x => x.LogInformation(It.Is<string>(s => s.Contains("Include Test Assembly: True"))),
      Times.Once);
  }

  [Fact]
  public void LogConfigurationSummary_WithBooleanOptionsNotSet_LogsFalseOrDefaultValues()
  {
    // Arrange
    _mockCommandLineOptions
      .Setup(x => x.TryGetOptionArgumentList(It.IsAny<string>(), out It.Ref<string[]?>.IsAny))
      .Returns(false);
    _mockCommandLineOptions
      .Setup(x => x.IsOptionSet(CoverletOptionNames.SingleHit))
      .Returns(false);
    _mockCommandLineOptions
      .Setup(x => x.IsOptionSet(CoverletOptionNames.IncludeTestAssembly))
      .Returns(false);
    _mockCommandLineOptions
      .Setup(x => x.IsOptionSet(CoverletOptionNames.SkipAutoProps))
      .Returns(false);
    _mockCommandLineOptions
      .Setup(x => x.IsOptionSet(CoverletOptionNames.ExcludeAssembliesWithoutSources))
      .Returns(false);

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object, _mockLogger.Object);

    // Act
    config.LogConfigurationSummary();

    // Assert
    _mockLogger.Verify(
      x => x.LogInformation(It.Is<string>(s => s.Contains("Single Hit: False"))),
      Times.Once);
    _mockLogger.Verify(
      x => x.LogInformation(It.Is<string>(s => s.Contains("Include Test Assembly: False"))),
      Times.Once);
    // SkipAutoProps defaults to true when not set
    _mockLogger.Verify(
      x => x.LogInformation(It.Is<string>(s => s.Contains("Skip Auto Properties: True"))),
      Times.Once);
    // ExcludeAssembliesWithoutSources defaults to true when not set
    _mockLogger.Verify(
      x => x.LogInformation(It.Is<string>(s => s.Contains("Exclude Assemblies Without Sources: True"))),
      Times.Once);
  }

  [Fact]
  public void LogConfigurationSummary_LogsHeaderAndFooter()
  {
    // Arrange
    _mockCommandLineOptions
      .Setup(x => x.TryGetOptionArgumentList(It.IsAny<string>(), out It.Ref<string[]?>.IsAny))
      .Returns(false);
    _mockCommandLineOptions
      .Setup(x => x.IsOptionSet(It.IsAny<string>()))
      .Returns(false);

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object, _mockLogger.Object);

    // Act
    config.LogConfigurationSummary();

    // Assert - Verify header and footer separators are logged
    _mockLogger.Verify(
      x => x.LogInformation(It.Is<string>(s => s == "=== Coverlet Coverage Configuration ===")),
      Times.Once);
    _mockLogger.Verify(
      x => x.LogInformation(It.Is<string>(s => s == "========================================")),
      Times.Once);
  }

  #endregion
}
