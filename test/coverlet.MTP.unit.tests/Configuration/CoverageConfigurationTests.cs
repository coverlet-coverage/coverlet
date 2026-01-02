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

    // Setup default behavior for TryGetOptionArgumentList to return false
    _mockCommandLineOptions
      .Setup(x => x.TryGetOptionArgumentList(It.IsAny<string>(), out It.Ref<string[]?>.IsAny))
      .Returns(false);

    // Setup default behavior for IsOptionSet to return false
    _mockCommandLineOptions
      .Setup(x => x.IsOptionSet(It.IsAny<string>()))
      .Returns(false);
  }

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

  [Fact]
  public void Constructor_WithNullLogger_CreatesInstance()
  {
    // Act
    var config = new CoverageConfiguration(_mockCommandLineOptions.Object, logger: null);

    // Assert
    Assert.NotNull(config);
  }

  #endregion

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
    _mockCommandLineOptions.Verify(x => x.IsOptionSet(CoverletOptionNames.Coverage), Times.Once);
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

  [Fact]
  public void IsCoverageEnabled_AccessedMultipleTimes_QueriesCommandLineEachTime()
  {
    // Arrange
    _mockCommandLineOptions
      .Setup(x => x.IsOptionSet(CoverletOptionNames.Coverage))
      .Returns(true);

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object);

    // Act
    _ = config.IsCoverageEnabled;
    _ = config.IsCoverageEnabled;

    // Assert
    _mockCommandLineOptions.Verify(x => x.IsOptionSet(CoverletOptionNames.Coverage), Times.Exactly(2));
  }

  #endregion

  #region GetOutputFormats Tests

  [Fact]
  public void GetOutputFormats_WhenNotSpecified_ReturnsDefaultFormats()
  {
    // Arrange
    var config = new CoverageConfiguration(_mockCommandLineOptions.Object);

    // Act
    string[] result = config.GetOutputFormats();

    // Assert
    Assert.Equal(2, result.Length);
    Assert.Contains("json", result);
    Assert.Contains("cobertura", result);
  }

  [Fact]
  public void GetOutputFormats_WhenSpecified_ReturnsSpecifiedFormats()
  {
    // Arrange
    string[]? formats = new[] { "lcov", "opencover" };
    _mockCommandLineOptions
      .Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.Formats, out formats))
      .Returns(true);

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object);

    // Act
    string[] result = config.GetOutputFormats();

    // Assert
    Assert.Equal(2, result.Length);
    Assert.Equal("lcov", result[0]);
    Assert.Equal("opencover", result[1]);
  }

  [Fact]
  public void GetOutputFormats_WithLogger_LogsDefaultFormats()
  {
    // Arrange
    var config = new CoverageConfiguration(_mockCommandLineOptions.Object, _mockLogger.Object);

    // Act
    string[] result = config.GetOutputFormats();

    // Assert
    Assert.Contains("json", result);
    Assert.Contains("cobertura", result);
    _mockLogger.Verify(
      x => x.Log(
        LogLevel.Debug,
        It.IsAny<string>(),
        It.IsAny<Exception?>(),
        It.IsAny<Func<string, Exception?, string>>()),
      Times.AtLeastOnce);
  }

  [Fact]
  public void GetOutputFormats_WithLoggerAndExplicitFormats_LogsExplicitFormats()
  {
    // Arrange
    string[]? formats = new[] { "json" };
    _mockCommandLineOptions
      .Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.Formats, out formats))
      .Returns(true);

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object, _mockLogger.Object);

    // Act
    string[] result = config.GetOutputFormats();

    // Assert
    Assert.Single(result);
    Assert.Equal("json", result[0]);
  }

  #endregion

  #region GetIncludeFilters Tests

  [Fact]
  public void GetIncludeFilters_WhenNotSpecified_ReturnsEmptyArray()
  {
    // Arrange
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

  [Fact]
  public void GetIncludeFilters_WithSingleFilter_ReturnsSingleFilter()
  {
    // Arrange
    string[]? filters = new[] { "[MyAssembly]*" };
    _mockCommandLineOptions
      .Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.Include, out filters))
      .Returns(true);

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object);

    // Act
    string[] result = config.GetIncludeFilters();

    // Assert
    Assert.Single(result);
    Assert.Equal("[MyAssembly]*", result[0]);
  }

  [Fact]
  public void GetIncludeFilters_WithLogger_LogsFilters()
  {
    // Arrange
    string[]? filters = new[] { "[MyAssembly]*" };
    _mockCommandLineOptions
      .Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.Include, out filters))
      .Returns(true);

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object, _mockLogger.Object);

    // Act
    string[] result = config.GetIncludeFilters();

    // Assert
    Assert.Single(result);
    _mockLogger.Verify(
      x => x.Log(
        LogLevel.Debug,
        It.IsAny<string>(),
        It.IsAny<Exception?>(),
        It.IsAny<Func<string, Exception?, string>>()),
      Times.AtLeastOnce);
  }

  #endregion

  #region GetExcludeFilters Tests

  [Fact]
  public void GetExcludeFilters_WhenNotSpecified_ReturnsDefaultExclusions()
  {
    // Arrange
    var config = new CoverageConfiguration(_mockCommandLineOptions.Object);

    // Act
    string[] result = config.GetExcludeFilters();

    // Assert
    Assert.Contains("[xunit.*]*", result);
    Assert.Contains("[Microsoft.Testing.*]*", result);
    Assert.Contains("[coverlet.*]*", result);
    Assert.Equal(3, result.Length);
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
    Assert.Equal(4, result.Length);
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
    Assert.Equal(4, result.Length); // 3 defaults + 1 custom (duplicate removed)
  }

  [Fact]
  public void GetExcludeFilters_WithMultipleCustomFilters_MergesAllWithDefaults()
  {
    // Arrange
    string[]? filters = new[] { "[MyTests]*", "[AnotherTests]*", "[MoreTests]*" };
    _mockCommandLineOptions
      .Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.Exclude, out filters))
      .Returns(true);

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object);

    // Act
    string[] result = config.GetExcludeFilters();

    // Assert
    Assert.Equal(6, result.Length); // 3 defaults + 3 custom
  }

  [Fact]
  public void GetExcludeFilters_WithLogger_LogsFilters()
  {
    // Arrange
    var config = new CoverageConfiguration(_mockCommandLineOptions.Object, _mockLogger.Object);

    // Act
    string[] result = config.GetExcludeFilters();

    // Assert
    Assert.NotEmpty(result);
    _mockLogger.Verify(
      x => x.Log(
        LogLevel.Debug,
        It.IsAny<string>(),
        It.IsAny<Exception?>(),
        It.IsAny<Func<string, Exception?, string>>()),
      Times.AtLeastOnce);
  }

  #endregion

  #region GetExcludeByFileFilters Tests

  [Fact]
  public void GetExcludeByFileFilters_WhenNotSpecified_ReturnsEmptyArray()
  {
    // Arrange
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

  [Fact]
  public void GetExcludeByFileFilters_WithLogger_LogsPatterns()
  {
    // Arrange
    string[]? patterns = new[] { "**/Generated/*.cs" };
    _mockCommandLineOptions
      .Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.ExcludeByFile, out patterns))
      .Returns(true);

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object, _mockLogger.Object);

    // Act
    string[] result = config.GetExcludeByFileFilters();

    // Assert
    Assert.Single(result);
    _mockLogger.Verify(
      x => x.Log(
        LogLevel.Debug,
        It.IsAny<string>(),
        It.IsAny<Exception?>(),
        It.IsAny<Func<string, Exception?, string>>()),
      Times.AtLeastOnce);
  }

  #endregion

  #region GetExcludeByAttributeFilters Tests

  [Fact]
  public void GetExcludeByAttributeFilters_WhenNotSpecified_ReturnsDefaultAttributes()
  {
    // Arrange
    var config = new CoverageConfiguration(_mockCommandLineOptions.Object);

    // Act
    string[] result = config.GetExcludeByAttributeFilters();

    // Assert
    Assert.Contains("ExcludeFromCodeCoverage", result);
    Assert.Contains("ExcludeFromCodeCoverageAttribute", result);
    Assert.Contains("GeneratedCodeAttribute", result);
    Assert.Contains("CompilerGeneratedAttribute", result);
    Assert.Equal(4, result.Length);
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
    Assert.Equal(5, result.Length);
  }

  [Fact]
  public void GetExcludeByAttributeFilters_WhenSpecifiedWithDuplicates_ReturnsDistinctValues()
  {
    // Arrange
    string[]? attributes = new[] { "ExcludeFromCodeCoverage", "CustomAttribute" };
    _mockCommandLineOptions
      .Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.ExcludeByAttribute, out attributes))
      .Returns(true);

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object);

    // Act
    string[] result = config.GetExcludeByAttributeFilters();

    // Assert
    int excludeCount = result.Count(a => a == "ExcludeFromCodeCoverage");
    Assert.Equal(1, excludeCount);
    Assert.Contains("CustomAttribute", result);
  }

  [Fact]
  public void GetExcludeByAttributeFilters_WithLogger_LogsAttributes()
  {
    // Arrange
    var config = new CoverageConfiguration(_mockCommandLineOptions.Object, _mockLogger.Object);

    // Act
    string[] result = config.GetExcludeByAttributeFilters();

    // Assert
    Assert.NotEmpty(result);
    _mockLogger.Verify(
      x => x.Log(
        LogLevel.Debug,
        It.IsAny<string>(),
        It.IsAny<Exception?>(),
        It.IsAny<Func<string, Exception?, string>>()),
      Times.AtLeastOnce);
  }

  #endregion

  #region GetIncludeDirectories Tests

  [Fact]
  public void GetIncludeDirectories_WhenNotSpecified_ReturnsEmptyArray()
  {
    // Arrange
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

  [Fact]
  public void GetIncludeDirectories_WithLogger_LogsDirectories()
  {
    // Arrange
    string[]? directories = new[] { "/path/to/libs" };
    _mockCommandLineOptions
      .Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.IncludeDirectory, out directories))
      .Returns(true);

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object, _mockLogger.Object);

    // Act
    string[] result = config.GetIncludeDirectories();

    // Assert
    Assert.Single(result);
    _mockLogger.Verify(
      x => x.Log(
        LogLevel.Debug,
        It.IsAny<string>(),
        It.IsAny<Exception?>(),
        It.IsAny<Func<string, Exception?, string>>()),
      Times.AtLeastOnce);
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
    // Arrange - default setup returns false for IsOptionSet
    var config = new CoverageConfiguration(_mockCommandLineOptions.Object);

    // Act
    bool result = config.SkipAutoProps;

    // Assert
    Assert.True(result); // Default is true
  }

  [Fact]
  public void SkipAutoProps_WithLogger_LogsValue()
  {
    // Arrange
    _mockCommandLineOptions
      .Setup(x => x.IsOptionSet(CoverletOptionNames.SkipAutoProps))
      .Returns(true);

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object, _mockLogger.Object);

    // Act
    bool result = config.SkipAutoProps;

    // Assert
    Assert.True(result);
    _mockLogger.Verify(
      x => x.Log(
        LogLevel.Debug,
        It.IsAny<string>(),
        It.IsAny<Exception?>(),
        It.IsAny<Func<string, Exception?, string>>()),
      Times.AtLeastOnce);
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
    // Arrange - default setup returns false for IsOptionSet
    var config = new CoverageConfiguration(_mockCommandLineOptions.Object);

    // Act
    bool result = config.ExcludeAssembliesWithoutSources;

    // Assert
    Assert.True(result); // Default is true
  }

  [Fact]
  public void ExcludeAssembliesWithoutSources_WithLogger_LogsValue()
  {
    // Arrange
    _mockCommandLineOptions
      .Setup(x => x.IsOptionSet(CoverletOptionNames.ExcludeAssembliesWithoutSources))
      .Returns(true);

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object, _mockLogger.Object);

    // Act
    bool result = config.ExcludeAssembliesWithoutSources;

    // Assert
    Assert.True(result);
    _mockLogger.Verify(
      x => x.Log(
        LogLevel.Debug,
        It.IsAny<string>(),
        It.IsAny<Exception?>(),
        It.IsAny<Func<string, Exception?, string>>()),
      Times.AtLeastOnce);
  }

  #endregion

  #region GetDoesNotReturnAttributes Tests

  [Fact]
  public void GetDoesNotReturnAttributes_WhenNotSpecified_ReturnsEmptyArray()
  {
    // Arrange
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

  [Fact]
  public void GetDoesNotReturnAttributes_WithLogger_LogsAttributes()
  {
    // Arrange
    string[]? attributes = new[] { "DoesNotReturnAttribute" };
    _mockCommandLineOptions
      .Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.DoesNotReturnAttribute, out attributes))
      .Returns(true);

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object, _mockLogger.Object);

    // Act
    string[] result = config.GetDoesNotReturnAttributes();

    // Assert
    Assert.Single(result);
    _mockLogger.Verify(
      x => x.Log(
        LogLevel.Debug,
        It.IsAny<string>(),
        It.IsAny<Exception?>(),
        It.IsAny<Func<string, Exception?, string>>()),
      Times.AtLeastOnce);
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
  public void GetTestAssemblyPath_ReturnsExistingPathOrDirectory()
  {
    // Act
    string result = CoverageConfiguration.GetTestAssemblyPath();

    // Assert
    // Should return either a file that exists or a directory that exists
    Assert.True(File.Exists(result) || Directory.Exists(result));
  }

  [Fact]
  public void GetTestAssemblyPath_IsStatic_CanBeCalledWithoutInstance()
  {
    // Act & Assert - Should not throw
    string result = CoverageConfiguration.GetTestAssemblyPath();
    Assert.NotNull(result);
  }

  #endregion

  #region LogConfigurationSummary Tests

  [Fact]
  public void LogConfigurationSummary_WithNullLogger_DoesNotThrow()
  {
    // Arrange
    var config = new CoverageConfiguration(_mockCommandLineOptions.Object, logger: null);

    // Act & Assert - Should not throw
    var exception = Record.Exception(() => config.LogConfigurationSummary());
    Assert.Null(exception);
  }

  [Fact]
  public void LogConfigurationSummary_WithLogger_LogsHeader()
  {
    // Arrange
    var config = new CoverageConfiguration(_mockCommandLineOptions.Object, _mockLogger.Object);

    // Act
    config.LogConfigurationSummary();

    // Assert
    _mockLogger.Verify(
      x => x.Log(
        LogLevel.Information,
        It.Is<string>(s => s.Contains("=== Coverlet Coverage Configuration ===")),
        It.IsAny<Exception?>(),
        It.IsAny<Func<string, Exception?, string>>()),
      Times.Once);
  }

  [Fact]
  public void LogConfigurationSummary_WithLogger_LogsFooter()
  {
    // Arrange
    var config = new CoverageConfiguration(_mockCommandLineOptions.Object, _mockLogger.Object);

    // Act
    config.LogConfigurationSummary();

    // Assert
    _mockLogger.Verify(
      x => x.Log(
        LogLevel.Information,
        It.Is<string>(s => s.Contains("========================================")),
        It.IsAny<Exception?>(),
        It.IsAny<Func<string, Exception?, string>>()),
      Times.Once);
  }

  [Fact]
  public void LogConfigurationSummary_WithLogger_LogsTestAssemblyPath()
  {
    // Arrange
    var config = new CoverageConfiguration(_mockCommandLineOptions.Object, _mockLogger.Object);

    // Act
    config.LogConfigurationSummary();

    // Assert
    _mockLogger.Verify(
      x => x.Log(
        LogLevel.Information,
        It.Is<string>(s => s.StartsWith("Test Assembly:")),
        It.IsAny<Exception?>(),
        It.IsAny<Func<string, Exception?, string>>()),
      Times.Once);
  }

  [Fact]
  public void LogConfigurationSummary_WithLogger_LogsOutputFormats()
  {
    // Arrange
    var config = new CoverageConfiguration(_mockCommandLineOptions.Object, _mockLogger.Object);

    // Act
    config.LogConfigurationSummary();

    // Assert
    _mockLogger.Verify(
      x => x.Log(
        LogLevel.Information,
        It.Is<string>(s => s.StartsWith("Output Formats:") && s.Contains("json") && s.Contains("cobertura")),
        It.IsAny<Exception?>(),
        It.IsAny<Func<string, Exception?, string>>()),
      Times.Once);
  }

  [Fact]
  public void LogConfigurationSummary_WithLogger_LogsIncludeFiltersAsNone()
  {
    // Arrange
    var config = new CoverageConfiguration(_mockCommandLineOptions.Object, _mockLogger.Object);

    // Act
    config.LogConfigurationSummary();

    // Assert
    _mockLogger.Verify(
      x => x.Log(
        LogLevel.Information,
        It.Is<string>(s => s.StartsWith("Include Filters:") && s.Contains("(none)")),
        It.IsAny<Exception?>(),
        It.IsAny<Func<string, Exception?, string>>()),
      Times.Once);
  }

  [Fact]
  public void LogConfigurationSummary_WithLogger_LogsExcludeFilters()
  {
    // Arrange
    var config = new CoverageConfiguration(_mockCommandLineOptions.Object, _mockLogger.Object);

    // Act
    config.LogConfigurationSummary();

    // Assert
    _mockLogger.Verify(
      x => x.Log(
        LogLevel.Information,
        It.Is<string>(s => s.StartsWith("Exclude Filters:") && s.Contains("[xunit.*]*")),
        It.IsAny<Exception?>(),
        It.IsAny<Func<string, Exception?, string>>()),
      Times.Once);
  }

  [Fact]
  public void LogConfigurationSummary_WithLogger_LogsExcludeByFile()
  {
    // Arrange
    var config = new CoverageConfiguration(_mockCommandLineOptions.Object, _mockLogger.Object);

    // Act
    config.LogConfigurationSummary();

    // Assert
    _mockLogger.Verify(
      x => x.Log(
        LogLevel.Information,
        It.Is<string>(s => s.StartsWith("Exclude by File:")),
        It.IsAny<Exception?>(),
        It.IsAny<Func<string, Exception?, string>>()),
      Times.Once);
  }

  [Fact]
  public void LogConfigurationSummary_WithLogger_LogsExcludeByAttribute()
  {
    // Arrange
    var config = new CoverageConfiguration(_mockCommandLineOptions.Object, _mockLogger.Object);

    // Act
    config.LogConfigurationSummary();

    // Assert
    _mockLogger.Verify(
      x => x.Log(
        LogLevel.Information,
        It.Is<string>(s => s.StartsWith("Exclude by Attribute:") && s.Contains("ExcludeFromCodeCoverage")),
        It.IsAny<Exception?>(),
        It.IsAny<Func<string, Exception?, string>>()),
      Times.Once);
  }

  [Fact]
  public void LogConfigurationSummary_WithLogger_LogsIncludeDirectories()
  {
    // Arrange
    var config = new CoverageConfiguration(_mockCommandLineOptions.Object, _mockLogger.Object);

    // Act
    config.LogConfigurationSummary();

    // Assert
    _mockLogger.Verify(
      x => x.Log(
        LogLevel.Information,
        It.Is<string>(s => s.StartsWith("Include Directories:")),
        It.IsAny<Exception?>(),
        It.IsAny<Func<string, Exception?, string>>()),
      Times.Once);
  }

  [Fact]
  public void LogConfigurationSummary_WithLogger_LogsSingleHit()
  {
    // Arrange
    var config = new CoverageConfiguration(_mockCommandLineOptions.Object, _mockLogger.Object);

    // Act
    config.LogConfigurationSummary();

    // Assert
    _mockLogger.Verify(
      x => x.Log(
        LogLevel.Information,
        It.Is<string>(s => s.StartsWith("Single Hit:")),
        It.IsAny<Exception?>(),
        It.IsAny<Func<string, Exception?, string>>()),
      Times.Once);
  }

  [Fact]
  public void LogConfigurationSummary_WithLogger_LogsIncludeTestAssembly()
  {
    // Arrange
    var config = new CoverageConfiguration(_mockCommandLineOptions.Object, _mockLogger.Object);

    // Act
    config.LogConfigurationSummary();

    // Assert
    _mockLogger.Verify(
      x => x.Log(
        LogLevel.Information,
        It.Is<string>(s => s.StartsWith("Include Test Assembly:")),
        It.IsAny<Exception?>(),
        It.IsAny<Func<string, Exception?, string>>()),
      Times.Once);
  }

  [Fact]
  public void LogConfigurationSummary_WithLogger_LogsSkipAutoProperties()
  {
    // Arrange
    var config = new CoverageConfiguration(_mockCommandLineOptions.Object, _mockLogger.Object);

    // Act
    config.LogConfigurationSummary();

    // Assert
    _mockLogger.Verify(
      x => x.Log(
        LogLevel.Information,
        It.Is<string>(s => s.StartsWith("Skip Auto Properties:")),
        It.IsAny<Exception?>(),
        It.IsAny<Func<string, Exception?, string>>()),
      Times.Once);
  }

  [Fact]
  public void LogConfigurationSummary_WithLogger_LogsExcludeAssembliesWithoutSources()
  {
    // Arrange
    var config = new CoverageConfiguration(_mockCommandLineOptions.Object, _mockLogger.Object);

    // Act
    config.LogConfigurationSummary();

    // Assert
    _mockLogger.Verify(
      x => x.Log(
        LogLevel.Information,
        It.Is<string>(s => s.StartsWith("Exclude Assemblies Without Sources:")),
        It.IsAny<Exception?>(),
        It.IsAny<Func<string, Exception?, string>>()),
      Times.Once);
  }

  [Fact]
  public void LogConfigurationSummary_WithCustomFormats_LogsCustomFormats()
  {
    // Arrange
    string[]? formats = new[] { "lcov", "opencover" };
    _mockCommandLineOptions
      .Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.Formats, out formats))
      .Returns(true);

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object, _mockLogger.Object);

    // Act
    config.LogConfigurationSummary();

    // Assert
    _mockLogger.Verify(
      x => x.Log(
        LogLevel.Information,
        It.Is<string>(s => s.Contains("lcov") && s.Contains("opencover")),
        It.IsAny<Exception?>(),
        It.IsAny<Func<string, Exception?, string>>()),
      Times.Once);
  }

  [Fact]
  public void LogConfigurationSummary_WithIncludeFilters_LogsFilters()
  {
    // Arrange
    string[]? filters = new[] { "[MyAssembly]*" };
    _mockCommandLineOptions
      .Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.Include, out filters))
      .Returns(true);

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object, _mockLogger.Object);

    // Act
    config.LogConfigurationSummary();

    // Assert
    _mockLogger.Verify(
      x => x.Log(
        LogLevel.Information,
        It.Is<string>(s => s.StartsWith("Include Filters:") && s.Contains("[MyAssembly]*")),
        It.IsAny<Exception?>(),
        It.IsAny<Func<string, Exception?, string>>()),
      Times.Once);
  }

  [Fact]
  public void LogConfigurationSummary_WithSingleHitTrue_LogsTrueValue()
  {
    // Arrange
    _mockCommandLineOptions
      .Setup(x => x.IsOptionSet(CoverletOptionNames.SingleHit))
      .Returns(true);

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object, _mockLogger.Object);

    // Act
    config.LogConfigurationSummary();

    // Assert
    _mockLogger.Verify(
      x => x.Log(
        LogLevel.Information,
        It.Is<string>(s => s == "Single Hit: True"),
        It.IsAny<Exception?>(),
        It.IsAny<Func<string, Exception?, string>>()),
      Times.Once);
  }

  [Fact]
  public void LogConfigurationSummary_WithIncludeTestAssemblyTrue_LogsTrueValue()
  {
    // Arrange
    _mockCommandLineOptions
      .Setup(x => x.IsOptionSet(CoverletOptionNames.IncludeTestAssembly))
      .Returns(true);

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object, _mockLogger.Object);

    // Act
    config.LogConfigurationSummary();

    // Assert
    _mockLogger.Verify(
      x => x.Log(
        LogLevel.Information,
        It.Is<string>(s => s == "Include Test Assembly: True"),
        It.IsAny<Exception?>(),
        It.IsAny<Func<string, Exception?, string>>()),
      Times.Once);
  }

  [Fact]
  public void LogConfigurationSummary_LogsAllLines()
  {
    // Arrange
    var config = new CoverageConfiguration(_mockCommandLineOptions.Object, _mockLogger.Object);

    // Act
    config.LogConfigurationSummary();

    // Assert - Should log at least 13 lines (header + 11 config lines + footer)
    _mockLogger.Verify(
      x => x.Log(
        LogLevel.Information,
        It.IsAny<string>(),
        It.IsAny<Exception?>(),
        It.IsAny<Func<string, Exception?, string>>()),
      Times.AtLeast(13));
  }

  #endregion

  #region Integration Tests

  [Fact]
  public void AllMethodsCalled_InSequence_WorkCorrectly()
  {
    // Arrange
    _mockCommandLineOptions
      .Setup(x => x.IsOptionSet(CoverletOptionNames.Coverage))
      .Returns(true);

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object, _mockLogger.Object);

    // Act & Assert - Call all methods in sequence
    Assert.True(config.IsCoverageEnabled);
    Assert.NotEmpty(config.GetOutputFormats());
    Assert.Empty(config.GetIncludeFilters());
    Assert.NotEmpty(config.GetExcludeFilters());
    Assert.Empty(config.GetExcludeByFileFilters());
    Assert.NotEmpty(config.GetExcludeByAttributeFilters());
    Assert.Empty(config.GetIncludeDirectories());
    Assert.False(config.UseSingleHit);
    Assert.False(config.IncludeTestAssembly);
    Assert.True(config.SkipAutoProps);
    Assert.True(config.ExcludeAssembliesWithoutSources);
    Assert.Empty(config.GetDoesNotReturnAttributes());
    Assert.NotEmpty(CoverageConfiguration.GetTestAssemblyPath());

    // Should not throw
    config.LogConfigurationSummary();
  }

  [Fact]
  public void AllOptionsSpecified_ReturnsSpecifiedValues()
  {
    // Arrange
    string[]? formats = new[] { "json" };
    string[]? includeFilters = new[] { "[MyAssembly]*" };
    string[]? excludeFilters = new[] { "[TestAssembly]*" };
    string[]? excludeByFile = new[] { "**/Generated/*.cs" };
    string[]? excludeByAttribute = new[] { "ObsoleteAttribute" };
    string[]? includeDirectories = new[] { "/path/to/libs" };
    string[]? doesNotReturnAttributes = new[] { "DoesNotReturnAttribute" };

    _mockCommandLineOptions.Setup(x => x.IsOptionSet(CoverletOptionNames.Coverage)).Returns(true);
    _mockCommandLineOptions.Setup(x => x.IsOptionSet(CoverletOptionNames.SingleHit)).Returns(true);
    _mockCommandLineOptions.Setup(x => x.IsOptionSet(CoverletOptionNames.IncludeTestAssembly)).Returns(true);
    _mockCommandLineOptions.Setup(x => x.IsOptionSet(CoverletOptionNames.SkipAutoProps)).Returns(true);
    _mockCommandLineOptions.Setup(x => x.IsOptionSet(CoverletOptionNames.ExcludeAssembliesWithoutSources)).Returns(true);
    _mockCommandLineOptions.Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.Formats, out formats)).Returns(true);
    _mockCommandLineOptions.Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.Include, out includeFilters)).Returns(true);
    _mockCommandLineOptions.Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.Exclude, out excludeFilters)).Returns(true);
    _mockCommandLineOptions.Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.ExcludeByFile, out excludeByFile)).Returns(true);
    _mockCommandLineOptions.Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.ExcludeByAttribute, out excludeByAttribute)).Returns(true);
    _mockCommandLineOptions.Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.IncludeDirectory, out includeDirectories)).Returns(true);
    _mockCommandLineOptions.Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.DoesNotReturnAttribute, out doesNotReturnAttributes)).Returns(true);

    var config = new CoverageConfiguration(_mockCommandLineOptions.Object);

    // Act & Assert
    Assert.True(config.IsCoverageEnabled);
    Assert.Single(config.GetOutputFormats());
    Assert.Single(config.GetIncludeFilters());
    Assert.True(config.GetExcludeFilters().Length > 1); // Merged with defaults
    Assert.Single(config.GetExcludeByFileFilters());
    Assert.True(config.GetExcludeByAttributeFilters().Length > 1); // Merged with defaults
    Assert.Single(config.GetIncludeDirectories());
    Assert.True(config.UseSingleHit);
    Assert.True(config.IncludeTestAssembly);
    Assert.True(config.SkipAutoProps);
    Assert.True(config.ExcludeAssembliesWithoutSources);
    Assert.Single(config.GetDoesNotReturnAttributes());
  }

  #endregion
}
