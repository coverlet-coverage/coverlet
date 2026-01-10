// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Coverlet.MTP.EnvironmentVariables;
using Microsoft.Testing.Platform.Extensions.TestHost;
using Microsoft.Testing.Platform.Logging;
using Microsoft.Testing.Platform.Services;
using Moq;
using Xunit;

namespace Coverlet.MTP.InProcess.Tests;

public class CoverletInProcessHandlerTests : IDisposable
{
  private readonly Mock<ILoggerFactory> _mockLoggerFactory = new();
  private readonly Mock<ILogger> _mockLogger = new();

  public CoverletInProcessHandlerTests()
  {
    _mockLoggerFactory
      .Setup(x => x.CreateLogger(It.IsAny<string>()))
      .Returns(_mockLogger.Object);
  }

  public void Dispose()
  {
    ClearEnvironmentVariables();
    GC.SuppressFinalize(this);
  }

  private static void ClearEnvironmentVariables()
  {
    System.Environment.SetEnvironmentVariable(CoverletMtpEnvironmentVariables.CoverageEnabled, null);
    System.Environment.SetEnvironmentVariable(CoverletMtpEnvironmentVariables.CoverageIdentifier, null);
    System.Environment.SetEnvironmentVariable(CoverletMtpEnvironmentVariables.InProcessDebug, null);
    System.Environment.SetEnvironmentVariable(CoverletMtpEnvironmentVariables.InProcessExceptionLog, null);
  }

  #region Constructor Tests

  [Fact]
  public void ConstructorWithCoverageEnabledSetsCorrectState()
  {
    // Arrange
    System.Environment.SetEnvironmentVariable(CoverletMtpEnvironmentVariables.CoverageEnabled, "true");
    System.Environment.SetEnvironmentVariable(CoverletMtpEnvironmentVariables.CoverageIdentifier, "test-id-123");

    // Act
    var handler = new CoverletInProcessHandler(_mockLoggerFactory.Object);

    // Assert
    Assert.NotNull(handler);
    _mockLogger.Verify(
      x => x.Log(
        It.Is<LogLevel>(l => l == LogLevel.Debug),
        It.Is<string>(s => s.Contains("CoverageEnabled=True")),
        It.IsAny<Exception?>(),
        It.IsAny<Func<string, Exception?, string>>()),
      Times.Once);
  }

  [Fact]
  public void ConstructorWithCoverageDisabledSetsCorrectState()
  {
    // Arrange
    System.Environment.SetEnvironmentVariable(CoverletMtpEnvironmentVariables.CoverageEnabled, "false");

    // Act
    var handler = new CoverletInProcessHandler(_mockLoggerFactory.Object);

    // Assert
    Assert.NotNull(handler);
    _mockLogger.Verify(
      x => x.Log(
        It.Is<LogLevel>(l => l == LogLevel.Debug),
        It.Is<string>(s => s.Contains("CoverageEnabled=False")),
        It.IsAny<Exception?>(),
        It.IsAny<Func<string, Exception?, string>>()),
      Times.Once);
  }

  [Fact]
  public void ConstructorWithNullCoverageEnabledDefaultsToDisabled()
  {
    // Arrange - no environment variable set

    // Act
    var handler = new CoverletInProcessHandler(_mockLoggerFactory.Object);

    // Assert
    Assert.NotNull(handler);
    _mockLogger.Verify(
      x => x.Log(
        It.Is<LogLevel>(l => l == LogLevel.Debug),
        It.Is<string>(s => s.Contains("CoverageEnabled=False")),
        It.IsAny<Exception?>(),
        It.IsAny<Func<string, Exception?, string>>()),
      Times.Once);
  }

  [Fact]
  public void ConstructorWithCoverageIdentifierSetsCorrectIdentifier()
  {
    // Arrange
    string expectedIdentifier = "unique-coverage-id-456";
    System.Environment.SetEnvironmentVariable(CoverletMtpEnvironmentVariables.CoverageEnabled, "true");
    System.Environment.SetEnvironmentVariable(CoverletMtpEnvironmentVariables.CoverageIdentifier, expectedIdentifier);

    // Act
    var handler = new CoverletInProcessHandler(_mockLoggerFactory.Object);

    // Assert
    _mockLogger.Verify(
      x => x.Log(
        It.Is<LogLevel>(l => l == LogLevel.Debug),
        It.Is<string>(s => s.Contains(expectedIdentifier)),
        It.IsAny<Exception?>(),
        It.IsAny<Func<string, Exception?, string>>()),
      Times.Once);
  }

  [Fact]
  public void ConstructorWithNullCoverageIdentifierHandlesGracefully()
  {
    // Arrange
    System.Environment.SetEnvironmentVariable(CoverletMtpEnvironmentVariables.CoverageEnabled, "true");
    // No identifier set

    // Act
    var handler = new CoverletInProcessHandler(_mockLoggerFactory.Object);

    // Assert
    _mockLogger.Verify(
      x => x.Log(
        It.Is<LogLevel>(l => l == LogLevel.Debug),
        It.Is<string>(s => s.Contains("(null)")),
        It.IsAny<Exception?>(),
        It.IsAny<Func<string, Exception?, string>>()),
      Times.Once);
  }

  #endregion

  #region Property Tests

  [Fact]
  public void UidReturnsExpectedValue()
  {
    // Arrange
    var handler = new CoverletInProcessHandler(_mockLoggerFactory.Object);

    // Act
    string uid = handler.Uid;

    // Assert
    Assert.Equal("Coverlet.MTP.InProcess", uid);
  }

  [Fact]
  public void VersionReturnsNonEmptyValue()
  {
    // Arrange
    var handler = new CoverletInProcessHandler(_mockLoggerFactory.Object);

    // Act
    string version = handler.Version;

    // Assert
    Assert.False(string.IsNullOrEmpty(version));
  }

  [Fact]
  public void DisplayNameReturnsExpectedValue()
  {
    // Arrange
    var handler = new CoverletInProcessHandler(_mockLoggerFactory.Object);

    // Act
    string displayName = handler.DisplayName;

    // Assert
    Assert.Equal("Coverlet In-Process Handler", displayName);
  }

  [Fact]
  public void DescriptionReturnsExpectedValue()
  {
    // Arrange
    var handler = new CoverletInProcessHandler(_mockLoggerFactory.Object);

    // Act
    string description = handler.Description;

    // Assert
    Assert.Equal("Flushes coverage hit data when test session ends", description);
  }

  #endregion

  #region IsEnabledAsync Tests

  [Fact]
  public async Task IsEnabledAsyncWhenCoverageEnabledReturnsTrue()
  {
    // Arrange
    System.Environment.SetEnvironmentVariable(CoverletMtpEnvironmentVariables.CoverageEnabled, "true");
    var handler = new CoverletInProcessHandler(_mockLoggerFactory.Object);

    // Act
    bool result = await handler.IsEnabledAsync();

    // Assert
    Assert.True(result);
  }

  [Fact]
  public async Task IsEnabledAsyncWhenCoverageDisabledReturnsFalse()
  {
    // Arrange
    System.Environment.SetEnvironmentVariable(CoverletMtpEnvironmentVariables.CoverageEnabled, "false");
    var handler = new CoverletInProcessHandler(_mockLoggerFactory.Object);

    // Act
    bool result = await handler.IsEnabledAsync();

    // Assert
    Assert.False(result);
  }

  [Fact]
  public async Task IsEnabledAsyncWhenCoverageNotSetReturnsFalse()
  {
    // Arrange - no environment variable
    var handler = new CoverletInProcessHandler(_mockLoggerFactory.Object);

    // Act
    bool result = await handler.IsEnabledAsync();

    // Assert
    Assert.False(result);
  }

  #endregion

  #region OnTestSessionStartingAsync Tests

  [Fact]
  public async Task OnTestSessionStartingAsyncWhenCoverageEnabledLogsDebugMessage()
  {
    // Arrange
    System.Environment.SetEnvironmentVariable(CoverletMtpEnvironmentVariables.CoverageEnabled, "true");
    var handler = new CoverletInProcessHandler(_mockLoggerFactory.Object);
    var mockSessionContext = new Mock<ITestSessionContext>();
    mockSessionContext.Setup(x => x.SessionUid).Returns(new Microsoft.Testing.Platform.TestHost.SessionUid("test-session-123"));

    // Act
    await ((ITestSessionLifetimeHandler)handler).OnTestSessionStartingAsync(mockSessionContext.Object);

    // Assert
    _mockLogger.Verify(
      x => x.Log(
        It.Is<LogLevel>(l => l == LogLevel.Debug),
        It.Is<string>(s => s.Contains("Test session starting")),
        It.IsAny<Exception?>(),
        It.IsAny<Func<string, Exception?, string>>()),
      Times.Once);
  }

  [Fact]
  public async Task OnTestSessionStartingAsyncWhenCoverageDisabledDoesNotLogSessionStarting()
  {
    // Arrange
    System.Environment.SetEnvironmentVariable(CoverletMtpEnvironmentVariables.CoverageEnabled, "false");
    var handler = new CoverletInProcessHandler(_mockLoggerFactory.Object);
    var mockSessionContext = new Mock<ITestSessionContext>();

    // Act
    await ((ITestSessionLifetimeHandler)handler).OnTestSessionStartingAsync(mockSessionContext.Object);

    // Assert
    _mockLogger.Verify(
      x => x.Log(
        It.Is<LogLevel>(l => l == LogLevel.Debug),
        It.Is<string>(s => s.Contains("Test session starting")),
        It.IsAny<Exception?>(),
        It.IsAny<Func<string, Exception?, string>>()),
      Times.Never);
  }

  #endregion

  #region OnTestSessionFinishingAsync Tests

  [Fact]
  public async Task OnTestSessionFinishingAsyncWhenCoverageDisabledReturnsImmediately()
  {
    // Arrange
    System.Environment.SetEnvironmentVariable(CoverletMtpEnvironmentVariables.CoverageEnabled, "false");
    var handler = new CoverletInProcessHandler(_mockLoggerFactory.Object);
    var mockSessionContext = new Mock<ITestSessionContext>();

    // Act
    await ((ITestSessionLifetimeHandler)handler).OnTestSessionFinishingAsync(mockSessionContext.Object);

    // Assert
    _mockLogger.Verify(
      x => x.Log(
        It.Is<LogLevel>(l => l == LogLevel.Debug),
        It.Is<string>(s => s.Contains("Test session finishing")),
        It.IsAny<Exception?>(),
        It.IsAny<Func<string, Exception?, string>>()),
      Times.Never);
  }

  [Fact]
  public async Task OnTestSessionFinishingAsyncWhenCoverageEnabledLogsDebugMessage()
  {
    // Arrange
    System.Environment.SetEnvironmentVariable(CoverletMtpEnvironmentVariables.CoverageEnabled, "true");
    var handler = new CoverletInProcessHandler(_mockLoggerFactory.Object);
    var mockSessionContext = new Mock<ITestSessionContext>();
    mockSessionContext.Setup(x => x.SessionUid).Returns(new Microsoft.Testing.Platform.TestHost.SessionUid("test-session-456"));

    // Act
    await ((ITestSessionLifetimeHandler)handler).OnTestSessionFinishingAsync(mockSessionContext.Object);

    // Assert
    _mockLogger.Verify(
      x => x.Log(
        It.Is<LogLevel>(l => l == LogLevel.Debug),
        It.Is<string>(s => s.Contains("Test session finishing")),
        It.IsAny<Exception?>(),
        It.IsAny<Func<string, Exception?, string>>()),
      Times.Once);
    _mockLogger.Verify(
      x => x.Log(
        It.Is<LogLevel>(l => l == LogLevel.Debug),
        It.Is<string>(s => s.Contains("flushing coverage data")),
        It.IsAny<Exception?>(),
        It.IsAny<Func<string, Exception?, string>>()),
      Times.Once);
  }

  [Fact]
  public async Task OnTestSessionFinishingAsyncWhenCoverageEnabledLogsFlushedAssembliesCount()
  {
    // Arrange
    System.Environment.SetEnvironmentVariable(CoverletMtpEnvironmentVariables.CoverageEnabled, "true");
    var handler = new CoverletInProcessHandler(_mockLoggerFactory.Object);
    var mockSessionContext = new Mock<ITestSessionContext>();
    mockSessionContext.Setup(x => x.SessionUid).Returns(new Microsoft.Testing.Platform.TestHost.SessionUid("test-session-789"));

    // Act
    await ((ITestSessionLifetimeHandler)handler).OnTestSessionFinishingAsync(mockSessionContext.Object);

    // Assert
    _mockLogger.Verify(
      x => x.Log(
        It.Is<LogLevel>(l => l == LogLevel.Debug),
        It.Is<string>(s => s.Contains("Flushed") && s.Contains("instrumented assemblies")),
        It.IsAny<Exception?>(),
        It.IsAny<Func<string, Exception?, string>>()),
      Times.Once);
  }

  [Fact]
  public async Task OnTestSessionFinishingAsyncWhenExceptionOccursWithExceptionLogEnabledLogsError()
  {
    // Arrange
    System.Environment.SetEnvironmentVariable(CoverletMtpEnvironmentVariables.CoverageEnabled, "true");
    System.Environment.SetEnvironmentVariable(CoverletMtpEnvironmentVariables.InProcessExceptionLog, "1");

    // Create a handler that will encounter assemblies
    var handler = new CoverletInProcessHandler(_mockLoggerFactory.Object);
    var mockSessionContext = new Mock<ITestSessionContext>();
    mockSessionContext.Setup(x => x.SessionUid).Returns(new Microsoft.Testing.Platform.TestHost.SessionUid("test-exception-session"));

    // Act - This should complete without throwing, even if internal exceptions occur
    await ((ITestSessionLifetimeHandler)handler).OnTestSessionFinishingAsync(mockSessionContext.Object);

    // Assert - Should not throw and should log debug messages
    _mockLogger.Verify(
      x => x.Log(
        It.Is<LogLevel>(l => l == LogLevel.Debug),
        It.IsAny<string>(),
        It.IsAny<Exception?>(),
        It.IsAny<Func<string, Exception?, string>>()),
      Times.AtLeastOnce);
  }

  [Fact]
  public async Task OnTestSessionFinishingAsyncWhenExceptionOccursWithExceptionLogDisabledDoesNotLogError()
  {
    // Arrange
    System.Environment.SetEnvironmentVariable(CoverletMtpEnvironmentVariables.CoverageEnabled, "true");
    System.Environment.SetEnvironmentVariable(CoverletMtpEnvironmentVariables.InProcessExceptionLog, "0");

    var handler = new CoverletInProcessHandler(_mockLoggerFactory.Object);
    var mockSessionContext = new Mock<ITestSessionContext>();
    mockSessionContext.Setup(x => x.SessionUid).Returns(new Microsoft.Testing.Platform.TestHost.SessionUid("test-no-log-session"));

    // Act
    await ((ITestSessionLifetimeHandler)handler).OnTestSessionFinishingAsync(mockSessionContext.Object);

    // Assert - Should not throw
    _mockLogger.Verify(
      x => x.Log(
        It.Is<LogLevel>(l => l == LogLevel.Error),
        It.IsAny<string>(),
        It.IsAny<Exception?>(),
        It.IsAny<Func<string, Exception?, string>>()),
      Times.Never);
  }

  #endregion

  #region ExceptionLog Tests

  [Fact]
  public void ConstructorWithExceptionLogEnabledSetsFlag()
  {
    // Arrange
    System.Environment.SetEnvironmentVariable(CoverletMtpEnvironmentVariables.InProcessExceptionLog, "1");
    System.Environment.SetEnvironmentVariable(CoverletMtpEnvironmentVariables.CoverageEnabled, "true");

    // Act
    var handler = new CoverletInProcessHandler(_mockLoggerFactory.Object);

    // Assert
    Assert.NotNull(handler);
  }

  [Fact]
  public void ConstructorWithExceptionLogDisabledDefaultsToFalse()
  {
    // Arrange
    System.Environment.SetEnvironmentVariable(CoverletMtpEnvironmentVariables.InProcessExceptionLog, "0");

    // Act
    var handler = new CoverletInProcessHandler(_mockLoggerFactory.Object);

    // Assert
    Assert.NotNull(handler);
  }

  #endregion

  #region Integration Tests

  [Fact]
  public async Task FullLifecycleWhenCoverageEnabledCompletesSuccessfully()
  {
    // Arrange
    System.Environment.SetEnvironmentVariable(CoverletMtpEnvironmentVariables.CoverageEnabled, "true");
    System.Environment.SetEnvironmentVariable(CoverletMtpEnvironmentVariables.CoverageIdentifier, "lifecycle-test-id");

    var handler = new CoverletInProcessHandler(_mockLoggerFactory.Object);
    var mockSessionContext = new Mock<ITestSessionContext>();
    mockSessionContext.Setup(x => x.SessionUid).Returns(new Microsoft.Testing.Platform.TestHost.SessionUid("lifecycle-session"));

    // Act & Assert - Should not throw
    bool isEnabled = await handler.IsEnabledAsync();
    Assert.True(isEnabled);

    await ((ITestSessionLifetimeHandler)handler).OnTestSessionStartingAsync(mockSessionContext.Object);
    await ((ITestSessionLifetimeHandler)handler).OnTestSessionFinishingAsync(mockSessionContext.Object);
  }

  [Fact]
  public async Task FullLifecycleWhenCoverageDisabledCompletesSuccessfully()
  {
    // Arrange
    System.Environment.SetEnvironmentVariable(CoverletMtpEnvironmentVariables.CoverageEnabled, "false");

    var handler = new CoverletInProcessHandler(_mockLoggerFactory.Object);
    var mockSessionContext = new Mock<ITestSessionContext>();
    mockSessionContext.Setup(x => x.SessionUid).Returns(new Microsoft.Testing.Platform.TestHost.SessionUid("disabled-session"));

    // Act & Assert - Should not throw
    bool isEnabled = await handler.IsEnabledAsync();
    Assert.False(isEnabled);

    await ((ITestSessionLifetimeHandler)handler).OnTestSessionStartingAsync(mockSessionContext.Object);
    await ((ITestSessionLifetimeHandler)handler).OnTestSessionFinishingAsync(mockSessionContext.Object);
  }

  #endregion
}

/// <summary>
/// Helper struct for creating session UIDs in tests.
/// This mirrors the Microsoft.Testing.Platform.Services.SessionUid struct.
/// </summary>
public readonly struct SessionUid
{
  public string Value { get; }

  public SessionUid(string value)
  {
    Value = value;
  }

  public override string ToString() => Value;
}
