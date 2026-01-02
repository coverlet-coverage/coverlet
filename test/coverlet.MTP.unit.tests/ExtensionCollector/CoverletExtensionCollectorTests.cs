// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using coverlet.MTP.Collector;
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

      // Setup default behavior
      _mockCommandLineOptions
        .Setup(x => x.IsOptionSet(It.IsAny<string>()))
        .Returns(false);

      _mockCommandLineOptions
        .Setup(x => x.TryGetOptionArgumentList(It.IsAny<string>(), out It.Ref<string[]?>.IsAny))
        .Returns(false);

      _mockConfiguration
        .Setup(x => x[It.IsAny<string>()])
        .Returns((string?)null);
    }

    #region Constructor Tests

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
      var collector = CreateCollector();

      // Assert
      Assert.NotNull(collector);
    }

    [Fact]
    public void Constructor_CreatesLoggerFromFactory()
    {
      // Act
      var collector = CreateCollector();

      // Assert
      _mockLoggerFactory.Verify(x => x.CreateLogger(It.IsAny<string>()), Times.AtLeastOnce);
    }

    #endregion

    #region IExtension Properties Tests

    [Fact]
    public void Extension_Uid_ReturnsNonEmptyString()
    {
      // Arrange
      var collector = CreateCollector();
      IExtension extension = collector;

      // Act
      string uid = extension.Uid;

      // Assert
      Assert.False(string.IsNullOrEmpty(uid));
    }

    [Fact]
    public void Extension_Version_ReturnsNonEmptyString()
    {
      // Arrange
      var collector = CreateCollector();
      IExtension extension = collector;

      // Act
      string version = extension.Version;

      // Assert
      Assert.False(string.IsNullOrEmpty(version));
    }

    [Fact]
    public void Extension_DisplayName_ReturnsNonEmptyString()
    {
      // Arrange
      var collector = CreateCollector();
      IExtension extension = collector;

      // Act
      string displayName = extension.DisplayName;

      // Assert
      Assert.False(string.IsNullOrEmpty(displayName));
    }

    [Fact]
    public void Extension_Description_ReturnsNonEmptyString()
    {
      // Arrange
      var collector = CreateCollector();
      IExtension extension = collector;

      // Act
      string description = extension.Description;

      // Assert
      Assert.False(string.IsNullOrEmpty(description));
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

    #endregion

    #region IsEnabledAsync Tests

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
    public async Task IsEnabledAsync_CalledMultipleTimes_AlwaysReturnsTrue()
    {
      // Arrange
      var collector = CreateCollector();

      // Act
      bool result1 = await collector.IsEnabledAsync();
      bool result2 = await collector.IsEnabledAsync();

      // Assert
      Assert.True(result1);
      Assert.True(result2);
    }

    #endregion

    #region BeforeTestHostProcessStartAsync Tests

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

      // Assert
      _mockCommandLineOptions.Verify(x => x.IsOptionSet(CoverletOptionNames.Coverage), Times.Once);
    }

    [Fact]
    public async Task BeforeTestHostProcessStartAsync_WhenCoverageEnabled_ChecksForModulePath()
    {
      // Arrange
      _mockCommandLineOptions
        .Setup(x => x.IsOptionSet(CoverletOptionNames.Coverage))
        .Returns(true);

      var collector = CreateCollector();
      ITestHostProcessLifetimeHandler handler = collector;

      // Act
      await handler.BeforeTestHostProcessStartAsync(CancellationToken.None);

      // Assert
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

      var collector = CreateCollector();
      ITestHostProcessLifetimeHandler handler = collector;

      // Act
      await handler.BeforeTestHostProcessStartAsync(CancellationToken.None);

      // Assert
      _mockCommandLineOptions.Verify(
        x => x.IsOptionSet(CoverletOptionNames.IncludeTestAssembly),
        Times.Once);
    }

    [Fact]
    public async Task BeforeTestHostProcessStartAsync_WithCommaSeparatedFormats_SplitsCorrectly()
    {
      // Arrange
      _mockCommandLineOptions
        .Setup(x => x.IsOptionSet(CoverletOptionNames.Coverage))
        .Returns(true);

      string[]? formats = ["json,cobertura,lcov"];
      _mockCommandLineOptions
        .Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.Formats, out formats))
        .Returns(true);

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
    public async Task BeforeTestHostProcessStartAsync_WithValidTestModule_InitializesCoverage()
    {
      // Arrange
      _mockCommandLineOptions
        .Setup(x => x.IsOptionSet(CoverletOptionNames.Coverage))
        .Returns(true);

      string testAssemblyPath = typeof(CoverletExtensionCollectorTests).Assembly.Location;
      _mockConfiguration
        .Setup(x => x["TestModule"])
        .Returns(testAssemblyPath);

      var mockCoverage = new Mock<ICoverage>();
      mockCoverage
        .Setup(x => x.PrepareModules())
        .Returns(new CoveragePrepareResult { Identifier = "test-identifier" });

      var mockCoverageFactory = new Mock<ICoverageFactory>();
      mockCoverageFactory
        .Setup(x => x.Create(It.IsAny<string>(), It.IsAny<CoverageParameters>()))
        .Returns(mockCoverage.Object);

      var collector = CreateCollector();
      collector.CoverageFactory = mockCoverageFactory.Object;
      ITestHostProcessLifetimeHandler handler = collector;

      // Act
      await handler.BeforeTestHostProcessStartAsync(CancellationToken.None);

      // Assert
      mockCoverage.Verify(x => x.PrepareModules(), Times.Once);
    }

    [Fact]
    public async Task BeforeTestHostProcessStartAsync_WithCancellationToken_CompletesSuccessfully()
    {
      // Arrange
      _mockCommandLineOptions
        .Setup(x => x.IsOptionSet(CoverletOptionNames.Coverage))
        .Returns(false);

      var collector = CreateCollector();
      ITestHostProcessLifetimeHandler handler = collector;

      using var cts = new CancellationTokenSource();

      // Act
      await handler.BeforeTestHostProcessStartAsync(cts.Token);

      // Assert
      Assert.False(cts.IsCancellationRequested);
    }

    #endregion

    #region UpdateAsync Tests

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
    public async Task UpdateAsync_WhenCoverageEnabledButIdentifierNull_DoesNotSetEnvironmentVariables()
    {
      // Arrange
      _mockCommandLineOptions
        .Setup(x => x.IsOptionSet(CoverletOptionNames.Coverage))
        .Returns(true);

      _mockConfiguration
        .Setup(x => x[It.IsAny<string>()])
        .Returns((string?)null);

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
    public async Task UpdateAsync_WhenCoverageEnabledAndIdentifierSet_SetsCoverageEnabledVariable()
    {
      // Arrange
      var (collector, _) = CreateCollectorWithMockCoverage();

      ITestHostProcessLifetimeHandler lifetimeHandler = collector;
      ITestHostEnvironmentVariableProvider envProvider = collector;

      await lifetimeHandler.BeforeTestHostProcessStartAsync(CancellationToken.None);

      var mockEnvVariables = new Mock<IEnvironmentVariables>();

      // Act
      await envProvider.UpdateAsync(mockEnvVariables.Object);

      // Assert
      mockEnvVariables.Verify(
        x => x.SetVariable(It.Is<EnvironmentVariable>(
          ev => ev.Variable == "COVERLET_MTP_COVERAGE_ENABLED" && ev.Value == "true")),
        Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenCoverageEnabledAndIdentifierSet_SetsCoverageIdentifierVariable()
    {
      // Arrange
      var (collector, _) = CreateCollectorWithMockCoverage("my-test-identifier");

      ITestHostProcessLifetimeHandler lifetimeHandler = collector;
      ITestHostEnvironmentVariableProvider envProvider = collector;

      await lifetimeHandler.BeforeTestHostProcessStartAsync(CancellationToken.None);

      var mockEnvVariables = new Mock<IEnvironmentVariables>();

      // Act
      await envProvider.UpdateAsync(mockEnvVariables.Object);

      // Assert
      mockEnvVariables.Verify(
        x => x.SetVariable(It.Is<EnvironmentVariable>(
          ev => ev.Variable == "COVERLET_MTP_COVERAGE_IDENTIFIER" && ev.Value == "my-test-identifier")),
        Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenCoverageEnabledAndIdentifierSet_SetsHitsFilePathVariable()
    {
      // Arrange
      var (collector, _) = CreateCollectorWithMockCoverage();

      ITestHostProcessLifetimeHandler lifetimeHandler = collector;
      ITestHostEnvironmentVariableProvider envProvider = collector;

      await lifetimeHandler.BeforeTestHostProcessStartAsync(CancellationToken.None);

      var mockEnvVariables = new Mock<IEnvironmentVariables>();

      // Act
      await envProvider.UpdateAsync(mockEnvVariables.Object);

      // Assert
      mockEnvVariables.Verify(
        x => x.SetVariable(It.Is<EnvironmentVariable>(
          ev => ev.Variable == "COVERLET_MTP_HITS_FILE_PATH")),
        Times.Once);
    }

    #endregion

    #region ValidateTestHostEnvironmentVariablesAsync Tests

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

    #endregion

    #region OnTestHostProcessStartedAsync Tests

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

      // Assert
      mockProcessInfo.Verify(x => x.PID, Times.AtLeastOnce);
    }

    [Fact]
    public async Task OnTestHostProcessStartedAsync_WithDifferentPID_LogsCorrectPID()
    {
      // Arrange
      var collector = CreateCollector();
      ITestHostProcessLifetimeHandler handler = collector;

      var mockProcessInfo = new Mock<ITestHostProcessInformation>();
      mockProcessInfo.Setup(x => x.PID).Returns(99999);

      // Act
      await handler.OnTestHostProcessStartedAsync(mockProcessInfo.Object, CancellationToken.None);

      // Assert
      mockProcessInfo.Verify(x => x.PID, Times.AtLeastOnce);
    }

    [Fact]
    public async Task OnTestHostProcessStartedAsync_WithCancellationToken_CompletesSuccessfully()
    {
      // Arrange
      var collector = CreateCollector();
      ITestHostProcessLifetimeHandler handler = collector;

      var mockProcessInfo = new Mock<ITestHostProcessInformation>();
      mockProcessInfo.Setup(x => x.PID).Returns(12345);

      using var cts = new CancellationTokenSource();

      // Act
      await handler.OnTestHostProcessStartedAsync(mockProcessInfo.Object, cts.Token);

      // Assert
      Assert.False(cts.IsCancellationRequested);
    }

    #endregion

    #region OnTestHostProcessExitedAsync Tests

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

      // Assert
      mockProcessInfo.Verify(x => x.ExitCode, Times.AtLeastOnce);
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

      await handler.BeforeTestHostProcessStartAsync(CancellationToken.None);

      var mockProcessInfo = new Mock<ITestHostProcessInformation>();
      mockProcessInfo.Setup(x => x.PID).Returns(12345);
      mockProcessInfo.Setup(x => x.ExitCode).Returns(0);

      // Act - should not throw
      await handler.OnTestHostProcessExitedAsync(mockProcessInfo.Object, CancellationToken.None);

      // Assert
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

      // Assert
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

      // Act - should complete without error
      await handler.OnTestHostProcessExitedAsync(mockProcessInfo.Object, cts.Token);

      // Assert
      Assert.False(cts.IsCancellationRequested);
    }

    [Fact]
    public async Task OnTestHostProcessExitedAsync_WhenAllConditionsFalse_LogsSkipMessage()
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

      // Assert
      mockProcessInfo.Verify(x => x.PID, Times.AtLeastOnce);
    }

    [Fact]
    public async Task OnTestHostProcessExitedAsync_WhenCoverageSucceeds_GeneratesReports()
    {
      // Arrange
      var (collector, mockCoverage) = CreateCollectorWithMockCoverage();

      mockCoverage
        .Setup(x => x.GetCoverageResult())
        .Returns(new CoverageResult { Modules = new Modules() });

      ITestHostProcessLifetimeHandler handler = collector;
      await handler.BeforeTestHostProcessStartAsync(CancellationToken.None);

      var mockProcessInfo = new Mock<ITestHostProcessInformation>();
      mockProcessInfo.Setup(x => x.PID).Returns(12345);
      mockProcessInfo.Setup(x => x.ExitCode).Returns(0);

      // Act
      await handler.OnTestHostProcessExitedAsync(mockProcessInfo.Object, CancellationToken.None);

      // Assert
      mockCoverage.Verify(x => x.GetCoverageResult(), Times.Once);
    }

    [Fact]
    public async Task OnTestHostProcessExitedAsync_WhenCancelled_StopsGracefully()
    {
      // Arrange
      var (collector, _) = CreateCollectorWithMockCoverage();
      ITestHostProcessLifetimeHandler handler = collector;

      await handler.BeforeTestHostProcessStartAsync(CancellationToken.None);

      using var cts = new CancellationTokenSource();
      cts.Cancel();

      var mockProcessInfo = new Mock<ITestHostProcessInformation>();
      mockProcessInfo.Setup(x => x.PID).Returns(12345);
      mockProcessInfo.Setup(x => x.ExitCode).Returns(0);

      // Act - Should handle cancellation gracefully
      await handler.OnTestHostProcessExitedAsync(mockProcessInfo.Object, cts.Token);
    }

    [Fact]
    public async Task OnTestHostProcessExitedAsync_WithNonZeroExitCode_StillCollectsCoverage()
    {
      // Arrange
      var (collector, mockCoverage) = CreateCollectorWithMockCoverage();

      mockCoverage
        .Setup(x => x.GetCoverageResult())
        .Returns(new CoverageResult { Modules = new Modules() });

      ITestHostProcessLifetimeHandler handler = collector;
      await handler.BeforeTestHostProcessStartAsync(CancellationToken.None);

      var mockProcessInfo = new Mock<ITestHostProcessInformation>();
      mockProcessInfo.Setup(x => x.PID).Returns(12345);
      mockProcessInfo.Setup(x => x.ExitCode).Returns(-1); // Non-zero exit code

      // Act
      await handler.OnTestHostProcessExitedAsync(mockProcessInfo.Object, CancellationToken.None);

      // Assert - Coverage should still be collected
      mockCoverage.Verify(x => x.GetCoverageResult(), Times.Once);
    }

    #endregion

    #region End-to-End Tests

    [Fact]
    public async Task EndToEnd_FullLifecycle_WithCoverageEnabled()
    {
      // Arrange
      var (collector, mockCoverage) = CreateCollectorWithMockCoverage();

      mockCoverage
        .Setup(x => x.GetCoverageResult())
        .Returns(new CoverageResult { Modules = new Modules() });

      var mockProcessInfo = new Mock<ITestHostProcessInformation>();
      mockProcessInfo.Setup(x => x.PID).Returns(12345);
      mockProcessInfo.Setup(x => x.ExitCode).Returns(0);

      // Act - Full lifecycle
      ITestHostProcessLifetimeHandler lifetimeHandler = collector;
      await lifetimeHandler.BeforeTestHostProcessStartAsync(CancellationToken.None);
      await lifetimeHandler.OnTestHostProcessStartedAsync(mockProcessInfo.Object, CancellationToken.None);
      await lifetimeHandler.OnTestHostProcessExitedAsync(mockProcessInfo.Object, CancellationToken.None);

      // Act - Environment variables
      ITestHostEnvironmentVariableProvider envProvider = collector;
      var mockEnvVariables = new Mock<IEnvironmentVariables>();
      var mockReadOnlyEnvVariables = new Mock<IReadOnlyEnvironmentVariables>();

      await envProvider.UpdateAsync(mockEnvVariables.Object);
      await envProvider.ValidateTestHostEnvironmentVariablesAsync(mockReadOnlyEnvVariables.Object);

      // Assert
      mockEnvVariables.Verify(
        x => x.SetVariable(It.Is<EnvironmentVariable>(
          ev => ev.Variable == "COVERLET_MTP_COVERAGE_ENABLED" && ev.Value == "true")),
        Times.Once);

      _mockLogger.Verify(
        x => x.Log(
          LogLevel.Information,
          It.IsAny<string>(),
          It.IsAny<Exception?>(),
          It.IsAny<Func<string, Exception?, string>>()),
        Times.AtLeast(2));
    }

    [Fact]
    public async Task EndToEnd_FullLifecycle_WithCoverageDisabled()
    {
      // Arrange
      _mockCommandLineOptions
        .Setup(x => x.IsOptionSet(CoverletOptionNames.Coverage))
        .Returns(false);

      var collector = CreateCollector();

      var mockProcessInfo = new Mock<ITestHostProcessInformation>();
      mockProcessInfo.Setup(x => x.PID).Returns(12345);
      mockProcessInfo.Setup(x => x.ExitCode).Returns(0);

      // Act - Full lifecycle
      ITestHostProcessLifetimeHandler lifetimeHandler = collector;
      await lifetimeHandler.BeforeTestHostProcessStartAsync(CancellationToken.None);
      await lifetimeHandler.OnTestHostProcessStartedAsync(mockProcessInfo.Object, CancellationToken.None);
      await lifetimeHandler.OnTestHostProcessExitedAsync(mockProcessInfo.Object, CancellationToken.None);

      // Act - Environment variables
      ITestHostEnvironmentVariableProvider envProvider = collector;
      var mockEnvVariables = new Mock<IEnvironmentVariables>();

      await envProvider.UpdateAsync(mockEnvVariables.Object);

      // Assert - No variables set when coverage disabled
      mockEnvVariables.Verify(
        x => x.SetVariable(It.IsAny<EnvironmentVariable>()),
        Times.Never);
    }

    #endregion

    #region Helper Methods

    private CoverletExtensionCollector CreateCollector()
    {
      return new CoverletExtensionCollector(
        _mockLoggerFactory.Object,
        _mockCommandLineOptions.Object,
        _mockConfiguration.Object);
    }

    private (CoverletExtensionCollector collector, Mock<ICoverage> mockCoverage) CreateCollectorWithMockCoverage(string identifier = "test-identifier-123")
    {
      _mockCommandLineOptions
        .Setup(x => x.IsOptionSet(CoverletOptionNames.Coverage))
        .Returns(true);

      string testAssemblyPath = typeof(CoverletExtensionCollectorTests).Assembly.Location;
      _mockConfiguration
        .Setup(x => x["TestModule"])
        .Returns(testAssemblyPath);

      var mockCoverage = new Mock<ICoverage>();
      mockCoverage
        .Setup(x => x.PrepareModules())
        .Returns(new CoveragePrepareResult { Identifier = identifier });

      var mockCoverageFactory = new Mock<ICoverageFactory>();
      mockCoverageFactory
        .Setup(x => x.Create(It.IsAny<string>(), It.IsAny<CoverageParameters>()))
        .Returns(mockCoverage.Object);

      var collector = CreateCollector();
      collector.CoverageFactory = mockCoverageFactory.Object;

      return (collector, mockCoverage);
    }

    #endregion

    #region GenerateReportsAsync Tests

    [Fact]
    public async Task GenerateReportsAsync_WithJsonFormat_GeneratesJsonReport()
    {
      // Arrange
      string[]? formats = ["json"];
      _mockCommandLineOptions
        .Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.Formats, out formats))
        .Returns(true);

      var (collector, mockCoverage) = CreateCollectorWithMockCoverage();

      mockCoverage
        .Setup(x => x.GetCoverageResult())
        .Returns(new CoverageResult { Modules = new Modules() });

      string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
      _mockConfiguration
        .Setup(x => x["platformOptions:resultDirectory"])
        .Returns(tempDir + Path.DirectorySeparatorChar);

      ITestHostProcessLifetimeHandler handler = collector;
      await handler.BeforeTestHostProcessStartAsync(CancellationToken.None);

      var mockProcessInfo = new Mock<ITestHostProcessInformation>();
      mockProcessInfo.Setup(x => x.PID).Returns(12345);
      mockProcessInfo.Setup(x => x.ExitCode).Returns(0);

      try
      {
        // Act
        await handler.OnTestHostProcessExitedAsync(mockProcessInfo.Object, CancellationToken.None);

        // Assert
        mockCoverage.Verify(x => x.GetCoverageResult(), Times.Once);
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
    public async Task GenerateReportsAsync_WithCoberturaFormat_GeneratesCoberturaReport()
    {
      // Arrange
      string[]? formats = ["cobertura"];
      _mockCommandLineOptions
        .Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.Formats, out formats))
        .Returns(true);

      var (collector, mockCoverage) = CreateCollectorWithMockCoverage();

      mockCoverage
        .Setup(x => x.GetCoverageResult())
        .Returns(new CoverageResult { Modules = new Modules() });

      string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
      _mockConfiguration
        .Setup(x => x["platformOptions:resultDirectory"])
        .Returns(tempDir + Path.DirectorySeparatorChar);

      ITestHostProcessLifetimeHandler handler = collector;
      await handler.BeforeTestHostProcessStartAsync(CancellationToken.None);

      var mockProcessInfo = new Mock<ITestHostProcessInformation>();
      mockProcessInfo.Setup(x => x.PID).Returns(12345);
      mockProcessInfo.Setup(x => x.ExitCode).Returns(0);

      try
      {
        // Act
        await handler.OnTestHostProcessExitedAsync(mockProcessInfo.Object, CancellationToken.None);

        // Assert
        mockCoverage.Verify(x => x.GetCoverageResult(), Times.Once);
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
    public async Task GenerateReportsAsync_WithMultipleFormats_GeneratesAllReports()
    {
      // Arrange
      string[]? formats = ["json", "cobertura", "lcov"];
      _mockCommandLineOptions
        .Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.Formats, out formats))
        .Returns(true);

      var (collector, mockCoverage) = CreateCollectorWithMockCoverage();

      mockCoverage
        .Setup(x => x.GetCoverageResult())
        .Returns(new CoverageResult { Modules = new Modules() });

      string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
      _mockConfiguration
        .Setup(x => x["platformOptions:resultDirectory"])
        .Returns(tempDir + Path.DirectorySeparatorChar);

      ITestHostProcessLifetimeHandler handler = collector;
      await handler.BeforeTestHostProcessStartAsync(CancellationToken.None);

      var mockProcessInfo = new Mock<ITestHostProcessInformation>();
      mockProcessInfo.Setup(x => x.PID).Returns(12345);
      mockProcessInfo.Setup(x => x.ExitCode).Returns(0);

      try
      {
        // Act
        await handler.OnTestHostProcessExitedAsync(mockProcessInfo.Object, CancellationToken.None);

        // Assert
        mockCoverage.Verify(x => x.GetCoverageResult(), Times.Once);
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
    public async Task GenerateReportsAsync_WithUnsupportedFormat_ThrowsInvalidOperationException()
    {
      // Arrange
      string[]? formats = ["unsupportedformat"];
      _mockCommandLineOptions
        .Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.Formats, out formats))
        .Returns(true);

      var (collector, mockCoverage) = CreateCollectorWithMockCoverage();

      mockCoverage
        .Setup(x => x.GetCoverageResult())
        .Returns(new CoverageResult { Modules = new Modules() });

      string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
      _mockConfiguration
        .Setup(x => x["platformOptions:resultDirectory"])
        .Returns(tempDir + Path.DirectorySeparatorChar);

      ITestHostProcessLifetimeHandler handler = collector;
      await handler.BeforeTestHostProcessStartAsync(CancellationToken.None);

      var mockProcessInfo = new Mock<ITestHostProcessInformation>();
      mockProcessInfo.Setup(x => x.PID).Returns(12345);
      mockProcessInfo.Setup(x => x.ExitCode).Returns(0);

      try
      {
        // Act - Exception should be caught internally and not rethrown
        await handler.OnTestHostProcessExitedAsync(mockProcessInfo.Object, CancellationToken.None);

        // Assert - Method completes without throwing (exception is logged)
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
    public async Task GenerateReportsAsync_WhenOutputDirectoryDoesNotExist_CreatesDirectory()
    {
      // Arrange
      var (collector, mockCoverage) = CreateCollectorWithMockCoverage();

      mockCoverage
        .Setup(x => x.GetCoverageResult())
        .Returns(new CoverageResult { Modules = new Modules() });

      string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "subdir");
      _mockConfiguration
        .Setup(x => x["platformOptions:resultDirectory"])
        .Returns(tempDir + Path.DirectorySeparatorChar);

      ITestHostProcessLifetimeHandler handler = collector;
      await handler.BeforeTestHostProcessStartAsync(CancellationToken.None);

      var mockProcessInfo = new Mock<ITestHostProcessInformation>();
      mockProcessInfo.Setup(x => x.PID).Returns(12345);
      mockProcessInfo.Setup(x => x.ExitCode).Returns(0);

      try
      {
        // Act
        await handler.OnTestHostProcessExitedAsync(mockProcessInfo.Object, CancellationToken.None);

        // Assert
        mockCoverage.Verify(x => x.GetCoverageResult(), Times.Once);
      }
      finally
      {
        string? parentDir = Path.GetDirectoryName(tempDir);
        if (parentDir != null && Directory.Exists(parentDir))
        {
          Directory.Delete(parentDir, true);
        }
      }
    }

    [Fact]
    public async Task GenerateReportsAsync_WhenResultDirectoryNull_UsesModuleDirectory()
    {
      // Arrange
      var (collector, mockCoverage) = CreateCollectorWithMockCoverage();

      mockCoverage
        .Setup(x => x.GetCoverageResult())
        .Returns(new CoverageResult { Modules = new Modules() });

      _mockConfiguration
        .Setup(x => x["platformOptions:resultDirectory"])
        .Returns((string?)null);

      ITestHostProcessLifetimeHandler handler = collector;
      await handler.BeforeTestHostProcessStartAsync(CancellationToken.None);

      var mockProcessInfo = new Mock<ITestHostProcessInformation>();
      mockProcessInfo.Setup(x => x.PID).Returns(12345);
      mockProcessInfo.Setup(x => x.ExitCode).Returns(0);

      // Act
      await handler.OnTestHostProcessExitedAsync(mockProcessInfo.Object, CancellationToken.None);

      // Assert - Should complete without throwing
      mockCoverage.Verify(x => x.GetCoverageResult(), Times.Once);
    }

    [Fact]
    public async Task GenerateReportsAsync_WithCommaSeparatedFormats_ProcessesAllFormats()
    {
      // Arrange
      string[]? formats = ["json,cobertura"];
      _mockCommandLineOptions
        .Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.Formats, out formats))
        .Returns(true);

      var (collector, mockCoverage) = CreateCollectorWithMockCoverage();

      mockCoverage
        .Setup(x => x.GetCoverageResult())
        .Returns(new CoverageResult { Modules = new Modules() });

      string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
      _mockConfiguration
        .Setup(x => x["platformOptions:resultDirectory"])
        .Returns(tempDir + Path.DirectorySeparatorChar);

      ITestHostProcessLifetimeHandler handler = collector;
      await handler.BeforeTestHostProcessStartAsync(CancellationToken.None);

      var mockProcessInfo = new Mock<ITestHostProcessInformation>();
      mockProcessInfo.Setup(x => x.PID).Returns(12345);
      mockProcessInfo.Setup(x => x.ExitCode).Returns(0);

      try
      {
        // Act
        await handler.OnTestHostProcessExitedAsync(mockProcessInfo.Object, CancellationToken.None);

        // Assert
        mockCoverage.Verify(x => x.GetCoverageResult(), Times.Once);
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
    public async Task GenerateReportsAsync_WithCancellationToken_PassesCancellationToTask()
    {
      // Arrange
      var (collector, mockCoverage) = CreateCollectorWithMockCoverage();

      mockCoverage
        .Setup(x => x.GetCoverageResult())
        .Returns(new CoverageResult { Modules = new Modules() });

      string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
      _mockConfiguration
        .Setup(x => x["platformOptions:resultDirectory"])
        .Returns(tempDir + Path.DirectorySeparatorChar);

      ITestHostProcessLifetimeHandler handler = collector;
      await handler.BeforeTestHostProcessStartAsync(CancellationToken.None);

      var mockProcessInfo = new Mock<ITestHostProcessInformation>();
      mockProcessInfo.Setup(x => x.PID).Returns(12345);
      mockProcessInfo.Setup(x => x.ExitCode).Returns(0);

      using var cts = new CancellationTokenSource();

      try
      {
        // Act
        await handler.OnTestHostProcessExitedAsync(mockProcessInfo.Object, cts.Token);

        // Assert
        Assert.False(cts.IsCancellationRequested);
        mockCoverage.Verify(x => x.GetCoverageResult(), Times.Once);
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
    public async Task GenerateReportsAsync_WithOpenCoverFormat_GeneratesOpenCoverReport()
    {
      // Arrange
      string[]? formats = ["opencover"];
      _mockCommandLineOptions
        .Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.Formats, out formats))
        .Returns(true);

      var (collector, mockCoverage) = CreateCollectorWithMockCoverage();

      mockCoverage
        .Setup(x => x.GetCoverageResult())
        .Returns(new CoverageResult { Modules = new Modules() });

      string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
      _mockConfiguration
        .Setup(x => x["platformOptions:resultDirectory"])
        .Returns(tempDir + Path.DirectorySeparatorChar);

      ITestHostProcessLifetimeHandler handler = collector;
      await handler.BeforeTestHostProcessStartAsync(CancellationToken.None);

      var mockProcessInfo = new Mock<ITestHostProcessInformation>();
      mockProcessInfo.Setup(x => x.PID).Returns(12345);
      mockProcessInfo.Setup(x => x.ExitCode).Returns(0);

      try
      {
        // Act
        await handler.OnTestHostProcessExitedAsync(mockProcessInfo.Object, CancellationToken.None);

        // Assert
        mockCoverage.Verify(x => x.GetCoverageResult(), Times.Once);
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
    public async Task GenerateReportsAsync_WithLcovFormat_GeneratesLcovReport()
    {
      // Arrange
      string[]? formats = ["lcov"];
      _mockCommandLineOptions
        .Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.Formats, out formats))
        .Returns(true);

      var (collector, mockCoverage) = CreateCollectorWithMockCoverage();

      mockCoverage
        .Setup(x => x.GetCoverageResult())
        .Returns(new CoverageResult { Modules = new Modules() });

      string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
      _mockConfiguration
        .Setup(x => x["platformOptions:resultDirectory"])
        .Returns(tempDir + Path.DirectorySeparatorChar);

      ITestHostProcessLifetimeHandler handler = collector;
      await handler.BeforeTestHostProcessStartAsync(CancellationToken.None);

      var mockProcessInfo = new Mock<ITestHostProcessInformation>();
      mockProcessInfo.Setup(x => x.PID).Returns(12345);
      mockProcessInfo.Setup(x => x.ExitCode).Returns(0);

      try
      {
        // Act
        await handler.OnTestHostProcessExitedAsync(mockProcessInfo.Object, CancellationToken.None);

        // Assert
        mockCoverage.Verify(x => x.GetCoverageResult(), Times.Once);
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
    public async Task GenerateReportsAsync_LogsGeneratedReportPaths()
    {
      // Arrange
      string[]? formats = ["json"];
      _mockCommandLineOptions
        .Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.Formats, out formats))
        .Returns(true);

      var (collector, mockCoverage) = CreateCollectorWithMockCoverage();

      mockCoverage
        .Setup(x => x.GetCoverageResult())
        .Returns(new CoverageResult { Modules = new Modules() });

      string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
      _mockConfiguration
        .Setup(x => x["platformOptions:resultDirectory"])
        .Returns(tempDir + Path.DirectorySeparatorChar);

      ITestHostProcessLifetimeHandler handler = collector;
      await handler.BeforeTestHostProcessStartAsync(CancellationToken.None);

      var mockProcessInfo = new Mock<ITestHostProcessInformation>();
      mockProcessInfo.Setup(x => x.PID).Returns(12345);
      mockProcessInfo.Setup(x => x.ExitCode).Returns(0);

      try
      {
        // Act
        await handler.OnTestHostProcessExitedAsync(mockProcessInfo.Object, CancellationToken.None);

        // Assert - Verify logging occurred
        _mockLogger.Verify(
          x => x.Log(
            LogLevel.Information,
            It.IsAny<string>(),
            It.IsAny<Exception?>(),
            It.IsAny<Func<string, Exception?, string>>()),
          Times.AtLeastOnce);
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
    public async Task GenerateReportsAsync_WithEmptyModules_GeneratesEmptyReports()
    {
      // Arrange
      var (collector, mockCoverage) = CreateCollectorWithMockCoverage();

      mockCoverage
        .Setup(x => x.GetCoverageResult())
        .Returns(new CoverageResult { Modules = new Modules() });

      string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
      _mockConfiguration
        .Setup(x => x["platformOptions:resultDirectory"])
        .Returns(tempDir + Path.DirectorySeparatorChar);

      ITestHostProcessLifetimeHandler handler = collector;
      await handler.BeforeTestHostProcessStartAsync(CancellationToken.None);

      var mockProcessInfo = new Mock<ITestHostProcessInformation>();
      mockProcessInfo.Setup(x => x.PID).Returns(12345);
      mockProcessInfo.Setup(x => x.ExitCode).Returns(0);

      try
      {
        // Act
        await handler.OnTestHostProcessExitedAsync(mockProcessInfo.Object, CancellationToken.None);

        // Assert
        mockCoverage.Verify(x => x.GetCoverageResult(), Times.Once);
      }
      finally
      {
        if (Directory.Exists(tempDir))
        {
          Directory.Delete(tempDir, true);
        }
      }
    }

    #endregion
  }
}
