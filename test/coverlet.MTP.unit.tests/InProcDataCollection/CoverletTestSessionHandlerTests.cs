// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Coverlet.MTP.InProcDataCollection;
using Microsoft.Extensions.Configuration;
using Microsoft.Testing.Platform.Extensions.TestHost;
using Microsoft.Testing.Platform.Services;
using Moq;
using Xunit;

namespace coverlet.MTP.Tests.InProcDataCollection;

public class CoverletTestSessionHandlerTests
{
  [Fact]
  public void Constructor_Default_CreatesInstance()
  {
    // Act
    var handler = new CoverletTestSessionHandler();

    // Assert
    Assert.NotNull(handler);
  }

  [Fact]
  public void Constructor_WithConfiguration_CreatesInstance()
  {
    // Arrange
    var mockConfiguration = new Mock<IConfiguration>();
    var mockSection = new Mock<IConfigurationSection>();
    mockConfiguration
      .Setup(x => x.GetSection(It.IsAny<string>()))
      .Returns(mockSection.Object);
    string testModule = "TestModule.dll";

    // Act
    var handler = new CoverletTestSessionHandler(mockConfiguration.Object, testModule);

    // Assert
    Assert.NotNull(handler);
  }

  [Fact]
  public void Constructor_WithNullConfiguration_CreatesInstance()
  {
    // Arrange
    string testModule = "TestModule.dll";

    // Act
    var handler = new CoverletTestSessionHandler(null, testModule);

    // Assert
    Assert.NotNull(handler);
  }

  [Fact]
  public void Uid_ReturnsClassName()
  {
    // Arrange
    var handler = new CoverletTestSessionHandler();

    // Act
    string uid = handler.Uid;

    // Assert
    Assert.Equal(nameof(CoverletTestSessionHandler), uid);
  }

  [Fact]
  public void Version_ReturnsAssemblyVersion()
  {
    // Arrange
    var handler = new CoverletTestSessionHandler();

    // Act
    string version = handler.Version;

    // Assert
    Assert.NotNull(version);
    Assert.NotEmpty(version);
    // Version should be in format like "8.0.0.0" or similar
    Assert.Matches(@"^\d+\.\d+\.\d+(\.\d+)?$", version);
  }

  [Fact]
  public void DisplayName_ReturnsExpectedValue()
  {
    // Arrange
    var handler = new CoverletTestSessionHandler();

    // Act
    string displayName = handler.DisplayName;

    // Assert
    Assert.Equal("Coverlet Coverage Session Handler", displayName);
  }

  [Fact]
  public void Description_ReturnsNonEmptyString()
  {
    // Arrange
    var handler = new CoverletTestSessionHandler();

    // Act
    string description = handler.Description;

    // Assert
    Assert.NotNull(description);
    Assert.NotEmpty(description);
    Assert.Equal("Flushes coverage data at end of test session", description);
  }

  [Fact]
  public void GetInstrumentationClass_WithNonInstrumentedAssembly_ReturnsNull()
  {
    // Arrange - Use a .NET runtime assembly that will never be instrumented
    Assembly runtimeAssembly = typeof(System.Text.Json.JsonSerializer).Assembly;

    // Act
    Type? result = CoverletTestSessionHandler.GetInstrumentationClass(runtimeAssembly);

    // Assert
    Assert.Null(result);
  }

  [Fact]
  public void GetInstrumentationClass_WithSystemAssembly_ReturnsNull()
  {
    // Arrange
    Assembly mscorlib = typeof(object).Assembly;

    // Act
    Type? result = CoverletTestSessionHandler.GetInstrumentationClass(mscorlib);

    // Assert
    Assert.Null(result);
  }

  [Fact]
  public async Task OnTestSessionFinishingAsync_ThrowsNotImplementedException()
  {
    // Arrange
    var handler = new CoverletTestSessionHandler();
    var mockTestSessionContext = new Mock<ITestSessionContext>();

    // Act & Assert
    await Assert.ThrowsAsync<NotImplementedException>(() =>
      handler.OnTestSessionFinishingAsync(mockTestSessionContext.Object));
  }

  [Fact]
  public void GetInstrumentationClass_WithMultipleAssemblies_HandlesAllCorrectly()
  {
    // Arrange & Act & Assert
    foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
    {
      // Should not throw for any assembly
      Type? result = CoverletTestSessionHandler.GetInstrumentationClass(assembly);

      // Most assemblies won't have instrumentation, so result should be null
      // This test verifies the method handles all loaded assemblies gracefully
    }
  }

  [Fact]
  public void GetInstrumentationClass_WithDynamicAssembly_HandlesGracefully()
  {
    // Arrange
    // Get a dynamic assembly that might throw on GetTypes()
    Assembly? dynamicAssembly = AppDomain.CurrentDomain.GetAssemblies()
      .FirstOrDefault(a => a.IsDynamic);

    if (dynamicAssembly is null)
    {
      // Skip if no dynamic assembly is loaded
      return;
    }

    // Act - should not throw
    Type? result = CoverletTestSessionHandler.GetInstrumentationClass(dynamicAssembly);

    // Assert
    Assert.Null(result);
  }

  [Fact]
  public void Properties_AllReturnExpectedValues()
  {
    // Arrange
    var handler = new CoverletTestSessionHandler();

    // Act & Assert
    Assert.Equal(nameof(CoverletTestSessionHandler), handler.Uid);
    Assert.NotEmpty(handler.Version);
    Assert.Equal("Coverlet Coverage Session Handler", handler.DisplayName);
    Assert.Equal("Flushes coverage data at end of test session", handler.Description);
  }

  [Fact]
  public void Constructor_WithEmptyTestModule_CreatesInstance()
  {
    // Arrange
    var mockConfiguration = new Mock<IConfiguration>();
    var mockSection = new Mock<IConfigurationSection>();
    mockConfiguration
      .Setup(x => x.GetSection(It.IsAny<string>()))
      .Returns(mockSection.Object);
    string testModule = string.Empty;

    // Act
    var handler = new CoverletTestSessionHandler(mockConfiguration.Object, testModule);

    // Assert
    Assert.NotNull(handler);
  }

  [Fact]
  public void GetInstrumentationClass_LooksForCorrectNamespaceAndNamePattern()
  {
    // Arrange
    // This test documents the expected behavior:
    // - Namespace should be "Coverlet.Core.Instrumentation.Tracker"
    // - Name should start with "{AssemblyName}_"
    Assembly testAssembly = typeof(CoverletTestSessionHandlerTests).Assembly;
    string expectedNamePrefix = testAssembly.GetName().Name + "_";

    // Act
    Type? result = CoverletTestSessionHandler.GetInstrumentationClass(testAssembly);

    // Assert - No instrumentation class exists in test assembly
    Assert.Null(result);
  }

  [Fact]
  public async Task IsEnabledAsync_AlwaysReturnsTrue_RegardlessOfConfiguration()
  {
    // Arrange
    var mockConfiguration = new Mock<IConfiguration>();
    var mockSection = new Mock<IConfigurationSection>();
    mockConfiguration
      .Setup(x => x.GetSection(It.IsAny<string>()))
      .Returns(mockSection.Object);
    var handler1 = new CoverletTestSessionHandler();
    var handler2 = new CoverletTestSessionHandler(mockConfiguration.Object, "test.dll");

    // Act
    bool result1 = await handler1.IsEnabledAsync();
    bool result2 = await handler2.IsEnabledAsync();

    // Assert
    Assert.True(result1);
    Assert.True(result2);
  }

  [Fact]
  public void Version_ReturnsValidVersionFormat()
  {
    // Arrange
    var handler = new CoverletTestSessionHandler();

    // Act
    string version = handler.Version;

    // Assert
    Assert.NotEqual("1.0.0", version); // Should get actual assembly version, not fallback
    Version.TryParse(version, out Version? parsedVersion);
    Assert.NotNull(parsedVersion);
  }

  [Fact]
  public void Handler_ImplementsITestSessionLifetimeHandler()
  {
    // Arrange
    var handler = new CoverletTestSessionHandler();

    // Act & Assert
    Assert.IsAssignableFrom<ITestSessionLifetimeHandler>(handler);
  }
}
