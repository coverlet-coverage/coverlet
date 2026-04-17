// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Coverlet.Core.Abstractions;
using Moq;
using Xunit;

namespace Coverlet.MTP.Configuration.Tests;

public class TestConfigParserTests
{
  private readonly Mock<IFileSystem> _mockFileSystem;

  public TestConfigParserTests()
  {
    _mockFileSystem = new Mock<IFileSystem>();
  }

  #region FindTestConfigFile Tests

  [Fact]
  public void FindTestConfigFileWithAppSpecificConfigReturnsAppSpecificPath()
  {
    // Arrange - use Path.Combine for platform-independent paths
    string testDirectory = Path.Combine("C:", "fake", "path");
    string testModulePath = Path.Combine(testDirectory, "MyTests.dll");
    string expectedConfigPath = Path.Combine(testDirectory, "MyTests.testconfig.json");

    _mockFileSystem.Setup(fs => fs.Exists(expectedConfigPath)).Returns(true);

    // Act
    string? result = TestConfigParser.FindTestConfigFile(testModulePath, _mockFileSystem.Object);

    // Assert
    Assert.Equal(expectedConfigPath, result);
    _mockFileSystem.Verify(fs => fs.Exists(expectedConfigPath), Times.Once);
  }

  [Fact]
  public void FindTestConfigFileWithGenericConfigReturnsGenericPath()
  {
    // Arrange
    string testDirectory = Path.Combine("C:", "fake", "path");
    string testModulePath = Path.Combine(testDirectory, "MyTests.dll");
    string appSpecificPath = Path.Combine(testDirectory, "MyTests.testconfig.json");
    string genericPath = Path.Combine(testDirectory, "testconfig.json");

    _mockFileSystem.Setup(fs => fs.Exists(appSpecificPath)).Returns(false);
    _mockFileSystem.Setup(fs => fs.Exists(genericPath)).Returns(true);

    // Act
    string? result = TestConfigParser.FindTestConfigFile(testModulePath, _mockFileSystem.Object);

    // Assert
    Assert.Equal(genericPath, result);
  }

  [Fact]
  public void FindTestConfigFileWithNoConfigFilesReturnsNull()
  {
    // Arrange
    string testDirectory = Path.Combine("C:", "fake", "path");
    string testModulePath = Path.Combine(testDirectory, "MyTests.dll");

    _mockFileSystem.Setup(fs => fs.Exists(It.IsAny<string>())).Returns(false);

    // Act
    string? result = TestConfigParser.FindTestConfigFile(testModulePath, _mockFileSystem.Object);

    // Assert
    Assert.Null(result);
  }

  [Fact]
  public void FindTestConfigFileWithEmptyDirectoryReturnsNull()
  {
    // Arrange - Path.GetDirectoryName returns empty for root paths on some systems
    string testModulePath = "test.dll";

    // Act
    string? result = TestConfigParser.FindTestConfigFile(testModulePath, _mockFileSystem.Object);

    // Assert
    Assert.Null(result);
  }

  [Fact]
  public void FindTestConfigFilePrefersAppSpecificOverGeneric()
  {
    // Arrange
    string testDirectory = Path.Combine("C:", "fake", "path");
    string testModulePath = Path.Combine(testDirectory, "MyTests.dll");
    string appSpecificPath = Path.Combine(testDirectory, "MyTests.testconfig.json");
    string genericPath = Path.Combine(testDirectory, "testconfig.json");

    // Both files exist, but app-specific should be preferred
    _mockFileSystem.Setup(fs => fs.Exists(appSpecificPath)).Returns(true);
    _mockFileSystem.Setup(fs => fs.Exists(genericPath)).Returns(true);

    // Act
    string? result = TestConfigParser.FindTestConfigFile(testModulePath, _mockFileSystem.Object);

    // Assert
    Assert.Equal(appSpecificPath, result);
    // Should not check generic path when app-specific exists
    _mockFileSystem.Verify(fs => fs.Exists(genericPath), Times.Never);
  }

  #endregion

  #region Parse Tests

  [Fact]
  public void ParseWithValidTestConfigReturnsParsedSettings()
  {
    // Arrange
    string testDirectory = Path.Combine("C:", "fake", "path");
    string testModulePath = Path.Combine(testDirectory, "MyTests.dll");
    string configPath = Path.Combine(testDirectory, "MyTests.testconfig.json");
    string jsonContent = """
    {
      "platformOptions": {
        "Coverlet": {
          "format": "cobertura,json",
          "include": "[MyApp.*]*",
          "exclude": "[*.Tests]*",
          "excludeByAttribute": "GeneratedCode",
          "useSourceLink": true,
          "singleHit": false,
          "includeTestAssembly": true,
          "skipAutoProps": true
        }
      }
    }
    """;

    _mockFileSystem.Setup(fs => fs.Exists(configPath)).Returns(true);
    _mockFileSystem.Setup(fs => fs.ReadAllText(configPath)).Returns(jsonContent);

    // Act
    CoverletMTPSettings? settings = TestConfigParser.Parse(testModulePath, _mockFileSystem.Object);

    // Assert
    Assert.NotNull(settings);
    Assert.Equal(testModulePath, settings.TestModule);
    Assert.Equal(["cobertura", "json"], settings.ReportFormats);
    Assert.Equal(["[MyApp.*]*"], settings.IncludeFilters);
    Assert.Equal(["[coverlet.*]*", "[*.Tests]*"], settings.ExcludeFilters); // Default + configured
    Assert.Equal(["GeneratedCode"], settings.ExcludeAttributes);
    Assert.True(settings.UseSourceLink);
    Assert.False(settings.SingleHit);
    Assert.True(settings.IncludeTestAssembly);
    Assert.True(settings.SkipAutoProps);
    Assert.True(settings.IsFromConfigFile);
  }

  [Fact]
  public void ParseWithNoCoverletSectionReturnsNull()
  {
    // Arrange
    string testDirectory = Path.Combine("C:", "fake", "path");
    string testModulePath = Path.Combine(testDirectory, "MyTests.dll");
    string configPath = Path.Combine(testDirectory, "MyTests.testconfig.json");
    string jsonContent = """
    {
      "platformOptions": {
        "SomeOtherExtension": {
          "setting": "value"
        }
      }
    }
    """;

    _mockFileSystem.Setup(fs => fs.Exists(configPath)).Returns(true);
    _mockFileSystem.Setup(fs => fs.ReadAllText(configPath)).Returns(jsonContent);

    // Act
    CoverletMTPSettings? settings = TestConfigParser.Parse(testModulePath, _mockFileSystem.Object);

    // Assert
    Assert.Null(settings);
  }

  [Fact]
  public void ParseWithEmptyPlatformOptionsReturnsNull()
  {
    // Arrange
    string testDirectory = Path.Combine("C:", "fake", "path");
    string testModulePath = Path.Combine(testDirectory, "MyTests.dll");
    string configPath = Path.Combine(testDirectory, "MyTests.testconfig.json");
    string jsonContent = """
    {
      "platformOptions": {}
    }
    """;

    _mockFileSystem.Setup(fs => fs.Exists(configPath)).Returns(true);
    _mockFileSystem.Setup(fs => fs.ReadAllText(configPath)).Returns(jsonContent);

    // Act
    CoverletMTPSettings? settings = TestConfigParser.Parse(testModulePath, _mockFileSystem.Object);

    // Assert
    Assert.Null(settings);
  }

  [Fact]
  public void ParseWithNoConfigFileReturnsNull()
  {
    // Arrange
    string testDirectory = Path.Combine("C:", "fake", "path");
    string testModulePath = Path.Combine(testDirectory, "MyTests.dll");

    _mockFileSystem.Setup(fs => fs.Exists(It.IsAny<string>())).Returns(false);

    // Act
    CoverletMTPSettings? settings = TestConfigParser.Parse(testModulePath, _mockFileSystem.Object);

    // Assert
    Assert.Null(settings);
  }

  [Theory]
  [InlineData("format", "opencover", "opencover")]
  [InlineData("include", "[MyApp]*", "[MyApp]*")]
  [InlineData("excludeByFile", "**/Generated/**", "**/Generated/**")]
  public void ParseWithLowercaseKeysMapsCorrectly(string key, string value, string expected)
  {
    // Arrange
    string testDirectory = Path.Combine("C:", "fake", "path");
    string testModulePath = Path.Combine(testDirectory, "MyTests.dll");
    string configPath = Path.Combine(testDirectory, "MyTests.testconfig.json");
    string jsonContent = $$"""
    {
      "platformOptions": {
        "Coverlet": {
          "{{key}}": "{{value}}"
        }
      }
    }
    """;

    _mockFileSystem.Setup(fs => fs.Exists(configPath)).Returns(true);
    _mockFileSystem.Setup(fs => fs.ReadAllText(configPath)).Returns(jsonContent);

    // Act
    CoverletMTPSettings? settings = TestConfigParser.Parse(testModulePath, _mockFileSystem.Object);

    // Assert
    Assert.NotNull(settings);

    switch (key)
    {
      case "format":
        Assert.Contains(expected, settings.ReportFormats);
        break;
      case "include":
        Assert.Contains(expected, settings.IncludeFilters);
        break;
      case "excludeByFile":
        Assert.Contains(expected, settings.ExcludeSourceFiles);
        break;
    }
  }

  [Fact]
  public void ParseWithAllSettingsParsesAllCorrectly()
  {
    // Arrange
    string testDirectory = Path.Combine("C:", "fake", "path");
    string testModulePath = Path.Combine(testDirectory, "MyTests.dll");
    string configPath = Path.Combine(testDirectory, "MyTests.testconfig.json");
    string jsonContent = """
    {
      "platformOptions": {
        "Coverlet": {
          "include": "[MyApp.*]*",
          "includeDirectory": "/path/to/include",
          "exclude": "[*.Tests]*",
          "excludeByFile": "**/Migrations/*.cs",
          "excludeByAttribute": "GeneratedCode,ExcludeFromCodeCoverage",
          "mergeWith": "/path/to/merge.json",
          "useSourceLink": true,
          "singleHit": true,
          "includeTestAssembly": true,
          "format": "cobertura,json,opencover",
          "skipAutoProps": true,
          "doesNotReturnAttribute": "DoesNotReturnAttribute",
          "deterministicReport": true,
          "excludeAssembliesWithoutSources": "MissingAll",
          "disableManagedInstrumentationRestore": true
        }
      }
    }
    """;

    _mockFileSystem.Setup(fs => fs.Exists(configPath)).Returns(true);
    _mockFileSystem.Setup(fs => fs.ReadAllText(configPath)).Returns(jsonContent);

    // Act
    CoverletMTPSettings? settings = TestConfigParser.Parse(testModulePath, _mockFileSystem.Object);

    // Assert
    Assert.NotNull(settings);
    Assert.Equal(["[MyApp.*]*"], settings.IncludeFilters);
    Assert.Equal(["/path/to/include"], settings.IncludeDirectories);
    Assert.Equal(["[coverlet.*]*", "[*.Tests]*"], settings.ExcludeFilters);
    Assert.Equal(["**/Migrations/*.cs"], settings.ExcludeSourceFiles);
    Assert.Equal(["GeneratedCode", "ExcludeFromCodeCoverage"], settings.ExcludeAttributes);
    Assert.Equal("/path/to/merge.json", settings.MergeWith);
    Assert.True(settings.UseSourceLink);
    Assert.True(settings.SingleHit);
    Assert.True(settings.IncludeTestAssembly);
    Assert.Equal(["cobertura", "json", "opencover"], settings.ReportFormats);
    Assert.True(settings.SkipAutoProps);
    Assert.Equal(["DoesNotReturnAttribute"], settings.DoesNotReturnAttributes);
    Assert.True(settings.DeterministicReport);
    Assert.Equal("MissingAll", settings.ExcludeAssembliesWithoutSources);
    Assert.True(settings.DisableManagedInstrumentationRestore);
  }

  [Fact]
  public void ParseWithGenericTestConfigParsesCorrectly()
  {
    // Arrange
    string testDirectory = Path.Combine("C:", "fake", "path");
    string testModulePath = Path.Combine(testDirectory, "MyTests.dll");
    string appSpecificPath = Path.Combine(testDirectory, "MyTests.testconfig.json");
    string genericPath = Path.Combine(testDirectory, "testconfig.json");
    string jsonContent = """
    {
      "platformOptions": {
        "Coverlet": {
          "format": "lcov"
        }
      }
    }
    """;

    _mockFileSystem.Setup(fs => fs.Exists(appSpecificPath)).Returns(false);
    _mockFileSystem.Setup(fs => fs.Exists(genericPath)).Returns(true);
    _mockFileSystem.Setup(fs => fs.ReadAllText(genericPath)).Returns(jsonContent);

    // Act
    CoverletMTPSettings? settings = TestConfigParser.Parse(testModulePath, _mockFileSystem.Object);

    // Assert
    Assert.NotNull(settings);
    Assert.Equal(["lcov"], settings.ReportFormats);
  }

  [Fact]
  public void ParseWithPascalCaseKeysMapsCorrectly()
  {
    // This test uses the exact JSON format the user reported as not working
    // PascalCase keys like "Exclude", "ExcludeByAttribute", "Format", etc.
    string testDirectory = Path.Combine("C:", "fake", "path");
    string testModulePath = Path.Combine(testDirectory, "XUnitProject3.Tests.dll");
    string configPath = Path.Combine(testDirectory, "XUnitProject3.Tests.testconfig.json");

    // This is the exact JSON format from the user's report
    string jsonContent = """
    {
      "platformOptions": {
        "Coverlet": {
          "Exclude": "[*.Tests]*,[xunit*]*",
          "ExcludeByAttribute": "GeneratedCode,ExcludeFromCodeCoverage",
          "Format": "cobertura,json,lcov,opencover",
          "SkipAutoProps": true,
          "ExcludeAssembliesWithoutSources": "MissingAll"
        }
      }
    }
    """;

    _mockFileSystem.Setup(fs => fs.Exists(configPath)).Returns(true);
    _mockFileSystem.Setup(fs => fs.ReadAllText(configPath)).Returns(jsonContent);

    // Act
    CoverletMTPSettings? settings = TestConfigParser.Parse(testModulePath, _mockFileSystem.Object);

    // Assert
    Assert.NotNull(settings);
    Assert.Equal(["[coverlet.*]*", "[*.Tests]*", "[xunit*]*"], settings.ExcludeFilters);
    Assert.Equal(["GeneratedCode", "ExcludeFromCodeCoverage"], settings.ExcludeAttributes);
    Assert.Equal(["cobertura", "json", "lcov", "opencover"], settings.ReportFormats);
    Assert.True(settings.SkipAutoProps);
    Assert.Equal("MissingAll", settings.ExcludeAssembliesWithoutSources);
    Assert.True(settings.IsFromConfigFile);
  }

  #endregion
}
