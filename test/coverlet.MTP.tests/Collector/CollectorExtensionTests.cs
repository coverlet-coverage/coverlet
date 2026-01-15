// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Coverlet.Core;
using Coverlet.Core.Abstractions;
using Coverlet.MTP.CommandLine;
using Coverlet.MTP.EnvironmentVariables;
using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Configurations;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.TestHostControllers;
using Microsoft.Testing.Platform.Logging;
using Microsoft.Testing.Platform.OutputDevice;
using Moq;
using Xunit;

namespace Coverlet.MTP.Collector.Tests;

public class CollectorExtensionTests
{
  private readonly Mock<ILoggerFactory> _mockLoggerFactory;
  private readonly Mock<Microsoft.Testing.Platform.Logging.ILogger> _mockLogger;
  private readonly Mock<ICommandLineOptions> _mockCommandLineOptions;
  private readonly Mock<IConfiguration> _mockConfiguration;
  private readonly Mock<IFileSystem> _mockFileSystem;
  private readonly Mock<IOutputDevice> _mockOutputDevice;

  // Simulated test module path - no real file created
  private const string SimulatedTestModulePath = "/fake/path/test.dll";
  private const string SimulatedTestModuleDirectory = "/fake/path";

  public CollectorExtensionTests()
  {
    _mockLoggerFactory = new Mock<ILoggerFactory>();
    _mockLogger = new Mock<Microsoft.Testing.Platform.Logging.ILogger>();
    _mockCommandLineOptions = new Mock<ICommandLineOptions>();
    _mockConfiguration = new Mock<IConfiguration>();
    _mockFileSystem = new Mock<IFileSystem>();
    _mockOutputDevice = new Mock<IOutputDevice>();

    _mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>()))
      .Returns(_mockLogger.Object);

    // Setup default behaviors
    _mockCommandLineOptions
      .Setup(x => x.IsOptionSet(It.IsAny<string>()))
      .Returns(false);

    _mockCommandLineOptions
      .Setup(x => x.TryGetOptionArgumentList(It.IsAny<string>(), out It.Ref<string[]?>.IsAny))
      .Returns(false);

    _mockConfiguration
      .Setup(x => x[It.IsAny<string>()])
      .Returns((string?)null);

    // Mock file system - simulate file exists without real I/O
    _mockFileSystem
      .Setup(x => x.Exists(SimulatedTestModulePath))
      .Returns(true);

    // Mock directory exists for report generation
    _mockFileSystem
      .Setup(x => x.Exists(SimulatedTestModuleDirectory))
      .Returns(true);
  }

  #region Helper Methods - No Real File I/O

  private void SetupTestModuleConfiguration(string modulePath = SimulatedTestModulePath)
  {
    _mockConfiguration.Setup(x => x["TestModule"]).Returns(modulePath);
    _mockConfiguration.Setup(x => x["TestResultDirectory"]).Returns(SimulatedTestModuleDirectory);
    _mockFileSystem.Setup(x => x.Exists(modulePath)).Returns(true);
  }

  private void SetupDefaultCommandLineOptions()
  {
    string[]? formats = null!;
    _mockCommandLineOptions
      .Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.Formats, out formats))
      .Returns(false);

    string[]? includes = null!;
    _mockCommandLineOptions
      .Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.Include, out includes))
      .Returns(false);

    string[]? excludes = null!;
    _mockCommandLineOptions
      .Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.Exclude, out excludes))
      .Returns(false);

    _mockCommandLineOptions.Setup(x => x.IsOptionSet(CoverletOptionNames.IncludeTestAssembly)).Returns(false);
  }

  private (CollectorExtension collector, Mock<ICoverage> mockCoverage) CreateCollectorWithMockCoverage(
    string identifier = "test-identifier-123")
  {
    _mockCommandLineOptions
      .Setup(x => x.IsOptionSet(CoverletOptionNames.Coverage))
      .Returns(true);

    SetupTestModuleConfiguration();
    SetupDefaultCommandLineOptions();

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

  private CollectorExtension CreateCollector()
  {
    return new CollectorExtension(
      _mockLoggerFactory.Object,
      _mockCommandLineOptions.Object,
      _mockOutputDevice.Object,
      _mockConfiguration.Object,
      _mockFileSystem.Object);  // Inject the mock file system
  }

  #endregion

  #region Constructor Tests

  [Fact]
  public void ConstructorThrowsArgumentNullExceptionWhenLoggerFactoryIsNull()
  {
    ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
      new CollectorExtension(null!, _mockCommandLineOptions.Object, _mockOutputDevice.Object, _mockConfiguration.Object));

    Assert.Equal("loggerFactory", exception.ParamName);
  }

  [Fact]
  public void ConstructorThrowsArgumentNullExceptionWhenCommandLineOptionsIsNull()
  {
    ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
      new CollectorExtension(_mockLoggerFactory.Object, null!, _mockOutputDevice.Object, _mockConfiguration.Object));

    Assert.Equal("commandLineOptions", exception.ParamName);
  }

  [Fact]
  public void ConstructorThrowsArgumentNullExceptionWhenConfigurationIsNull()
  {
    Assert.Throws<ArgumentNullException>(() =>
      new CollectorExtension(_mockLoggerFactory.Object, _mockCommandLineOptions.Object, _mockOutputDevice.Object, null!));
  }

  [Fact]
  public void ConstructorInitializesSuccessfullyWithValidParameters()
  {
    var collector = CreateCollector();

    Assert.NotNull(collector);
    _mockLoggerFactory.Verify(x => x.CreateLogger(It.IsAny<string>()), Times.AtLeastOnce);
  }

  #endregion

  #region IExtension Properties Tests

  [Theory]
  [InlineData(nameof(IExtension.Uid))]
  [InlineData(nameof(IExtension.Version))]
  [InlineData(nameof(IExtension.DisplayName))]
  [InlineData(nameof(IExtension.Description))]
  public void ExtensionPropertyReturnsNonEmptyString(string propertyName)
  {
    var collector = CreateCollector();
    IExtension extension = collector;

    string value = propertyName switch
    {
      nameof(IExtension.Uid) => extension.Uid,
      nameof(IExtension.Version) => extension.Version,
      nameof(IExtension.DisplayName) => extension.DisplayName,
      nameof(IExtension.Description) => extension.Description,
      _ => throw new ArgumentException($"Unknown property: {propertyName}")
    };

    Assert.False(string.IsNullOrEmpty(value));
  }

  #endregion

  #region IsEnabledAsync Tests

  [Fact]
  public async Task IsEnabledAsyncReturnsTrue()
  {
    var collector = CreateCollector();

    bool result = await collector.IsEnabledAsync();

    Assert.True(result);
  }

  #endregion

  #region BeforeTestHostProcessStartAsync Tests

  [Fact]
  public async Task BeforeTestHostProcessStartAsyncWhenCoverageNotEnabledReturnsWithoutInstrumentation()
  {
    _mockCommandLineOptions.Setup(x => x.IsOptionSet(CoverletOptionNames.Coverage)).Returns(false);

    var collector = CreateCollector();
    ITestHostProcessLifetimeHandler handler = collector;

    await handler.BeforeTestHostProcessStartAsync(CancellationToken.None);

    _mockCommandLineOptions.Verify(x => x.IsOptionSet(CoverletOptionNames.Coverage), Times.Once);
  }

  [Fact]
  public async Task BeforeTestHostProcessStartAsyncWhenCoverageEnabledButNoModulePathDisablesCoverage()
  {
    _mockCommandLineOptions.Setup(x => x.IsOptionSet(CoverletOptionNames.Coverage)).Returns(true);
    _mockConfiguration.Setup(x => x[It.IsAny<string>()]).Returns((string?)null);

    var collector = CreateCollector();
    ITestHostProcessLifetimeHandler handler = collector;

    await handler.BeforeTestHostProcessStartAsync(CancellationToken.None);

    // Verify no environment variables are set (coverage disabled)
    ITestHostEnvironmentVariableProvider envProvider = collector;
    var mockEnvVariables = new Mock<IEnvironmentVariables>();
    await envProvider.UpdateAsync(mockEnvVariables.Object);

    mockEnvVariables.Verify(x => x.SetVariable(It.IsAny<EnvironmentVariable>()), Times.Never);
  }

  [Fact]
  public async Task BeforeTestHostProcessStartAsyncWithValidTestModuleInitializesCoverage()
  {
    // No real file created - uses mocked coverage
    var (collector, mockCoverage) = CreateCollectorWithMockCoverage();
    ITestHostProcessLifetimeHandler handler = collector;

    await handler.BeforeTestHostProcessStartAsync(CancellationToken.None);

    mockCoverage.Verify(x => x.PrepareModules(), Times.Once);
  }

  [Fact]
  public async Task BeforeTestHostProcessStartAsyncWhenExceptionThrownDisablesCoverage()
  {
    _mockCommandLineOptions.Setup(x => x.IsOptionSet(CoverletOptionNames.Coverage)).Returns(true);
    SetupTestModuleConfiguration();

    var mockCoverageFactory = new Mock<ICoverageFactory>();
    mockCoverageFactory
      .Setup(x => x.Create(It.IsAny<string>(), It.IsAny<CoverageParameters>()))
      .Throws(new InvalidOperationException("Test exception"));

    var collector = CreateCollector();
    collector.CoverageFactory = mockCoverageFactory.Object;

    ITestHostProcessLifetimeHandler handler = collector;

    // Should not throw, just disable coverage
    await handler.BeforeTestHostProcessStartAsync(CancellationToken.None);

    // Verify coverage is disabled
    ITestHostEnvironmentVariableProvider envProvider = collector;
    var mockEnvVariables = new Mock<IEnvironmentVariables>();
    await envProvider.UpdateAsync(mockEnvVariables.Object);

    mockEnvVariables.Verify(x => x.SetVariable(It.IsAny<EnvironmentVariable>()), Times.Never);
  }

  [Fact]
  public async Task BeforeTestHostProcessStartAsyncParsesMultipleFormats()
  {
    // Note: ParseCommandLineOptions is currently disabled in the implementation.
    // This test verifies that BeforeTestHostProcessStartAsync completes successfully
    // when coverage is enabled with a valid test module, regardless of format options.
    _mockCommandLineOptions.Setup(x => x.IsOptionSet(CoverletOptionNames.Coverage)).Returns(true);
    SetupTestModuleConfiguration();
    SetupDefaultCommandLineOptions();

    var mockCoverage = new Mock<ICoverage>();
    mockCoverage.Setup(x => x.PrepareModules()).Returns(new CoveragePrepareResult { Identifier = "test-id" });

    var mockCoverageFactory = new Mock<ICoverageFactory>();
    mockCoverageFactory
      .Setup(x => x.Create(It.IsAny<string>(), It.IsAny<CoverageParameters>()))
      .Returns(mockCoverage.Object);

    var collector = CreateCollector();
    collector.CoverageFactory = mockCoverageFactory.Object;

    ITestHostProcessLifetimeHandler handler = collector;
    await handler.BeforeTestHostProcessStartAsync(CancellationToken.None);

    // Verify that coverage was enabled and modules were prepared
    _mockCommandLineOptions.Verify(x => x.IsOptionSet(CoverletOptionNames.Coverage), Times.Once);
    mockCoverage.Verify(x => x.PrepareModules(), Times.Once);
  }

  #endregion

  #region UpdateAsync Tests

  [Fact]
  public async Task UpdateAsyncWhenCoverageNotEnabledDoesNotSetEnvironmentVariables()
  {
    _mockCommandLineOptions.Setup(x => x.IsOptionSet(CoverletOptionNames.Coverage)).Returns(false);

    var collector = CreateCollector();
    ITestHostProcessLifetimeHandler lifetimeHandler = collector;
    ITestHostEnvironmentVariableProvider envProvider = collector;

    await lifetimeHandler.BeforeTestHostProcessStartAsync(CancellationToken.None);

    var mockEnvVariables = new Mock<IEnvironmentVariables>();
    await envProvider.UpdateAsync(mockEnvVariables.Object);

    mockEnvVariables.Verify(x => x.SetVariable(It.IsAny<EnvironmentVariable>()), Times.Never);
  }

  [Fact]
  public async Task UpdateAsyncWhenCoverageEnabledSetsAllRequiredEnvironmentVariables()
  {
    var (collector, _) = CreateCollectorWithMockCoverage("my-test-identifier");

    ITestHostProcessLifetimeHandler lifetimeHandler = collector;
    ITestHostEnvironmentVariableProvider envProvider = collector;

    await lifetimeHandler.BeforeTestHostProcessStartAsync(CancellationToken.None);

    var capturedVariables = new List<EnvironmentVariable>();
    var mockEnvVariables = new Mock<IEnvironmentVariables>();
    mockEnvVariables
      .Setup(x => x.SetVariable(It.IsAny<EnvironmentVariable>()))
      .Callback<EnvironmentVariable>(capturedVariables.Add);

    await envProvider.UpdateAsync(mockEnvVariables.Object);

    Assert.Contains(capturedVariables, v =>
      v.Variable == CoverletMtpEnvironmentVariables.CoverageEnabled && v.Value == "true");
    Assert.Contains(capturedVariables, v =>
      v.Variable == CoverletMtpEnvironmentVariables.CoverageIdentifier && v.Value == "my-test-identifier");
    Assert.Contains(capturedVariables, v =>
      v.Variable == CoverletMtpEnvironmentVariables.HitsFilePath);
  }

  [Fact]
  public async Task UpdateAsyncSetsVariablesInCorrectOrder()
  {
    var (collector, _) = CreateCollectorWithMockCoverage();

    ITestHostProcessLifetimeHandler lifetimeHandler = collector;
    ITestHostEnvironmentVariableProvider envProvider = collector;

    await lifetimeHandler.BeforeTestHostProcessStartAsync(CancellationToken.None);

    var capturedVariables = new List<string>();
    var mockEnvVariables = new Mock<IEnvironmentVariables>();
    mockEnvVariables
      .Setup(x => x.SetVariable(It.IsAny<EnvironmentVariable>()))
      .Callback<EnvironmentVariable>(v => capturedVariables.Add(v.Variable));

    await envProvider.UpdateAsync(mockEnvVariables.Object);

    Assert.Equal(3, capturedVariables.Count);
    Assert.Equal(CoverletMtpEnvironmentVariables.CoverageEnabled, capturedVariables[0]);
    Assert.Equal(CoverletMtpEnvironmentVariables.CoverageIdentifier, capturedVariables[1]);
    Assert.Equal(CoverletMtpEnvironmentVariables.HitsFilePath, capturedVariables[2]);
  }

  #endregion

  #region ValidateTestHostEnvironmentVariablesAsync Tests

  [Fact]
  public async Task ValidateTestHostEnvironmentVariablesAsyncWhenNoConflictReturnsValid()
  {
    var collector = CreateCollector();
    ITestHostEnvironmentVariableProvider envProvider = collector;

    var mockReadOnlyEnvVars = new Mock<IReadOnlyEnvironmentVariables>();
    OwnedEnvironmentVariable? existingVar = null;
    mockReadOnlyEnvVars
      .Setup(x => x.TryGetVariable(CoverletMtpEnvironmentVariables.CoverageEnabled, out existingVar))
      .Returns(false);

    ValidationResult result = await envProvider.ValidateTestHostEnvironmentVariablesAsync(mockReadOnlyEnvVars.Object);

    Assert.True(result.IsValid);
  }

  [Fact]
  public async Task ValidateTestHostEnvironmentVariablesAsyncWhenConflictExistsReturnsInvalid()
  {
    var collector = CreateCollector();
    ITestHostEnvironmentVariableProvider envProvider = collector;

    var mockOtherExtension = new Mock<IExtension>();
    var conflictingVariable = new OwnedEnvironmentVariable(
      mockOtherExtension.Object,
      CoverletMtpEnvironmentVariables.CoverageEnabled,
      "true",
      isSecret: false,
      isLocked: true);

    var mockReadOnlyEnvVariables = new Mock<IReadOnlyEnvironmentVariables>();
    mockReadOnlyEnvVariables
      .Setup(x => x.TryGetVariable(CoverletMtpEnvironmentVariables.CoverageEnabled, out conflictingVariable))
      .Returns(true);

    ValidationResult result = await envProvider.ValidateTestHostEnvironmentVariablesAsync(mockReadOnlyEnvVariables.Object);

    Assert.False(result.IsValid);
    Assert.Contains(CoverletMtpEnvironmentVariables.CoverageEnabled, result.ErrorMessage);
  }

  [Fact]
  public async Task ValidateTestHostEnvironmentVariablesAsyncWhenVariableOwnedBySelfReturnsValid()
  {
    var collector = CreateCollector();
    ITestHostEnvironmentVariableProvider envProvider = collector;
    IExtension extension = collector;

    var ownedVariable = new OwnedEnvironmentVariable(
      extension,
      CoverletMtpEnvironmentVariables.CoverageEnabled,
      "true",
      isSecret: false,
      isLocked: true);

    var mockReadOnlyEnvVariables = new Mock<IReadOnlyEnvironmentVariables>();
    mockReadOnlyEnvVariables
      .Setup(x => x.TryGetVariable(CoverletMtpEnvironmentVariables.CoverageEnabled, out ownedVariable))
      .Returns(true);

    ValidationResult result = await envProvider.ValidateTestHostEnvironmentVariablesAsync(mockReadOnlyEnvVariables.Object);

    Assert.True(result.IsValid);
  }

  #endregion

  #region OnTestHostProcessStartedAsync Tests

  [Fact]
  public async Task OnTestHostProcessStartedAsyncLogsProcessStarted()
  {
    var collector = CreateCollector();
    ITestHostProcessLifetimeHandler lifetimeHandler = collector;

    var mockProcessInfo = new Mock<ITestHostProcessInformation>();
    mockProcessInfo.Setup(x => x.PID).Returns(12345);

    await lifetimeHandler.OnTestHostProcessStartedAsync(mockProcessInfo.Object, CancellationToken.None);

    mockProcessInfo.Verify(x => x.PID, Times.AtLeastOnce);
  }

  #endregion

  #region OnTestHostProcessExitedAsync Tests

  [Fact]
  public async Task OnTestHostProcessExitedAsyncWhenCoverageEnabledCollectsCoverageResult()
  {
    var (collector, mockCoverage) = CreateCollectorWithMockCoverage();

    mockCoverage.Setup(x => x.GetCoverageResult()).Returns(new CoverageResult
    {
      Identifier = "test-id",
      Modules = [],
      Parameters = new CoverageParameters()
    });

    ITestHostProcessLifetimeHandler lifetimeHandler = collector;
    await lifetimeHandler.BeforeTestHostProcessStartAsync(CancellationToken.None);

    var mockProcessInfo = new Mock<ITestHostProcessInformation>();
    mockProcessInfo.Setup(x => x.PID).Returns(12345);
    mockProcessInfo.Setup(x => x.ExitCode).Returns(0);

    await lifetimeHandler.OnTestHostProcessExitedAsync(mockProcessInfo.Object, CancellationToken.None);

    mockCoverage.Verify(x => x.GetCoverageResult(), Times.Once);
  }

  [Fact]
  public async Task OnTestHostProcessExitedAsyncWhenExceptionThrownDoesNotRethrow()
  {
    var (collector, mockCoverage) = CreateCollectorWithMockCoverage();

    mockCoverage.Setup(x => x.GetCoverageResult())
      .Throws(new InvalidOperationException("Test exception"));

    ITestHostProcessLifetimeHandler lifetimeHandler = collector;
    await lifetimeHandler.BeforeTestHostProcessStartAsync(CancellationToken.None);

    var mockProcessInfo = new Mock<ITestHostProcessInformation>();
    mockProcessInfo.Setup(x => x.PID).Returns(12345);
    mockProcessInfo.Setup(x => x.ExitCode).Returns(0);

    // Should not throw
    await lifetimeHandler.OnTestHostProcessExitedAsync(mockProcessInfo.Object, CancellationToken.None);
  }

  #endregion
}

