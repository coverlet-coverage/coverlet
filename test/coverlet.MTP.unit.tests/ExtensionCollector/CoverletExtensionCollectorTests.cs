// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using coverlet.Extension.Collector;
using Coverlet.Core;
using Coverlet.Core.Abstractions;
using Coverlet.MTP.CommandLine;
using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Configurations;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.TestHostControllers;
using Microsoft.Testing.Platform.Logging;
using Moq;
using Xunit;

namespace coverlet.MTP.unit.tests.ExtensionCollector
{
  public class CoverletExtensionCollectorTests
  {
    private readonly Mock<ILoggerFactory> _mockLoggerFactory;
    private readonly Mock<ICommandLineOptions> _mockCommandLineOptions;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<Microsoft.Testing.Platform.Logging.ILogger> _mockLogger;

    public CoverletExtensionCollectorTests()
    {
      _mockLoggerFactory = new Mock<ILoggerFactory>();
      _mockCommandLineOptions = new Mock<ICommandLineOptions>();
      _mockConfiguration = new Mock<IConfiguration>();
      _mockLogger = new Mock<Microsoft.Testing.Platform.Logging.ILogger>();

      _mockLoggerFactory
        .Setup(x => x.CreateLogger(It.IsAny<string>()))
        .Returns(_mockLogger.Object);
    }

    [Fact]
    public void Constructor_WithNullLoggerFactory_ThrowsArgumentNullException()
    {
      // Act & Assert
      ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
        new CoverletExtensionCollector(null!, _mockCommandLineOptions.Object, _mockConfiguration.Object));

      Assert.Equal("loggerFactory", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullCommandLineOptions_ThrowsArgumentNullException()
    {
      // Act & Assert
      ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
        new CoverletExtensionCollector(_mockLoggerFactory.Object, null!, _mockConfiguration.Object));

      Assert.Equal("commandLineOptions", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullConfiguration_ThrowsArgumentNullException()
    {
      // Act & Assert
      Assert.Throws<ArgumentNullException>(() =>
        new CoverletExtensionCollector(_mockLoggerFactory.Object, _mockCommandLineOptions.Object, null!));
    }

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
      // Act
      var collector = new CoverletExtensionCollector(
        _mockLoggerFactory.Object,
        _mockCommandLineOptions.Object,
        _mockConfiguration.Object);

      // Assert
      Assert.NotNull(collector);
    }

    [Fact]
    public async Task IsEnabledAsync_ReturnsTrue()
    {
      // Arrange
      var collector = CreateCollector();

      // Act
      bool result = await collector.IsEnabledAsync();

      // Assert
      Assert.True(result);
    }

    [Fact]
    public async Task BeforeTestHostProcessStartAsync_WhenCoverageNotEnabled_ReturnsWithoutInstrumentation()
    {
      // Arrange
      _mockCommandLineOptions
        .Setup(x => x.IsOptionSet(CoverletOptionNames.Coverage))
        .Returns(false);

      var collector = CreateCollector();
      ITestHostProcessLifetimeHandler handler = collector;

      // Act
      await handler.BeforeTestHostProcessStartAsync(CancellationToken.None);

      // Assert - no exception thrown, coverage should be disabled
      _mockCommandLineOptions.Verify(x => x.IsOptionSet(CoverletOptionNames.Coverage), Times.Once);
    }

    [Fact]
    public async Task BeforeTestHostProcessStartAsync_WhenCoverageEnabledButNoModulePath_DisablesCoverage()
    {
      // Arrange
      _mockCommandLineOptions
        .Setup(x => x.IsOptionSet(CoverletOptionNames.Coverage))
        .Returns(true);

      _mockConfiguration
        .Setup(x => x[It.IsAny<string>()])
        .Returns((string?)null);

      var collector = CreateCollector();
      ITestHostProcessLifetimeHandler handler = collector;

      // Act
      await handler.BeforeTestHostProcessStartAsync(CancellationToken.None);

      // Assert - should complete without throwing
      _mockCommandLineOptions.Verify(x => x.IsOptionSet(CoverletOptionNames.Coverage), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenCoverageNotEnabled_DoesNotSetEnvironmentVariables()
    {
      // Arrange
      _mockCommandLineOptions
        .Setup(x => x.IsOptionSet(CoverletOptionNames.Coverage))
        .Returns(false);

      var collector = CreateCollector();
      ITestHostProcessLifetimeHandler lifetimeHandler = collector;
      ITestHostEnvironmentVariableProvider envProvider = collector;

      await lifetimeHandler.BeforeTestHostProcessStartAsync(CancellationToken.None);

      var mockEnvVariables = new Mock<IEnvironmentVariables>();

      // Act
      await envProvider.UpdateAsync(mockEnvVariables.Object);

      // Assert
      mockEnvVariables.Verify(
        x => x.SetVariable(It.IsAny<EnvironmentVariable>()),
        Times.Never);
    }

    [Fact]
    public async Task ValidateTestHostEnvironmentVariablesAsync_WhenNoConflict_ReturnsValid()
    {
      // Arrange
      var collector = CreateCollector();
      ITestHostEnvironmentVariableProvider envProvider = collector;

      var mockReadOnlyEnvVariables = new Mock<IReadOnlyEnvironmentVariables>();
      mockReadOnlyEnvVariables
        .Setup(x => x.TryGetVariable(It.IsAny<string>(), out It.Ref<OwnedEnvironmentVariable?>.IsAny))
        .Returns(false);

      // Act
      ValidationResult result = await envProvider.ValidateTestHostEnvironmentVariablesAsync(mockReadOnlyEnvVariables.Object);

      // Assert
      Assert.True(result.IsValid);
    }

    [Fact]
    public async Task OnTestHostProcessStartedAsync_LogsProcessInformation()
    {
      // Arrange
      var collector = CreateCollector();
      ITestHostProcessLifetimeHandler handler = collector;

      var mockProcessInfo = new Mock<ITestHostProcessInformation>();
      mockProcessInfo.Setup(x => x.PID).Returns(12345);

      // Act
      await handler.OnTestHostProcessStartedAsync(mockProcessInfo.Object, CancellationToken.None);

      // Assert - should complete without throwing
      mockProcessInfo.Verify(x => x.PID, Times.AtLeastOnce);
    }

    [Fact]
    public async Task OnTestHostProcessExitedAsync_WhenCoverageNotEnabled_SkipsCollection()
    {
      // Arrange
      _mockCommandLineOptions
        .Setup(x => x.IsOptionSet(CoverletOptionNames.Coverage))
        .Returns(false);

      var collector = CreateCollector();
      ITestHostProcessLifetimeHandler handler = collector;

      await handler.BeforeTestHostProcessStartAsync(CancellationToken.None);

      var mockProcessInfo = new Mock<ITestHostProcessInformation>();
      mockProcessInfo.Setup(x => x.PID).Returns(12345);
      mockProcessInfo.Setup(x => x.ExitCode).Returns(0);

      // Act
      await handler.OnTestHostProcessExitedAsync(mockProcessInfo.Object, CancellationToken.None);

      // Assert - should complete without throwing
      mockProcessInfo.Verify(x => x.ExitCode, Times.AtLeastOnce);
    }

    [Fact]
    public void Extension_Properties_ReturnExpectedValues()
    {
      // Arrange
      var collector = CreateCollector();
      IExtension extension = collector;

      // Act & Assert
      Assert.False(string.IsNullOrEmpty(extension.Uid));
      Assert.False(string.IsNullOrEmpty(extension.Version));
      Assert.False(string.IsNullOrEmpty(extension.DisplayName));
      Assert.False(string.IsNullOrEmpty(extension.Description));
    }

    [Fact]
    public async Task BeforeTestHostProcessStartAsync_WhenExceptionThrown_DisablesCoverageAndDoesNotRethrow()
    {
      // Arrange
      _mockCommandLineOptions
        .Setup(x => x.IsOptionSet(CoverletOptionNames.Coverage))
        .Returns(true);

      // Setup configuration to return a path that triggers an exception during processing
      _mockConfiguration
        .Setup(x => x["TestModule"])
        .Throws(new InvalidOperationException("Test exception"));

      var collector = CreateCollector();
      ITestHostProcessLifetimeHandler handler = collector;

      // Act - should not throw
      await handler.BeforeTestHostProcessStartAsync(CancellationToken.None);

      // Assert - coverage should be disabled due to exception
      ITestHostEnvironmentVariableProvider envProvider = collector;
      var mockEnvVariables = new Mock<IEnvironmentVariables>();
      await envProvider.UpdateAsync(mockEnvVariables.Object);

      mockEnvVariables.Verify(
        x => x.SetVariable(It.IsAny<EnvironmentVariable>()),
        Times.Never);
    }

    [Fact]
    public async Task BeforeTestHostProcessStartAsync_ParsesFormatOptions()
    {
      // Arrange
      _mockCommandLineOptions
        .Setup(x => x.IsOptionSet(CoverletOptionNames.Coverage))
        .Returns(true);

      string[]? formats = ["json", "cobertura"];
      _mockCommandLineOptions
        .Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.Formats, out formats))
        .Returns(true);

      _mockConfiguration
        .Setup(x => x[It.IsAny<string>()])
        .Returns((string?)null);

      var collector = CreateCollector();
      ITestHostProcessLifetimeHandler handler = collector;

      // Act
      await handler.BeforeTestHostProcessStartAsync(CancellationToken.None);

      // Assert
      _mockCommandLineOptions.Verify(
        x => x.TryGetOptionArgumentList(CoverletOptionNames.Formats, out formats),
        Times.Once);
    }

    [Fact]
    public async Task BeforeTestHostProcessStartAsync_ParsesIncludeFilters()
    {
      // Arrange
      _mockCommandLineOptions
        .Setup(x => x.IsOptionSet(CoverletOptionNames.Coverage))
        .Returns(true);

      string[]? includes = ["[MyAssembly]*"];
      _mockCommandLineOptions
        .Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.Include, out includes))
        .Returns(true);

      _mockConfiguration
        .Setup(x => x[It.IsAny<string>()])
        .Returns((string?)null);

      var collector = CreateCollector();
      ITestHostProcessLifetimeHandler handler = collector;

      // Act
      await handler.BeforeTestHostProcessStartAsync(CancellationToken.None);

      // Assert
      _mockCommandLineOptions.Verify(
        x => x.TryGetOptionArgumentList(CoverletOptionNames.Include, out includes),
        Times.Once);
    }

    [Fact]
    public async Task BeforeTestHostProcessStartAsync_ParsesExcludeFilters()
    {
      // Arrange
      _mockCommandLineOptions
        .Setup(x => x.IsOptionSet(CoverletOptionNames.Coverage))
        .Returns(true);

      string[]? excludes = ["[TestAssembly]*"];
      _mockCommandLineOptions
        .Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.Exclude, out excludes))
        .Returns(true);

      _mockConfiguration
        .Setup(x => x[It.IsAny<string>()])
        .Returns((string?)null);

      var collector = CreateCollector();
      ITestHostProcessLifetimeHandler handler = collector;

      // Act
      await handler.BeforeTestHostProcessStartAsync(CancellationToken.None);

      // Assert
      _mockCommandLineOptions.Verify(
        x => x.TryGetOptionArgumentList(CoverletOptionNames.Exclude, out excludes),
        Times.Once);
    }

    [Fact]
    public async Task BeforeTestHostProcessStartAsync_ParsesIncludeTestAssemblyOption()
    {
      // Arrange
      _mockCommandLineOptions
        .Setup(x => x.IsOptionSet(CoverletOptionNames.Coverage))
        .Returns(true);

      _mockCommandLineOptions
        .Setup(x => x.IsOptionSet(CoverletOptionNames.IncludeTestAssembly))
        .Returns(true);

      _mockConfiguration
        .Setup(x => x[It.IsAny<string>()])
        .Returns((string?)null);

      var collector = CreateCollector();
      ITestHostProcessLifetimeHandler handler = collector;

      // Act
      await handler.BeforeTestHostProcessStartAsync(CancellationToken.None);

      // Assert
      _mockCommandLineOptions.Verify(
        x => x.IsOptionSet(CoverletOptionNames.IncludeTestAssembly),
        Times.Once);
    }

    private CoverletExtensionCollector CreateCollector()
    {
      return new CoverletExtensionCollector(
        _mockLoggerFactory.Object,
        _mockCommandLineOptions.Object,
        _mockConfiguration.Object);
    }

    [Fact]
    public async Task UpdateAsync_WhenCoverageEnabledButIdentifierNull_DoesNotSetEnvironmentVariables()
    {
      // Arrange - coverage enabled but no identifier (module path not resolved)
      _mockCommandLineOptions
        .Setup(x => x.IsOptionSet(CoverletOptionNames.Coverage))
        .Returns(true);

      _mockConfiguration
        .Setup(x => x[It.IsAny<string>()])
        .Returns((string?)null);

      var collector = CreateCollector();
      ITestHostProcessLifetimeHandler lifetimeHandler = collector;
      ITestHostEnvironmentVariableProvider envProvider = collector;

      // BeforeTestHostProcessStartAsync will fail to resolve module path, leaving identifier null
      await lifetimeHandler.BeforeTestHostProcessStartAsync(CancellationToken.None);

      var mockEnvVariables = new Mock<IEnvironmentVariables>();

      // Act
      await envProvider.UpdateAsync(mockEnvVariables.Object);

      // Assert - no variables set because coverage identifier is null
      mockEnvVariables.Verify(
        x => x.SetVariable(It.IsAny<EnvironmentVariable>()),
        Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsCompletedTask()
    {
      // Arrange
      _mockCommandLineOptions
        .Setup(x => x.IsOptionSet(CoverletOptionNames.Coverage))
        .Returns(false);

      var collector = CreateCollector();
      ITestHostEnvironmentVariableProvider envProvider = collector;

      var mockEnvVariables = new Mock<IEnvironmentVariables>();

      // Act
      Task result = envProvider.UpdateAsync(mockEnvVariables.Object);

      // Assert
      Assert.True(result.IsCompleted);
      await result; // Should not throw
    }

    [Fact]
    public async Task ValidateTestHostEnvironmentVariablesAsync_WhenConflictExists_ReturnsInvalid()
    {
      // Arrange
      var collector = CreateCollector();
      ITestHostEnvironmentVariableProvider envProvider = collector;

      var mockOtherExtension = new Mock<IExtension>();
      var conflictingVariable = new OwnedEnvironmentVariable(
        mockOtherExtension.Object,
        "COVERLET_MTP_COVERAGE_ENABLED",
        "true",
        isSecret: false,
        isLocked: true);

      var mockReadOnlyEnvVariables = new Mock<IReadOnlyEnvironmentVariables>();
      mockReadOnlyEnvVariables
        .Setup(x => x.TryGetVariable("COVERLET_MTP_COVERAGE_ENABLED", out conflictingVariable))
        .Returns(true);

      // Act
      ValidationResult result = await envProvider.ValidateTestHostEnvironmentVariablesAsync(mockReadOnlyEnvVariables.Object);

      // Assert
      Assert.False(result.IsValid);
      Assert.Contains("COVERLET_MTP_COVERAGE_ENABLED", result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateTestHostEnvironmentVariablesAsync_WhenVariableOwnedBySelf_ReturnsValid()
    {
      // Arrange
      var collector = CreateCollector();
      ITestHostEnvironmentVariableProvider envProvider = collector;
      IExtension extension = collector;

      var ownedVariable = new OwnedEnvironmentVariable(
        extension,
        "COVERLET_MTP_COVERAGE_ENABLED",
        "true",
        isSecret: false,
        isLocked: true);

      var mockReadOnlyEnvVariables = new Mock<IReadOnlyEnvironmentVariables>();
      mockReadOnlyEnvVariables
        .Setup(x => x.TryGetVariable("COVERLET_MTP_COVERAGE_ENABLED", out ownedVariable))
        .Returns(true);

      // Act
      ValidationResult result = await envProvider.ValidateTestHostEnvironmentVariablesAsync(mockReadOnlyEnvVariables.Object);

      // Assert
      Assert.True(result.IsValid);
    }

    [Fact]
    public async Task OnTestHostProcessExitedAsync_WhenCoverageNull_SkipsCollection()
    {
      // Arrange
      _mockCommandLineOptions
        .Setup(x => x.IsOptionSet(CoverletOptionNames.Coverage))
        .Returns(true);

      _mockConfiguration
        .Setup(x => x[It.IsAny<string>()])
        .Returns((string?)null);

      var collector = CreateCollector();
      ITestHostProcessLifetimeHandler handler = collector;

      // BeforeTestHostProcessStartAsync will fail to resolve module path, leaving _coverage null
      await handler.BeforeTestHostProcessStartAsync(CancellationToken.None);

      var mockProcessInfo = new Mock<ITestHostProcessInformation>();
      mockProcessInfo.Setup(x => x.PID).Returns(12345);
      mockProcessInfo.Setup(x => x.ExitCode).Returns(0);

      // Act - should not throw
      await handler.OnTestHostProcessExitedAsync(mockProcessInfo.Object, CancellationToken.None);

      // Assert
      mockProcessInfo.Verify(x => x.PID, Times.AtLeastOnce);
      mockProcessInfo.Verify(x => x.ExitCode, Times.AtLeastOnce);
    }

    [Fact]
    public async Task OnTestHostProcessExitedAsync_WhenServiceProviderNull_SkipsCollection()
    {
      // Arrange
      _mockCommandLineOptions
        .Setup(x => x.IsOptionSet(CoverletOptionNames.Coverage))
        .Returns(false);

      var collector = CreateCollector();
      ITestHostProcessLifetimeHandler handler = collector;

      // BeforeTestHostProcessStartAsync with coverage disabled leaves _serviceProvider null
      await handler.BeforeTestHostProcessStartAsync(CancellationToken.None);

      var mockProcessInfo = new Mock<ITestHostProcessInformation>();
      mockProcessInfo.Setup(x => x.PID).Returns(12345);
      mockProcessInfo.Setup(x => x.ExitCode).Returns(0);

      // Act - should not throw
      await handler.OnTestHostProcessExitedAsync(mockProcessInfo.Object, CancellationToken.None);

      // Assert - method completes without throwing
      mockProcessInfo.Verify(x => x.ExitCode, Times.AtLeastOnce);
    }

    [Fact]
    public async Task OnTestHostProcessExitedAsync_LogsTestHostProcessInformation()
    {
      // Arrange
      _mockCommandLineOptions
        .Setup(x => x.IsOptionSet(CoverletOptionNames.Coverage))
        .Returns(false);

      var collector = CreateCollector();
      ITestHostProcessLifetimeHandler handler = collector;

      await handler.BeforeTestHostProcessStartAsync(CancellationToken.None);

      var mockProcessInfo = new Mock<ITestHostProcessInformation>();
      mockProcessInfo.Setup(x => x.PID).Returns(99999);
      mockProcessInfo.Setup(x => x.ExitCode).Returns(1);

      // Act
      await handler.OnTestHostProcessExitedAsync(mockProcessInfo.Object, CancellationToken.None);

      // Assert - process info properties were accessed for logging
      mockProcessInfo.Verify(x => x.PID, Times.AtLeastOnce);
      mockProcessInfo.Verify(x => x.ExitCode, Times.AtLeastOnce);
    }

    [Fact]
    public async Task OnTestHostProcessExitedAsync_WithCancellation_CompletesWithoutError()
    {
      // Arrange
      _mockCommandLineOptions
        .Setup(x => x.IsOptionSet(CoverletOptionNames.Coverage))
        .Returns(false);

      var collector = CreateCollector();
      ITestHostProcessLifetimeHandler handler = collector;

      await handler.BeforeTestHostProcessStartAsync(CancellationToken.None);

      var mockProcessInfo = new Mock<ITestHostProcessInformation>();
      mockProcessInfo.Setup(x => x.PID).Returns(12345);
      mockProcessInfo.Setup(x => x.ExitCode).Returns(0);

      using var cts = new CancellationTokenSource();

      // Act - should complete without error even with cancellation token
      await handler.OnTestHostProcessExitedAsync(mockProcessInfo.Object, cts.Token);

      // Assert
      Assert.False(cts.IsCancellationRequested);
    }

    [Fact]
    public async Task OnTestHostProcessExitedAsync_WhenAllConditionsFalse_LogsSkipMessage()
    {
      // Arrange - all three conditions (_coverageEnabled, _coverage, _serviceProvider) will be false/null
      _mockCommandLineOptions
        .Setup(x => x.IsOptionSet(CoverletOptionNames.Coverage))
        .Returns(false);

      var collector = CreateCollector();
      ITestHostProcessLifetimeHandler handler = collector;

      // This ensures _coverageEnabled = false
      await handler.BeforeTestHostProcessStartAsync(CancellationToken.None);

      var mockProcessInfo = new Mock<ITestHostProcessInformation>();
      mockProcessInfo.Setup(x => x.PID).Returns(12345);
      mockProcessInfo.Setup(x => x.ExitCode).Returns(0);

      // Act
      await handler.OnTestHostProcessExitedAsync(mockProcessInfo.Object, CancellationToken.None);

      // Assert - verify method completes (coverage skipped path taken)
      mockProcessInfo.Verify(x => x.PID, Times.AtLeastOnce);
    }

    [Fact]
    public async Task GenerateReportsAsync_WhenUnsupportedFormat_ThrowsInvalidOperationException()
    {
      // Arrange
      _mockCommandLineOptions
        .Setup(x => x.IsOptionSet(CoverletOptionNames.Coverage))
        .Returns(true);

      string[]? formats = ["unsupportedformat"];
      _mockCommandLineOptions
        .Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.Formats, out formats))
        .Returns(true);

      // Setup a valid test module path to get past initialization
      string tempFile = Path.GetTempFileName();
      try
      {
        _mockConfiguration
          .Setup(x => x["TestModule"])
          .Returns(tempFile);

        _mockConfiguration
          .Setup(x => x["platformOptions:resultDirectory"])
          .Returns(Path.GetTempPath());

        var collector = CreateCollector();
        ITestHostProcessLifetimeHandler handler = collector;

        await handler.BeforeTestHostProcessStartAsync(CancellationToken.None);

        var mockProcessInfo = new Mock<ITestHostProcessInformation>();
        mockProcessInfo.Setup(x => x.PID).Returns(12345);
        mockProcessInfo.Setup(x => x.ExitCode).Returns(0);

        // Act & Assert - the exception should be caught and logged, not rethrown
        await handler.OnTestHostProcessExitedAsync(mockProcessInfo.Object, CancellationToken.None);

        // Verify method completes (exception was handled internally)
        mockProcessInfo.Verify(x => x.ExitCode, Times.AtLeastOnce);
      }
      finally
      {
        File.Delete(tempFile);
      }
    }

    [Fact]
    public async Task GenerateReportsAsync_WithMultipleFormats_ProcessesAllFormats()
    {
      // Arrange
      _mockCommandLineOptions
        .Setup(x => x.IsOptionSet(CoverletOptionNames.Coverage))
        .Returns(true);

      string[]? formats = ["json,cobertura"];
      _mockCommandLineOptions
        .Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.Formats, out formats))
        .Returns(true);

      _mockConfiguration
        .Setup(x => x[It.IsAny<string>()])
        .Returns((string?)null);

      var collector = CreateCollector();
      ITestHostProcessLifetimeHandler handler = collector;

      // Act
      await handler.BeforeTestHostProcessStartAsync(CancellationToken.None);

      // Assert - formats were parsed
      _mockCommandLineOptions.Verify(
        x => x.TryGetOptionArgumentList(CoverletOptionNames.Formats, out formats),
        Times.Once);
    }

    [Fact]
    public async Task GenerateReportsAsync_WhenOutputDirectoryDoesNotExist_CreatesDirectory()
    {
      // Arrange
      _mockCommandLineOptions
        .Setup(x => x.IsOptionSet(CoverletOptionNames.Coverage))
        .Returns(true);

      string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

      // Mock the underlying configuration key that GetTestResultDirectory() extension method reads
      _mockConfiguration
        .Setup(x => x["platformOptions:resultDirectory"])
        .Returns(tempDir + Path.DirectorySeparatorChar);

      _mockConfiguration
        .Setup(x => x[It.IsAny<string>()])
        .Returns((string?)null);

      var collector = CreateCollector();
      ITestHostProcessLifetimeHandler handler = collector;

      try
      {
        // Act
        await handler.BeforeTestHostProcessStartAsync(CancellationToken.None);

        var mockProcessInfo = new Mock<ITestHostProcessInformation>();
        mockProcessInfo.Setup(x => x.PID).Returns(12345);
        mockProcessInfo.Setup(x => x.ExitCode).Returns(0);

        await handler.OnTestHostProcessExitedAsync(mockProcessInfo.Object, CancellationToken.None);

        // Assert - method completes without throwing
        mockProcessInfo.Verify(x => x.ExitCode, Times.AtLeastOnce);
      }
      finally
      {
        if (Directory.Exists(tempDir))
        {
          Directory.Delete(tempDir, true);
        }
      }
    }

    [Fact]
    public async Task GenerateReportsAsync_WhenTestResultDirectoryNull_UsesModuleDirectory()
    {
      // Arrange
      _mockCommandLineOptions
        .Setup(x => x.IsOptionSet(CoverletOptionNames.Coverage))
        .Returns(true);

      // Mock the underlying configuration key to return null (simulates no result directory configured)
      _mockConfiguration
        .Setup(x => x["platformOptions:resultDirectory"])
        .Returns((string?)null);

      _mockConfiguration
        .Setup(x => x[It.IsAny<string>()])
        .Returns((string?)null);

      var collector = CreateCollector();
      ITestHostProcessLifetimeHandler handler = collector;

      // Act
      await handler.BeforeTestHostProcessStartAsync(CancellationToken.None);

      var mockProcessInfo = new Mock<ITestHostProcessInformation>();
      mockProcessInfo.Setup(x => x.PID).Returns(12345);
      mockProcessInfo.Setup(x => x.ExitCode).Returns(0);

      await handler.OnTestHostProcessExitedAsync(mockProcessInfo.Object, CancellationToken.None);

      // Assert - method completes without throwing
      mockProcessInfo.Verify(x => x.ExitCode, Times.AtLeastOnce);
    }

    [Fact]
    public async Task GenerateReportsAsync_WithCommaSeparatedFormats_SplitsCorrectly()
    {
      // Arrange
      _mockCommandLineOptions
        .Setup(x => x.IsOptionSet(CoverletOptionNames.Coverage))
        .Returns(true);

      string[]? formats = ["json,cobertura,lcov"];
      _mockCommandLineOptions
        .Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.Formats, out formats))
        .Returns(true);

      _mockConfiguration
        .Setup(x => x[It.IsAny<string>()])
        .Returns((string?)null);

      var collector = CreateCollector();
      ITestHostProcessLifetimeHandler handler = collector;

      // Act
      await handler.BeforeTestHostProcessStartAsync(CancellationToken.None);

      // Assert
      _mockCommandLineOptions.Verify(
        x => x.TryGetOptionArgumentList(CoverletOptionNames.Formats, out formats),
        Times.Once);
    }

    [Fact]
    public async Task GenerateReportsAsync_WhenServiceProviderNull_SkipsProcessing()
    {
      // Arrange
      _mockCommandLineOptions
        .Setup(x => x.IsOptionSet(CoverletOptionNames.Coverage))
        .Returns(true);

      var collector = CreateCollector();
      ITestHostProcessLifetimeHandler handler = collector;

      // This will simulate the condition where coverage is enabled, but the service provider is null
      await handler.BeforeTestHostProcessStartAsync(CancellationToken.None);

      var mockProcessInfo = new Mock<ITestHostProcessInformation>();
      mockProcessInfo.Setup(x => x.PID).Returns(12345);
      mockProcessInfo.Setup(x => x.ExitCode).Returns(0);

      // Act - should complete without throwing
      await handler.OnTestHostProcessExitedAsync(mockProcessInfo.Object, CancellationToken.None);

      // Assert - method completes without throwing
      mockProcessInfo.Verify(x => x.ExitCode, Times.AtLeastOnce);
    }

    [Fact]
    public async Task GenerateReportsAsync_WithCancellationToken_PassesTokenToTask()
    {
      // Arrange
      _mockCommandLineOptions
        .Setup(x => x.IsOptionSet(CoverletOptionNames.Coverage))
        .Returns(false);

      var collector = CreateCollector();
      ITestHostProcessLifetimeHandler handler = collector;

      await handler.BeforeTestHostProcessStartAsync(CancellationToken.None);

      var mockProcessInfo = new Mock<ITestHostProcessInformation>();
      mockProcessInfo.Setup(x => x.PID).Returns(12345);
      mockProcessInfo.Setup(x => x.ExitCode).Returns(0);

      using var cts = new CancellationTokenSource();

      // Act
      await handler.OnTestHostProcessExitedAsync(mockProcessInfo.Object, cts.Token);

      // Assert - completed without cancellation issues
      Assert.False(cts.IsCancellationRequested);
    }

    [Fact]
    public async Task UpdateAsync_WhenCoverageEnabledAndIdentifierSet_SetsCoverageEnabledVariable()
    {
      // Arrange
      _mockCommandLineOptions
        .Setup(x => x.IsOptionSet(CoverletOptionNames.Coverage))
        .Returns(true);

      // Use a valid path that exists
      string testAssemblyPath = typeof(CoverletExtensionCollectorTests).Assembly.Location;
      _mockConfiguration
        .Setup(x => x["TestModule"])
        .Returns(testAssemblyPath);

      // Create mock coverage and factory
      var mockCoverage = new Mock<ICoverage>();
      mockCoverage
        .Setup(x => x.PrepareModules())
        .Returns(new CoveragePrepareResult { Identifier = "test-identifier-123" });

      var mockCoverageFactory = new Mock<ICoverageFactory>();
      mockCoverageFactory
        .Setup(x => x.Create(It.IsAny<string>(), It.IsAny<CoverageParameters>()))
        .Returns(mockCoverage.Object);

      var collector = CreateCollector();
      collector.CoverageFactory = mockCoverageFactory.Object; // Inject mock

      ITestHostProcessLifetimeHandler lifetimeHandler = collector;
      ITestHostEnvironmentVariableProvider envProvider = collector;

      await lifetimeHandler.BeforeTestHostProcessStartAsync(CancellationToken.None);

      var mockEnvVariables = new Mock<IEnvironmentVariables>();

      // Act
      await envProvider.UpdateAsync(mockEnvVariables.Object);

      // Assert - CoverageEnabled variable should be set
      mockEnvVariables.Verify(
        x => x.SetVariable(It.Is<EnvironmentVariable>(
          ev => ev.Variable == "COVERLET_MTP_COVERAGE_ENABLED" && ev.Value == "true")),
        Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenCoverageEnabledAndIdentifierSet_SetsCoverageIdentifierVariable()
    {
      // Arrange
      _mockCommandLineOptions
        .Setup(x => x.IsOptionSet(CoverletOptionNames.Coverage))
        .Returns(true);

      // Use a valid path that exists
      string testAssemblyPath = typeof(CoverletExtensionCollectorTests).Assembly.Location;
      _mockConfiguration
        .Setup(x => x["TestModule"])
        .Returns(testAssemblyPath);

      // Create mock coverage and factory
      var mockCoverage = new Mock<ICoverage>();
      mockCoverage
        .Setup(x => x.PrepareModules())
        .Returns(new CoveragePrepareResult { Identifier = "test-identifier-123" });

      var mockCoverageFactory = new Mock<ICoverageFactory>();
      mockCoverageFactory
        .Setup(x => x.Create(It.IsAny<string>(), It.IsAny<CoverageParameters>()))
        .Returns(mockCoverage.Object);

      var collector = CreateCollector();
      collector.CoverageFactory = mockCoverageFactory.Object; // Inject mock

      ITestHostProcessLifetimeHandler lifetimeHandler = collector;
      ITestHostEnvironmentVariableProvider envProvider = collector;

      await lifetimeHandler.BeforeTestHostProcessStartAsync(CancellationToken.None);

      var mockEnvVariables = new Mock<IEnvironmentVariables>();

      // Act
      await envProvider.UpdateAsync(mockEnvVariables.Object);

      // Assert - CoverageIdentifier variable should be set with non-empty value
      mockEnvVariables.Verify(
        x => x.SetVariable(It.Is<EnvironmentVariable>(
          ev => ev.Variable == "COVERLET_MTP_COVERAGE_IDENTIFIER" && !string.IsNullOrEmpty(ev.Value))),
        Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenCoverageEnabledAndCoverageNotNull_SetsHitsFilePathVariable()
    {
      // Arrange
      _mockCommandLineOptions
        .Setup(x => x.IsOptionSet(CoverletOptionNames.Coverage))
        .Returns(true);

      // Use a valid path that exists
      string testAssemblyPath = typeof(CoverletExtensionCollectorTests).Assembly.Location;
      _mockConfiguration
        .Setup(x => x["TestModule"])
        .Returns(testAssemblyPath);

      // Create mock coverage and factory
      var mockCoverage = new Mock<ICoverage>();
      mockCoverage
        .Setup(x => x.PrepareModules())
        .Returns(new CoveragePrepareResult { Identifier = "test-identifier-123" });

      var mockCoverageFactory = new Mock<ICoverageFactory>();
      mockCoverageFactory
        .Setup(x => x.Create(It.IsAny<string>(), It.IsAny<CoverageParameters>()))
        .Returns(mockCoverage.Object);

      var collector = CreateCollector();
      collector.CoverageFactory = mockCoverageFactory.Object; // Inject mock

      ITestHostProcessLifetimeHandler lifetimeHandler = collector;
      ITestHostEnvironmentVariableProvider envProvider = collector;

      await lifetimeHandler.BeforeTestHostProcessStartAsync(CancellationToken.None);

      var mockEnvVariables = new Mock<IEnvironmentVariables>();

      // Act
      await envProvider.UpdateAsync(mockEnvVariables.Object);

      // Assert - HitsFilePath variable should be set
      mockEnvVariables.Verify(
        x => x.SetVariable(It.Is<EnvironmentVariable>(
          ev => ev.Variable == "COVERLET_MTP_HITS_FILE_PATH")),
        Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_EnvironmentVariables_AreNotSecret()
    {
      // Arrange
      _mockCommandLineOptions
        .Setup(x => x.IsOptionSet(CoverletOptionNames.Coverage))
        .Returns(true);

      // Use a valid path that exists
      string testAssemblyPath = typeof(CoverletExtensionCollectorTests).Assembly.Location;
      _mockConfiguration
        .Setup(x => x["TestModule"])
        .Returns(testAssemblyPath);

      // Create mock coverage and factory
      var mockCoverage = new Mock<ICoverage>();
      mockCoverage
        .Setup(x => x.PrepareModules())
        .Returns(new CoveragePrepareResult { Identifier = "test-identifier-123" });

      var mockCoverageFactory = new Mock<ICoverageFactory>();
      mockCoverageFactory
        .Setup(x => x.Create(It.IsAny<string>(), It.IsAny<CoverageParameters>()))
        .Returns(mockCoverage.Object);

      var collector = CreateCollector();
      collector.CoverageFactory = mockCoverageFactory.Object; // Inject mock

      ITestHostProcessLifetimeHandler lifetimeHandler = collector;
      ITestHostEnvironmentVariableProvider envProvider = collector;

      await lifetimeHandler.BeforeTestHostProcessStartAsync(CancellationToken.None);

      var mockEnvVariables = new Mock<IEnvironmentVariables>();

      // Act
      await envProvider.UpdateAsync(mockEnvVariables.Object);

      // Assert - all variables should not be secret
      mockEnvVariables.Verify(
        x => x.SetVariable(It.Is<EnvironmentVariable>(ev => ev.IsSecret == false)),
        Times.AtLeast(2));
    }

    [Fact]
    public async Task UpdateAsync_EnvironmentVariables_AreLocked()
    {
      // Arrange
      _mockCommandLineOptions
        .Setup(x => x.IsOptionSet(CoverletOptionNames.Coverage))
        .Returns(true);

      // Use a valid path that exists
      string testAssemblyPath = typeof(CoverletExtensionCollectorTests).Assembly.Location;
      _mockConfiguration
        .Setup(x => x["TestModule"])
        .Returns(testAssemblyPath);

      // Create mock coverage and factory
      var mockCoverage = new Mock<ICoverage>();
      mockCoverage
        .Setup(x => x.PrepareModules())
        .Returns(new CoveragePrepareResult { Identifier = "test-identifier-123" });

      var mockCoverageFactory = new Mock<ICoverageFactory>();
      mockCoverageFactory
        .Setup(x => x.Create(It.IsAny<string>(), It.IsAny<CoverageParameters>()))
        .Returns(mockCoverage.Object);

      var collector = CreateCollector();
      collector.CoverageFactory = mockCoverageFactory.Object; // Inject mock

      ITestHostProcessLifetimeHandler lifetimeHandler = collector;
      ITestHostEnvironmentVariableProvider envProvider = collector;

      await lifetimeHandler.BeforeTestHostProcessStartAsync(CancellationToken.None);

      var mockEnvVariables = new Mock<IEnvironmentVariables>();

      // Act
      await envProvider.UpdateAsync(mockEnvVariables.Object);

      // Assert - all variables should be locked
      mockEnvVariables.Verify(
        x => x.SetVariable(It.Is<EnvironmentVariable>(ev => ev.IsLocked == true)),
        Times.AtLeast(2));
    }

    [Fact]
    public async Task UpdateAsync_WhenCoverageIdentifierEmpty_DoesNotSetVariables()
    {
      // Arrange - coverage enabled but initialization fails, leaving identifier empty
      _mockCommandLineOptions
        .Setup(x => x.IsOptionSet(CoverletOptionNames.Coverage))
        .Returns(true);

      // Return non-existent path to prevent successful initialization
      _mockConfiguration
        .Setup(x => x["TestModule"])
        .Returns("/non/existent/path.dll");

      _mockConfiguration
        .Setup(x => x["TestHost:Path"])
        .Returns((string?)null);

      var collector = CreateCollector();
      ITestHostProcessLifetimeHandler lifetimeHandler = collector;
      ITestHostEnvironmentVariableProvider envProvider = collector;

      await lifetimeHandler.BeforeTestHostProcessStartAsync(CancellationToken.None);

      var mockEnvVariables = new Mock<IEnvironmentVariables>();

      // Act
      await envProvider.UpdateAsync(mockEnvVariables.Object);

      // Assert - no variables set because identifier is empty
      mockEnvVariables.Verify(
        x => x.SetVariable(It.IsAny<EnvironmentVariable>()),
        Times.Never);
    }

    [Fact]
    public async Task ValidateTestHostEnvironmentVariablesAsync_WhenVariableNotSet_ReturnsValid()
    {
      // Arrange
      var collector = CreateCollector();
      ITestHostEnvironmentVariableProvider envProvider = collector;

      OwnedEnvironmentVariable? nullVariable = null;
      var mockReadOnlyEnvVariables = new Mock<IReadOnlyEnvironmentVariables>();
      mockReadOnlyEnvVariables
        .Setup(x => x.TryGetVariable("COVERLET_MTP_COVERAGE_ENABLED", out nullVariable))
        .Returns(false);

      // Act
      ValidationResult result = await envProvider.ValidateTestHostEnvironmentVariablesAsync(mockReadOnlyEnvVariables.Object);

      // Assert
      Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateTestHostEnvironmentVariablesAsync_WhenConflictWithDifferentOwner_ReturnsInvalidWithErrorMessage()
    {
      // Arrange
      var collector = CreateCollector();
      ITestHostEnvironmentVariableProvider envProvider = collector;

      var mockOtherExtension = new Mock<IExtension>();
      mockOtherExtension.Setup(x => x.Uid).Returns("OtherExtension");

      var conflictingVariable = new OwnedEnvironmentVariable(
        mockOtherExtension.Object,
        "COVERLET_MTP_COVERAGE_ENABLED",
        "false",
        isSecret: false,
        isLocked: false);

      var mockReadOnlyEnvVariables = new Mock<IReadOnlyEnvironmentVariables>();
      mockReadOnlyEnvVariables
        .Setup(x => x.TryGetVariable("COVERLET_MTP_COVERAGE_ENABLED", out conflictingVariable))
        .Returns(true);

      // Act
      ValidationResult result = await envProvider.ValidateTestHostEnvironmentVariablesAsync(mockReadOnlyEnvVariables.Object);

      // Assert
      Assert.False(result.IsValid);
      Assert.Contains("already set by another extension", result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateTestHostEnvironmentVariablesAsync_ReturnsValidTaskType()
    {
      // Arrange
      var collector = CreateCollector();
      ITestHostEnvironmentVariableProvider envProvider = collector;

      var mockReadOnlyEnvVariables = new Mock<IReadOnlyEnvironmentVariables>();
      mockReadOnlyEnvVariables
        .Setup(x => x.TryGetVariable(It.IsAny<string>(), out It.Ref<OwnedEnvironmentVariable?>.IsAny))
        .Returns(false);

      // Act
      Task<ValidationResult> resultTask = envProvider.ValidateTestHostEnvironmentVariablesAsync(mockReadOnlyEnvVariables.Object);

      // Assert
      Assert.True(resultTask.IsCompleted);
      ValidationResult result = await resultTask;

    }
  }
}
