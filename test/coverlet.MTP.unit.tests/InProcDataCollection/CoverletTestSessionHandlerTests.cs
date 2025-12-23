// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Coverlet.MTP.InProcDataCollection;
using Xunit;

namespace coverlet.MTP.unit.tests.InProcDataCollection;

public class CoverletTestSessionHandlerTests
{
  [Fact]
  public void Properties_ReturnExpectedValues()
  {
    // Arrange
    var handler = new CoverletTestSessionHandler();

    // Assert
    Assert.Equal(nameof(CoverletTestSessionHandler), handler.Uid);
    Assert.Equal("1.0.0", handler.Version);
    Assert.Equal("Coverlet Coverage Session Handler", handler.DisplayName);
    Assert.NotEmpty(handler.Description);
  }

  [Fact]
  public async Task IsEnabledAsync_ReturnsTrue()
  {
    // Arrange
    var handler = new CoverletTestSessionHandler();

    // Act
    bool result = await handler.IsEnabledAsync();

    // Assert
    Assert.True(result);
  }

  [Fact]
  public void GetInstrumentationClass_WithNonInstrumentedAssembly_ReturnsNull()
  {
    // Arrange
    Assembly testAssembly = typeof(CoverletTestSessionHandlerTests).Assembly;

    // Act
    Type? result = CoverletTestSessionHandler.GetInstrumentationClass(testAssembly);

    // Assert
    Assert.Null(result);
  }

  [Fact]
  public void GetInstrumentationClass_WithAssemblyThrowingException_ReturnsNull()
  {
    // This test validates that exceptions during type enumeration are handled gracefully
    // The method should return null rather than throwing
    Assembly mscorlib = typeof(object).Assembly;

    // Act
    Type? result = CoverletTestSessionHandler.GetInstrumentationClass(mscorlib);

    // Assert
    Assert.Null(result);
  }
}
