// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Coverlet.MTP.Diagnostics;
using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Configurations;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.TestHostControllers;
using Microsoft.Testing.Platform.Logging;
using Moq;
using Xunit;

namespace Coverlet.MTP.Tests;

public class CoverletExtensionEnvironmentVariableProviderTests : IDisposable
{
  private readonly Mock<IConfiguration> _mockConfiguration = new();
  private readonly Mock<ICommandLineOptions> _mockCommandLineOptions = new();
  private readonly Mock<ILoggerFactory> _mockLoggerFactory = new();
  private readonly Mock<ILogger> _mockLogger = new();
  private readonly CoverletExtensionEnvironmentVariableProvider _provider;

  public CoverletExtensionEnvironmentVariableProviderTests()
  {
    _mockLoggerFactory.As<ILoggerFactory>()
      .Setup(f => f.CreateLogger(It.IsAny<string>()))
      .Returns(_mockLogger.Object);

    _provider = new CoverletExtensionEnvironmentVariableProvider(
      _mockConfiguration.Object,
      _mockCommandLineOptions.Object,
      _mockLoggerFactory.Object);
  }

  public void Dispose()
  {
    // Clean up environment variables that may have been set during tests
    Environment.SetEnvironmentVariable(CoverletMtpDebugConstants.EnableTrackerLog, null);
    Environment.SetEnvironmentVariable(CoverletMtpDebugConstants.ExceptionLogEnabled, null);
    Environment.SetEnvironmentVariable(CoverletMtpDebugConstants.DebugLaunch, null);
    Environment.SetEnvironmentVariable(CoverletMtpDebugConstants.DebugWaitForAttach, null);
    GC.SuppressFinalize(this);
  }

  #region Properties Tests

  [Fact]
  public void Uid_ReturnsExpectedValue()
  {
    // Assert
    Assert.Equal(nameof(CoverletExtensionEnvironmentVariableProvider), _provider.Uid);
  }

  [Fact]
  public void Version_ReturnsNonEmptyString()
  {
    // Assert
    Assert.False(string.IsNullOrEmpty(_provider.Version));
  }

  [Fact]
  public void DisplayName_ReturnsExpectedValue()
  {
    // Assert
    Assert.Equal("Coverlet Environment Variable Provider", _provider.DisplayName);
  }

  [Fact]
  public void Description_ReturnsExpectedValue()
  {
    // Assert
    Assert.Equal("Provides environment variables for Coverlet coverage collection", _provider.Description);
  }

  #endregion

  #region IsEnabledAsync Tests

  [Fact]
  public async Task IsEnabledAsync_ReturnsTrue()
  {
    // Act
    bool result = await _provider.IsEnabledAsync();

    // Assert
    Assert.True(result);
  }

  #endregion

  #region UpdateAsync Tests

  [Fact]
  public async Task UpdateAsync_WhenTrackerLogEnabled_SetsEnvironmentVariable()
  {
    // Arrange
    Environment.SetEnvironmentVariable(CoverletMtpDebugConstants.EnableTrackerLog, "1");
    var mockEnvironmentVariables = new Mock<IEnvironmentVariables>();

    // Re-create provider to pick up the environment variable
    var provider = new CoverletExtensionEnvironmentVariableProvider(
      _mockConfiguration.Object,
      _mockCommandLineOptions.Object,
      _mockLoggerFactory.Object);

    // Act
    await provider.UpdateAsync(mockEnvironmentVariables.Object);

    // Assert
    mockEnvironmentVariables.Verify(
      x => x.SetVariable(It.Is<EnvironmentVariable>(
        ev => ev.Variable == CoverletMtpDebugConstants.EnableTrackerLog && ev.Value == "1")),
      Times.Once());
  }

  [Fact]
  public async Task UpdateAsync_WhenTrackerLogDisabled_DoesNotSetEnvironmentVariable()
  {
    // Arrange
    Environment.SetEnvironmentVariable(CoverletMtpDebugConstants.EnableTrackerLog, null);
    var mockEnvironmentVariables = new Mock<IEnvironmentVariables>();

    // Re-create provider to pick up the environment variable
    var provider = new CoverletExtensionEnvironmentVariableProvider(
      _mockConfiguration.Object,
      _mockCommandLineOptions.Object,
      _mockLoggerFactory.Object);

    // Act
    await provider.UpdateAsync(mockEnvironmentVariables.Object);

    // Assert
    mockEnvironmentVariables.Verify(
      x => x.SetVariable(It.Is<EnvironmentVariable>(
        ev => ev.Variable == CoverletMtpDebugConstants.EnableTrackerLog)),
      Times.Never());
  }

  [Fact]
  public async Task UpdateAsync_WhenExceptionLogEnabled_SetsEnvironmentVariable()
  {
    // Arrange
    Environment.SetEnvironmentVariable(CoverletMtpDebugConstants.ExceptionLogEnabled, "1");
    var mockEnvironmentVariables = new Mock<IEnvironmentVariables>();

    // Re-create provider to pick up the environment variable
    var provider = new CoverletExtensionEnvironmentVariableProvider(
      _mockConfiguration.Object,
      _mockCommandLineOptions.Object,
      _mockLoggerFactory.Object);

    // Act
    await provider.UpdateAsync(mockEnvironmentVariables.Object);

    // Assert
    mockEnvironmentVariables.Verify(
      x => x.SetVariable(It.Is<EnvironmentVariable>(
        ev => ev.Variable == CoverletMtpDebugConstants.ExceptionLogEnabled && ev.Value == "1")),
      Times.Once());
  }

  [Fact]
  public async Task UpdateAsync_WhenExceptionLogDisabled_DoesNotSetEnvironmentVariable()
  {
    // Arrange
    Environment.SetEnvironmentVariable(CoverletMtpDebugConstants.ExceptionLogEnabled, null);
    var mockEnvironmentVariables = new Mock<IEnvironmentVariables>();

    // Re-create provider to pick up the environment variable
    var provider = new CoverletExtensionEnvironmentVariableProvider(
      _mockConfiguration.Object,
      _mockCommandLineOptions.Object,
      _mockLoggerFactory.Object);

    // Act
    await provider.UpdateAsync(mockEnvironmentVariables.Object);

    // Assert
    mockEnvironmentVariables.Verify(
      x => x.SetVariable(It.Is<EnvironmentVariable>(
        ev => ev.Variable == CoverletMtpDebugConstants.ExceptionLogEnabled)),
      Times.Never());
  }

  [Fact]
  public async Task UpdateAsync_WhenBothTrackerAndExceptionLogEnabled_SetsBothEnvironmentVariables()
  {
    // Arrange
    Environment.SetEnvironmentVariable(CoverletMtpDebugConstants.EnableTrackerLog, "1");
    Environment.SetEnvironmentVariable(CoverletMtpDebugConstants.ExceptionLogEnabled, "true");
    var mockEnvironmentVariables = new Mock<IEnvironmentVariables>();

    // Re-create provider to pick up the environment variables
    var provider = new CoverletExtensionEnvironmentVariableProvider(
      _mockConfiguration.Object,
      _mockCommandLineOptions.Object,
      _mockLoggerFactory.Object);

    // Act
    await provider.UpdateAsync(mockEnvironmentVariables.Object);

    // Assert
    mockEnvironmentVariables.Verify(
      x => x.SetVariable(It.Is<EnvironmentVariable>(
        ev => ev.Variable == CoverletMtpDebugConstants.EnableTrackerLog)),
      Times.Once());
    mockEnvironmentVariables.Verify(
      x => x.SetVariable(It.Is<EnvironmentVariable>(
        ev => ev.Variable == CoverletMtpDebugConstants.ExceptionLogEnabled)),
      Times.Once());
  }

  [Fact]
  public async Task UpdateAsync_ReturnsCompletedTask()
  {
    // Arrange
    var mockEnvironmentVariables = new Mock<IEnvironmentVariables>();

    // Act
    Task task = _provider.UpdateAsync(mockEnvironmentVariables.Object);

    // Assert
    Assert.True(task.IsCompleted);
    await task; // Ensure no exceptions
  }

  [Fact]
  public async Task UpdateAsync_TrackerLogEnvironmentVariable_IsNotSecret()
  {
    // Arrange
    Environment.SetEnvironmentVariable(CoverletMtpDebugConstants.EnableTrackerLog, "1");
    var mockEnvironmentVariables = new Mock<IEnvironmentVariables>();

    var provider = new CoverletExtensionEnvironmentVariableProvider(
      _mockConfiguration.Object,
      _mockCommandLineOptions.Object,
      _mockLoggerFactory.Object);

    // Act
    await provider.UpdateAsync(mockEnvironmentVariables.Object);

    // Assert
    mockEnvironmentVariables.Verify(
      x => x.SetVariable(It.Is<EnvironmentVariable>(
        ev => ev.Variable == CoverletMtpDebugConstants.EnableTrackerLog && !ev.IsSecret && !ev.IsLocked)),
      Times.Once());
  }

  #endregion

  #region ValidateTestHostEnvironmentVariablesAsync Tests

  [Fact]
  public async Task ValidateTestHostEnvironmentVariablesAsync_ReturnsValidResult()
  {
    // Arrange
    var mockReadOnlyEnvironmentVariables = new Mock<IReadOnlyEnvironmentVariables>();

    // Act
    ValidationResult result = await _provider.ValidateTestHostEnvironmentVariablesAsync(mockReadOnlyEnvironmentVariables.Object);

    // Assert
    Assert.True(result.IsValid);
  }

  #endregion
}
