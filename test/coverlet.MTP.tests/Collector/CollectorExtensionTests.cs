// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable IDE0005 // Using directive is unnecessary.
using System.Runtime.InteropServices;
#pragma warning restore IDE0005 // Using directive is unnecessary.
using Coverlet.Core;
using Coverlet.Core.Abstractions;
using Coverlet.MTP.CommandLine;
using Coverlet.MTP.EnvironmentVariables;
using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Configurations;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.TestHostControllers;
using Microsoft.Testing.Platform.Logging;
using Moq;
using Xunit;

namespace Coverlet.MTP.Collector.Tests;

[Collection("Coverlet Extension Collector Tests")]
public class CollectorExtensionTests
{
  private readonly Mock<ILoggerFactory> _mockLoggerFactory;
  private readonly Mock<Microsoft.Testing.Platform.Logging.ILogger> _mockLogger;
  private readonly Mock<ICommandLineOptions> _mockCommandLineOptions;
  private readonly Mock<IConfiguration> _mockConfiguration;

  public CollectorExtensionTests()
  {
    _mockLoggerFactory = new Mock<ILoggerFactory>();
    _mockLogger = new Mock<Microsoft.Testing.Platform.Logging.ILogger>();
    _mockCommandLineOptions = new Mock<ICommandLineOptions>();
    _mockConfiguration = new Mock<IConfiguration>();

    _mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>()))
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

#if Windows
  [Fact]
  public void Constructor_ThrowsArgumentNullException_WhenLoggerFactoryIsNull()
  {
    Assert.Throws<ArgumentNullException>(() =>
      new CollectorExtension(null!, _mockCommandLineOptions.Object, _mockConfiguration.Object));
  }

  [Fact]
  public void Constructor_ThrowsArgumentNullException_WhenCommandLineOptionsIsNull()
  {
    Assert.Throws<ArgumentNullException>(() =>
      new CollectorExtension(_mockLoggerFactory.Object, null!, _mockConfiguration.Object));
  }

  [Fact]
  public void Constructor_ThrowsArgumentNullException_WhenConfigurationIsNull()
  {
    Assert.Throws<ArgumentNullException>(() =>
      new CollectorExtension(_mockLoggerFactory.Object, _mockCommandLineOptions.Object, null!));
  }
#endif
  [Fact]
  public void Constructor_InitializesSuccessfully_WithValidParameters()
  {
    var collector = new CollectorExtension(
      _mockLoggerFactory.Object,
      _mockCommandLineOptions.Object,
      _mockConfiguration.Object);

    Assert.NotNull(collector);
  }

  [Fact]
  public void Uid_ReturnsExpectedValue()
  {
    var collector = CreateCollector();
    var extension = collector as Microsoft.Testing.Platform.Extensions.IExtension;

    Assert.NotNull(extension.Uid);
    Assert.NotEmpty(extension.Uid);
  }

  [Fact]
  public void Version_ReturnsExpectedValue()
  {
    var collector = CreateCollector();
    var extension = collector as Microsoft.Testing.Platform.Extensions.IExtension;

    Assert.NotNull(extension.Version);
    Assert.NotEmpty(extension.Version);
  }

  [Fact]
  public void DisplayName_ReturnsExpectedValue()
  {
    var collector = CreateCollector();
    var extension = collector as Microsoft.Testing.Platform.Extensions.IExtension;

    Assert.NotNull(extension.DisplayName);
    Assert.NotEmpty(extension.DisplayName);
  }

  [Fact]
  public void Description_ReturnsExpectedValue()
  {
    var collector = CreateCollector();
    var extension = collector as Microsoft.Testing.Platform.Extensions.IExtension;

    Assert.NotNull(extension.Description);
    Assert.NotEmpty(extension.Description);
  }

  [Fact]
  public async Task BeforeTestHostProcessStartAsync_WhenCoverageNotEnabled_ReturnsCompletedTask()
  {
    _mockCommandLineOptions.Setup(x => x.IsOptionSet(CoverletOptionNames.Coverage)).Returns(false);

    var collector = CreateCollector();
    var lifetimeHandler = collector as ITestHostProcessLifetimeHandler;

    await lifetimeHandler.BeforeTestHostProcessStartAsync(CancellationToken.None);

    _mockCommandLineOptions.Verify(x => x.IsOptionSet(CoverletOptionNames.Coverage), Times.Once);
  }

  [Fact]
  public async Task BeforeTestHostProcessStartAsync_WhenCoverageEnabled_AndNoTestModule_DisablesCoverage()
  {
    _mockCommandLineOptions.Setup(x => x.IsOptionSet(CoverletOptionNames.Coverage)).Returns(true);
    _mockConfiguration.Setup(x => x[It.IsAny<string>()]).Returns((string)null!);

    var collector = CreateCollector();
    var lifetimeHandler = collector as ITestHostProcessLifetimeHandler;

    await lifetimeHandler.BeforeTestHostProcessStartAsync(CancellationToken.None);

    // Coverage should be disabled due to missing test module path
    _mockCommandLineOptions.Verify(x => x.IsOptionSet(CoverletOptionNames.Coverage), Times.Once);
  }

  [Fact]
  public async Task BeforeTestHostProcessStartAsync_WhenCoverageEnabled_ParsesCommandLineOptions()
  {
    string testModulePath = CreateTempTestModule();
    try
    {
      _mockCommandLineOptions.Setup(x => x.IsOptionSet(CoverletOptionNames.Coverage)).Returns(true);
      _mockConfiguration.Setup(x => x["TestModule"]).Returns(testModulePath);

      string[]? formats = null;
      _mockCommandLineOptions
        .Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.Formats, out formats!))
        .Returns(false);

      string[]? includes = null;
      _mockCommandLineOptions
        .Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.Include, out includes!))
        .Returns(false);

      string[]? excludes = null;
      _mockCommandLineOptions
        .Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.Exclude, out excludes!))
        .Returns(false);

      _mockCommandLineOptions.Setup(x => x.IsOptionSet(CoverletOptionNames.IncludeTestAssembly)).Returns(false);

      var collector = CreateCollector();
      var lifetimeHandler = collector as ITestHostProcessLifetimeHandler;

      await lifetimeHandler.BeforeTestHostProcessStartAsync(CancellationToken.None);

      _mockCommandLineOptions.Verify(x => x.TryGetOptionArgumentList(CoverletOptionNames.Formats, out formats!), Times.Once);
    }
    finally
    {
      CleanupTempTestModule(testModulePath);
    }
  }

#if Windows

  [Fact]
  public async Task BeforeTestHostProcessStartAsync_ParsesFormatsOption()
  {
    string testModulePath = CreateTempTestModule();
    try
    {
      _mockCommandLineOptions.Setup(x => x.IsOptionSet(CoverletOptionNames.Coverage)).Returns(true);
      _mockConfiguration.Setup(x => x["TestModule"]).Returns(testModulePath);

      string[]? formats = ["json", "cobertura"];
      _mockCommandLineOptions
        .Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.Formats, out formats))
        .Returns(true);

      string[]? includes = null!;
      _mockCommandLineOptions
        .Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.Include, out includes))
        .Returns(false);

      string[]? excludes = null!;
      _mockCommandLineOptions
        .Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.Exclude, out excludes))
        .Returns(false);

      _mockCommandLineOptions.Setup(x => x.IsOptionSet(CoverletOptionNames.IncludeTestAssembly)).Returns(false);

      var collector = CreateCollector();
      var lifetimeHandler = collector as ITestHostProcessLifetimeHandler;

      await lifetimeHandler.BeforeTestHostProcessStartAsync(CancellationToken.None);

      _mockCommandLineOptions.Verify(x => x.TryGetOptionArgumentList(CoverletOptionNames.Formats, out formats), Times.Once);
    }
    finally
    {
      CleanupTempTestModule(testModulePath);
    }
  }
#endif

#if Windows
  [Fact]
  public async Task BeforeTestHostProcessStartAsync_WithMockCoverageFactory_InstrumentsModules()
  {
    string testModulePath = CreateTempTestModule();
    try
    {
      _mockCommandLineOptions.Setup(x => x.IsOptionSet(CoverletOptionNames.Coverage)).Returns(true);
      _mockConfiguration.Setup(x => x["TestModule"]).Returns(testModulePath);

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

      var mockCoverage = new Mock<ICoverage>();
      mockCoverage.Setup(x => x.PrepareModules()).Returns(new CoveragePrepareResult
      {
        Identifier = Guid.NewGuid().ToString(),
        Results = []
      });

      var mockCoverageFactory = new Mock<ICoverageFactory>();
      mockCoverageFactory
        .Setup(x => x.Create(It.IsAny<string>(), It.IsAny<CoverageParameters>()))
        .Returns(mockCoverage.Object);

      var collector = CreateCollector();
      collector.CoverageFactory = mockCoverageFactory.Object;

      var lifetimeHandler = collector as ITestHostProcessLifetimeHandler;

      await lifetimeHandler.BeforeTestHostProcessStartAsync(CancellationToken.None);

      mockCoverageFactory.Verify(x => x.Create(It.IsAny<string>(), It.IsAny<CoverageParameters>()), Times.Once);
      mockCoverage.Verify(x => x.PrepareModules(), Times.Once);
    }
    finally
    {
      CleanupTempTestModule(testModulePath);
    }
  }

  [Fact]
  public async Task UpdateAsync_WhenCoverageEnabled_SetsEnvironmentVariables()
  {
    string testModulePath = CreateTempTestModule();
    try
    {
      _mockCommandLineOptions.Setup(x => x.IsOptionSet(CoverletOptionNames.Coverage)).Returns(true);
      _mockConfiguration.Setup(x => x["TestModule"]).Returns(testModulePath);

      SetupDefaultCommandLineOptions();

      var mockCoverage = new Mock<ICoverage>();
      string coverageIdentifier = Guid.NewGuid().ToString();
      mockCoverage.Setup(x => x.PrepareModules()).Returns(new CoveragePrepareResult
      {
        Identifier = coverageIdentifier,
        Results = []
      });

      var mockCoverageFactory = new Mock<ICoverageFactory>();
      mockCoverageFactory
        .Setup(x => x.Create(It.IsAny<string>(), It.IsAny<CoverageParameters>()))
        .Returns(mockCoverage.Object);

      var collector = CreateCollector();
      collector.CoverageFactory = mockCoverageFactory.Object;

      var lifetimeHandler = collector as ITestHostProcessLifetimeHandler;
      await lifetimeHandler.BeforeTestHostProcessStartAsync(CancellationToken.None);

      var capturedVariables = new List<EnvironmentVariable>();
      var mockEnvironmentVariables = new Mock<IEnvironmentVariables>();
      mockEnvironmentVariables
        .Setup(x => x.SetVariable(It.IsAny<EnvironmentVariable>()))
        .Callback<EnvironmentVariable>(capturedVariables.Add);

      var envProvider = collector as ITestHostEnvironmentVariableProvider;

      await envProvider.UpdateAsync(mockEnvironmentVariables.Object);

      Assert.Contains(capturedVariables, v => v.Variable == CoverletMtpEnvironmentVariables.CoverageEnabled);
      Assert.Contains(capturedVariables, v => v.Variable == CoverletMtpEnvironmentVariables.CoverageIdentifier);
    }
    finally
    {
      CleanupTempTestModule(testModulePath);
    }
  }
#endif

  [Fact]
  public async Task ValidateTestHostEnvironmentVariablesAsync_ReturnsValid_WhenNoConflict()
  {
    var collector = CreateCollector();
    var envProvider = collector as ITestHostEnvironmentVariableProvider;

    var mockReadOnlyEnvVars = new Mock<IReadOnlyEnvironmentVariables>();
    OwnedEnvironmentVariable? existingVar = null;
    mockReadOnlyEnvVars
      .Setup(x => x.TryGetVariable(CoverletMtpEnvironmentVariables.CoverageEnabled, out existingVar))
      .Returns(false);

    ValidationResult result = await envProvider.ValidateTestHostEnvironmentVariablesAsync(mockReadOnlyEnvVars.Object);

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
      CoverletMtpEnvironmentVariables.CoverageEnabled,
      "true",
      isSecret: false,
      isLocked: true);

    var mockReadOnlyEnvVariables = new Mock<IReadOnlyEnvironmentVariables>();
    mockReadOnlyEnvVariables
      .Setup(x => x.TryGetVariable(CoverletMtpEnvironmentVariables.CoverageEnabled, out conflictingVariable))
      .Returns(true);

    // Act
    ValidationResult result = await envProvider.ValidateTestHostEnvironmentVariablesAsync(mockReadOnlyEnvVariables.Object);

    // Assert
    Assert.False(result.IsValid);
    Assert.Contains(CoverletMtpEnvironmentVariables.CoverageEnabled, result.ErrorMessage);
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
      CoverletMtpEnvironmentVariables.CoverageEnabled,
      "true",
      isSecret: false,
      isLocked: true);

    var mockReadOnlyEnvVariables = new Mock<IReadOnlyEnvironmentVariables>();
    mockReadOnlyEnvVariables
      .Setup(x => x.TryGetVariable(CoverletMtpEnvironmentVariables.CoverageEnabled, out ownedVariable))
      .Returns(true);

    // Act
    ValidationResult result = await envProvider.ValidateTestHostEnvironmentVariablesAsync(mockReadOnlyEnvVariables.Object);

    // Assert
    Assert.True(result.IsValid);
  }

  [Fact]
  public async Task OnTestHostProcessStartedAsync_LogsProcessStarted()
  {
    var collector = CreateCollector();
    var lifetimeHandler = collector as ITestHostProcessLifetimeHandler;

    var mockProcessInfo = new Mock<ITestHostProcessInformation>();
    mockProcessInfo.Setup(x => x.PID).Returns(12345);

    await lifetimeHandler.OnTestHostProcessStartedAsync(mockProcessInfo.Object, CancellationToken.None);

    mockProcessInfo.Verify(x => x.PID, Times.AtLeastOnce);
  }

#if Windows
  [Fact]
  public async Task OnTestHostProcessExitedAsync_WhenCoverageEnabled_CollectsCoverageResult()
  {
    string testModulePath = CreateTempTestModule();
    try
    {
      _mockCommandLineOptions.Setup(x => x.IsOptionSet(CoverletOptionNames.Coverage)).Returns(true);
      _mockConfiguration.Setup(x => x["TestModule"]).Returns(testModulePath);
      _mockConfiguration.Setup(x => x["TestResultDirectory"]).Returns(Path.GetDirectoryName(testModulePath));

      SetupDefaultCommandLineOptions();

      var mockCoverage = new Mock<ICoverage>();
      string coverageIdentifier = Guid.NewGuid().ToString();
      mockCoverage.Setup(x => x.PrepareModules()).Returns(new CoveragePrepareResult
      {
        Identifier = coverageIdentifier,
        Results = []
      });
      mockCoverage.Setup(x => x.GetCoverageResult()).Returns(new CoverageResult
      {
        Identifier = coverageIdentifier,
        Modules = [],
        Parameters = new CoverageParameters()
      });

      var mockCoverageFactory = new Mock<ICoverageFactory>();
      mockCoverageFactory
        .Setup(x => x.Create(It.IsAny<string>(), It.IsAny<CoverageParameters>()))
        .Returns(mockCoverage.Object);

      var collector = CreateCollector();
      collector.CoverageFactory = mockCoverageFactory.Object;

      var lifetimeHandler = collector as ITestHostProcessLifetimeHandler;
      await lifetimeHandler.BeforeTestHostProcessStartAsync(CancellationToken.None);

      var mockProcessInfo = new Mock<ITestHostProcessInformation>();
      mockProcessInfo.Setup(x => x.PID).Returns(12345);
      mockProcessInfo.Setup(x => x.ExitCode).Returns(0);

      await lifetimeHandler.OnTestHostProcessExitedAsync(mockProcessInfo.Object, CancellationToken.None);

      mockCoverage.Verify(x => x.GetCoverageResult(), Times.Once);
    }
    finally
    {
      CleanupTempTestModule(testModulePath);
    }
  }
#endif

#if Windows
  [Fact]
  public async Task BeforeTestHostProcessStartAsync_HandlesException_DisablesCoverage()
  {
    _mockCommandLineOptions.Setup(x => x.IsOptionSet(CoverletOptionNames.Coverage)).Returns(true);
    _mockConfiguration.Setup(x => x["TestModule"]).Returns("nonexistent.dll");

    var mockCoverageFactory = new Mock<ICoverageFactory>();
    mockCoverageFactory
      .Setup(x => x.Create(It.IsAny<string>(), It.IsAny<CoverageParameters>()))
      .Throws(new InvalidOperationException("Test exception"));

    var collector = CreateCollector();
    collector.CoverageFactory = mockCoverageFactory.Object;

    var lifetimeHandler = collector as ITestHostProcessLifetimeHandler;

    // Should not throw, just disable coverage
    await lifetimeHandler.BeforeTestHostProcessStartAsync(CancellationToken.None);

    // Verify coverage is disabled by checking env vars are not set
    var mockEnvironmentVariables = new Mock<IEnvironmentVariables>();
    var envProvider = collector as ITestHostEnvironmentVariableProvider;
    await envProvider.UpdateAsync(mockEnvironmentVariables.Object);

    mockEnvironmentVariables.Verify(x => x.SetVariable(It.IsAny<EnvironmentVariable>()), Times.Never);
  }
#endif
#if Windows
  [Fact]
  public async Task ResolveTestModulePath_UsesTestHostPathFromConfiguration()
  {
    Assert.SkipUnless(RuntimeInformation.IsOSPlatform(OSPlatform.Windows), "Test requires Windows");
    string testModulePath = CreateTempTestModule();
    try
    {
      _mockCommandLineOptions.Setup(x => x.IsOptionSet(CoverletOptionNames.Coverage)).Returns(true);
      _mockConfiguration.Setup(x => x["TestModule"]).Returns((string)null!);
      _mockConfiguration.Setup(x => x["TestHost:Path"]).Returns(testModulePath);

      SetupDefaultCommandLineOptions();

      var mockCoverage = new Mock<ICoverage>();
      mockCoverage.Setup(x => x.PrepareModules()).Returns(new CoveragePrepareResult
      {
        Identifier = Guid.NewGuid().ToString(),
        Results = []
      });

      var mockCoverageFactory = new Mock<ICoverageFactory>();
      mockCoverageFactory
        .Setup(x => x.Create(testModulePath, It.IsAny<CoverageParameters>()))
        .Returns(mockCoverage.Object);

      var collector = CreateCollector();
      collector.CoverageFactory = mockCoverageFactory.Object;

      var lifetimeHandler = collector as ITestHostProcessLifetimeHandler;
      await lifetimeHandler.BeforeTestHostProcessStartAsync(CancellationToken.None);

      mockCoverageFactory.Verify(x => x.Create(testModulePath, It.IsAny<CoverageParameters>()), Times.Once);
    }
    finally
    {
      CleanupTempTestModule(testModulePath);
    }
  }
#endif

#if Windows
  [Fact]
  public async Task OnTestHostProcessExitedAsync_HandlesException_LogsError()
  {
    string testModulePath = CreateTempTestModule();
    try
    {
      _mockCommandLineOptions.Setup(x => x.IsOptionSet(CoverletOptionNames.Coverage)).Returns(true);
      _mockConfiguration.Setup(x => x["TestModule"]).Returns(testModulePath);
      _mockConfiguration.Setup(x => x["TestResultDirectory"]).Returns(Path.GetDirectoryName(testModulePath));

      SetupDefaultCommandLineOptions();

      var mockCoverage = new Mock<ICoverage>();
      mockCoverage.Setup(x => x.PrepareModules()).Returns(new CoveragePrepareResult
      {
        Identifier = Guid.NewGuid().ToString(),
        Results = []
      });
      mockCoverage.Setup(x => x.GetCoverageResult()).Throws(new InvalidOperationException("Test exception"));

      var mockCoverageFactory = new Mock<ICoverageFactory>();
      mockCoverageFactory
        .Setup(x => x.Create(It.IsAny<string>(), It.IsAny<CoverageParameters>()))
        .Returns(mockCoverage.Object);

      var collector = CreateCollector();
      collector.CoverageFactory = mockCoverageFactory.Object;

      var lifetimeHandler = collector as ITestHostProcessLifetimeHandler;
      await lifetimeHandler.BeforeTestHostProcessStartAsync(CancellationToken.None);

      var mockProcessInfo = new Mock<ITestHostProcessInformation>();
      mockProcessInfo.Setup(x => x.PID).Returns(12345);
      mockProcessInfo.Setup(x => x.ExitCode).Returns(0);

      // Should not throw, just log error
      await lifetimeHandler.OnTestHostProcessExitedAsync(mockProcessInfo.Object, CancellationToken.None);
    }
    finally
    {
      CleanupTempTestModule(testModulePath);
    }
  }
#endif

  #region Constructor Tests

  [Fact]
  public void Constructor_WithNullLoggerFactory_ThrowsArgumentNullException()
  {
    // Act & Assert
    ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
      new CollectorExtension(null!, _mockCommandLineOptions.Object, _mockConfiguration.Object));

    Assert.Equal("loggerFactory", exception.ParamName);
  }

  [Fact]
  public void Constructor_WithNullCommandLineOptions_ThrowsArgumentNullException()
  {
    // Act & Assert
    ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
      new CollectorExtension(_mockLoggerFactory.Object, null!, _mockConfiguration.Object));

    Assert.Equal("commandLineOptions", exception.ParamName);
  }

#if Windows
  [Fact]
  public void Constructor_WithNullConfiguration_ThrowsArgumentNullException()
  {
    // Act & Assert
    Assert.Throws<ArgumentNullException>(() =>
      new CollectorExtension(_mockLoggerFactory.Object, _mockCommandLineOptions.Object, null!));
  }
#endif

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

#if Windows
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
#endif

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

#if Windows
  [Fact]
  public async Task BeforeTestHostProcessStartAsync_WhenExceptionThrown_DisablesCoverage()
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
#endif

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

    string[]? includes = null!;
    _mockCommandLineOptions
      .Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.Include, out includes))
      .Returns(false);

    string[]? excludes = null!;
    _mockCommandLineOptions
      .Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.Exclude, out excludes))
      .Returns(false);

    _mockCommandLineOptions.Setup(x => x.IsOptionSet(CoverletOptionNames.IncludeTestAssembly)).Returns(false);

    var collector = CreateCollector();
    ITestHostProcessLifetimeHandler handler = collector;

    // Act
    await handler.BeforeTestHostProcessStartAsync(CancellationToken.None);

    // Assert
    _mockCommandLineOptions.Verify(
      x => x.TryGetOptionArgumentList(CoverletOptionNames.Formats, out formats),
      Times.Once);
  }

#if Windows
  [Fact]
  public async Task BeforeTestHostProcessStartAsync_ParsesMultipleFormatOptions_MtpConvention()
  {
    // NOTE: This tests the RECOMMENDED Microsoft Testing Platform approach.
    // Users should specify formats using multiple --coverlet-formats arguments:
    //   dotnet test --coverage --coverlet-formats json --coverlet-formats cobertura

    string testModulePath = CreateTempTestModule();
    try
    {
      _mockCommandLineOptions.Setup(x => x.IsOptionSet(CoverletOptionNames.Coverage)).Returns(true);
      _mockConfiguration.Setup(x => x["TestModule"]).Returns(testModulePath);

      // MTP-style: Multiple format arguments parsed as separate array elements
      string[]? formats = ["json", "cobertura", "lcov"];
      _mockCommandLineOptions
        .Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.Formats, out formats))
        .Returns(true);

      string[]? includes = null!;
      _mockCommandLineOptions
        .Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.Include, out includes))
        .Returns(false);

      string[]? excludes = null!;
      _mockCommandLineOptions
        .Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.Exclude, out excludes))
        .Returns(false);

      _mockCommandLineOptions.Setup(x => x.IsOptionSet(CoverletOptionNames.IncludeTestAssembly)).Returns(false);

      var mockCoverage = new Mock<ICoverage>();
      mockCoverage.Setup(x => x.PrepareModules()).Returns(new CoveragePrepareResult
      {
        Identifier = Guid.NewGuid().ToString(),
        Results = []
      });

      var mockCoverageFactory = new Mock<ICoverageFactory>();
      mockCoverageFactory
        .Setup(x => x.Create(It.IsAny<string>(), It.IsAny<CoverageParameters>()))
        .Returns(mockCoverage.Object);

      var collector = CreateCollector();
      collector.CoverageFactory = mockCoverageFactory.Object;

      var lifetimeHandler = collector as ITestHostProcessLifetimeHandler;
      await lifetimeHandler.BeforeTestHostProcessStartAsync(CancellationToken.None);

      // Verify the formats were parsed correctly
      _mockCommandLineOptions.Verify(x => x.TryGetOptionArgumentList(CoverletOptionNames.Formats, out formats), Times.Once);
      mockCoverage.Verify(x => x.PrepareModules(), Times.Once);
    }
    finally
    {
      CleanupTempTestModule(testModulePath);
    }
  }
#endif

#if Windows

  [Fact]
  public async Task BeforeTestHostProcessStartAsync_ParsesMultipleIncludeFiltersCorrectly()
  {
    // Tests MTP convention: --coverlet-include [Assembly1]* --coverlet-include [Assembly2]*

    _mockCommandLineOptions.Setup(x => x.IsOptionSet(CoverletOptionNames.Coverage)).Returns(true);

    // Multiple include filters as separate array elements (MTP style)
    string[]? includes = ["[Assembly1]*", "[Assembly2]*", "[Assembly3]*"];
    _mockCommandLineOptions
      .Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.Include, out includes))
      .Returns(true);

    string[]? formats = null!;
    _mockCommandLineOptions
      .Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.Formats, out formats))
      .Returns(false);

    string[]? excludes = null!;
    _mockCommandLineOptions
      .Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.Exclude, out excludes))
      .Returns(false);

    _mockCommandLineOptions.Setup(x => x.IsOptionSet(CoverletOptionNames.IncludeTestAssembly)).Returns(false);

    var collector = CreateCollector();
    ITestHostProcessLifetimeHandler handler = collector;

    // Act
    await handler.BeforeTestHostProcessStartAsync(CancellationToken.None);

    // Assert
    _mockCommandLineOptions.Verify(
      x => x.TryGetOptionArgumentList(CoverletOptionNames.Include, out includes),
      Times.Once);
  }
#endif

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

#if Windows
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
#endif

#if Windows
  [Fact]
  public async Task BeforeTestHostProcessStartAsync_WithValidTestModule_InitializesCoverage()
  {
    // Arrange
    string testModulePath = CreateTempTestModule();
    try
    {
      _mockCommandLineOptions
        .Setup(x => x.IsOptionSet(CoverletOptionNames.Coverage))
        .Returns(true);

      _mockConfiguration
        .Setup(x => x["TestModule"])
        .Returns(testModulePath);

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
    finally
    {
      CleanupTempTestModule(testModulePath);
    }
  }
#endif

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

#if Windows
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
#endif
#if Windows
  [Fact]
  public async Task UpdateAsync_WhenCoverageEnabledAndIdentifierSet_SetsCoverageEnabledVariable()
  {
    // Arrange
    var (collector, _, testModulePath) = CreateCollectorWithMockCoverage();
    try
    {
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
    finally
    {
      CleanupTempTestModule(testModulePath);
    }
  }
#endif

#if Windows
  [Fact]
  public async Task UpdateAsync_WhenCoverageEnabledAndIdentifierSet_SetsCoverageIdentifierVariable()
  {
    // Arrange
    var (collector, _, testModulePath) = CreateCollectorWithMockCoverage("my-test-identifier");
    try
    {
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
    finally
    {
      CleanupTempTestModule(testModulePath);
    }
  }
#endif

#if Windows
  [Fact]
  public async Task UpdateAsync_WhenCoverageEnabledAndIdentifierSet_SetsHitsFilePathVariable()
  {
    // Arrange
    var (collector, _, testModulePath) = CreateCollectorWithMockCoverage();
    try
    {
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
    finally
    {
      CleanupTempTestModule(testModulePath);
    }
  }
#endif

  [Fact]
  public async Task UpdateAsync_WhenCoverageEnabledButIdentifierEmpty_DoesNotSetEnvironmentVariables()
  {
    // Arrange
    string testModulePath = CreateTempTestModule();
    try
    {
      _mockCommandLineOptions
        .Setup(x => x.IsOptionSet(CoverletOptionNames.Coverage))
        .Returns(true);

      _mockConfiguration
        .Setup(x => x["TestModule"])
        .Returns(testModulePath);

      var mockCoverage = new Mock<ICoverage>();
      mockCoverage
        .Setup(x => x.PrepareModules())
        .Returns(new CoveragePrepareResult { Identifier = string.Empty }); // Empty identifier

      var mockCoverageFactory = new Mock<ICoverageFactory>();
      mockCoverageFactory
        .Setup(x => x.Create(It.IsAny<string>(), It.IsAny<CoverageParameters>()))
        .Returns(mockCoverage.Object);

      var collector = CreateCollector();
      collector.CoverageFactory = mockCoverageFactory.Object;

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
    finally
    {
      CleanupTempTestModule(testModulePath);
    }
  }

  [Fact]
  public async Task UpdateAsync_WhenCoverageNull_DoesNotSetHitsFilePathVariable()
  {
    // Arrange
    _mockCommandLineOptions
      .Setup(x => x.IsOptionSet(CoverletOptionNames.Coverage))
      .Returns(true);

    // Don't set up test module path - this will result in _coverage being null
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

    // Assert - No variables should be set when coverage is null
    mockEnvVariables.Verify(
      x => x.SetVariable(It.IsAny<EnvironmentVariable>()),
      Times.Never);
  }

#if Windows
  [Fact]
  public async Task UpdateAsync_LogsSettingEnvironmentVariables()
  {
    // Arrange
    var (collector, _, testModulePath) = CreateCollectorWithMockCoverage();
    try
    {
      ITestHostProcessLifetimeHandler lifetimeHandler = collector;
      ITestHostEnvironmentVariableProvider envProvider = collector;

      await lifetimeHandler.BeforeTestHostProcessStartAsync(CancellationToken.None);

      var mockEnvVariables = new Mock<IEnvironmentVariables>();

      // Act
      await envProvider.UpdateAsync(mockEnvVariables.Object);

      // Assert - Verify logging occurred
      _mockLogger.Verify(
        x => x.Log(
          LogLevel.Trace,
          It.Is<string>(s => s.Contains("Setting environment variables")),
          It.IsAny<Exception?>(),
          It.IsAny<Func<string, Exception?, string>>()),
        Times.AtLeastOnce);
    }
    finally
    {
      CleanupTempTestModule(testModulePath);
    }
  }
#endif

#if Windows
  [Fact]
  public async Task UpdateAsync_CalledMultipleTimes_SetsSameVariablesEachTime()
  {
    // Arrange
    var (collector, _, testModulePath) = CreateCollectorWithMockCoverage("consistent-id");
    try
    {
      ITestHostProcessLifetimeHandler lifetimeHandler = collector;
      ITestHostEnvironmentVariableProvider envProvider = collector;

      await lifetimeHandler.BeforeTestHostProcessStartAsync(CancellationToken.None);

      var mockEnvVariables1 = new Mock<IEnvironmentVariables>();
      var mockEnvVariables2 = new Mock<IEnvironmentVariables>();

      // Act
      await envProvider.UpdateAsync(mockEnvVariables1.Object);
      await envProvider.UpdateAsync(mockEnvVariables2.Object);

      // Assert - Both calls should set the same variables
      mockEnvVariables1.Verify(
        x => x.SetVariable(It.Is<EnvironmentVariable>(
          ev => ev.Variable == CoverletMtpEnvironmentVariables.CoverageIdentifier && ev.Value == "consistent-id")),
        Times.Once);

      mockEnvVariables2.Verify(
        x => x.SetVariable(It.Is<EnvironmentVariable>(
          ev => ev.Variable == CoverletMtpEnvironmentVariables.CoverageIdentifier && ev.Value == "consistent-id")),
        Times.Once);
    }
    finally
    {
      CleanupTempTestModule(testModulePath);
    }
  }
#endif
#if Windows
  [Fact]
  public async Task UpdateAsync_WhenCoverageEnabledAndAllConditionsMet_CompletesSuccessfully()
  {
    // Arrange
    var (collector, _, testModulePath) = CreateCollectorWithMockCoverage();
    try
    {
      ITestHostProcessLifetimeHandler lifetimeHandler = collector;
      ITestHostEnvironmentVariableProvider envProvider = collector;

      await lifetimeHandler.BeforeTestHostProcessStartAsync(CancellationToken.None);

      var mockEnvVariables = new Mock<IEnvironmentVariables>();

      // Act
      Task updateTask = envProvider.UpdateAsync(mockEnvVariables.Object);

      // Assert
      await updateTask; // Should complete without exception
      Assert.True(updateTask.IsCompletedSuccessfully);
    }
    finally
    {
      CleanupTempTestModule(testModulePath);
    }
  }
#endif
#if Windows
  [Fact]
  public async Task UpdateAsync_SetsVariablesInCorrectOrder()
  {
    // Arrange
    var (collector, _, testModulePath) = CreateCollectorWithMockCoverage();
    try
    {
      ITestHostProcessLifetimeHandler lifetimeHandler = collector;
      ITestHostEnvironmentVariableProvider envProvider = collector;

      await lifetimeHandler.BeforeTestHostProcessStartAsync(CancellationToken.None);

      var capturedVariables = new List<string>();
      var mockEnvVariables = new Mock<IEnvironmentVariables>();
      mockEnvVariables
        .Setup(x => x.SetVariable(It.IsAny<EnvironmentVariable>()))
        .Callback<EnvironmentVariable>(v => capturedVariables.Add(v.Variable));

      // Act
      await envProvider.UpdateAsync(mockEnvVariables.Object);

      // Assert - Verify order: CoverageEnabled, CoverageIdentifier, then HitsFilePath
      Assert.Equal(3, capturedVariables.Count);
      Assert.Equal(CoverletMtpEnvironmentVariables.CoverageEnabled, capturedVariables[0]);
      Assert.Equal(CoverletMtpEnvironmentVariables.CoverageIdentifier, capturedVariables[1]);
      Assert.Equal(CoverletMtpEnvironmentVariables.HitsFilePath, capturedVariables[2]);
    }
    finally
    {
      CleanupTempTestModule(testModulePath);
    }
  }
#endif
#if Windows
  [Fact]
  public async Task UpdateAsync_HitsFilePathContainsValidDirectoryPath()
  {
    // Arrange
    var (collector, _, testModulePath) = CreateCollectorWithMockCoverage();
    try
    {
      ITestHostProcessLifetimeHandler lifetimeHandler = collector;
      ITestHostEnvironmentVariableProvider envProvider = collector;

      await lifetimeHandler.BeforeTestHostProcessStartAsync(CancellationToken.None);

      string? capturedHitsPath = null;
      var mockEnvVariables = new Mock<IEnvironmentVariables>();
      mockEnvVariables
        .Setup(x => x.SetVariable(It.Is<EnvironmentVariable>(
          ev => ev.Variable == CoverletMtpEnvironmentVariables.HitsFilePath)))
        .Callback<EnvironmentVariable>(v => capturedHitsPath = v.Value);

      // Act
      await envProvider.UpdateAsync(mockEnvVariables.Object);

      // Assert
      Assert.NotNull(capturedHitsPath);
      Assert.False(string.IsNullOrWhiteSpace(capturedHitsPath));
      // The path should be the temp directory where test module was created
      Assert.Contains(Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar), capturedHitsPath);
    }
    finally
    {
      CleanupTempTestModule(testModulePath);
    }
  }
#endif

  #endregion
  #region helper methods

#pragma warning disable IDE0051 //  Private member 'name' is unused.
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
#pragma warning restore IDE0051 // Private member 'name' is unused.
  private static string CreateTempTestModule()
  {
    string tempPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.dll");
    File.WriteAllText(tempPath, "dummy content");
    return tempPath;
  }

  private static void CleanupTempTestModule(string path)
  {
    try
    {
      if (File.Exists(path))
      {
        File.Delete(path);
      }
    }
    catch
    {
      // Ignore cleanup failures during parallel test execution
    }
  }

  private CollectorExtension CreateCollector()
  {
    return new CollectorExtension(
      _mockLoggerFactory.Object,
      _mockCommandLineOptions.Object,
      _mockConfiguration.Object);
  }

#pragma warning disable IDE0051 //  Private member 'name' is unused.
  private (CollectorExtension collector, Mock<ICoverage> mockCoverage, string testModulePath) CreateCollectorWithMockCoverage(string identifier = "test-identifier-123")
  {
    string testModulePath = CreateTempTestModule();

    _mockCommandLineOptions
      .Setup(x => x.IsOptionSet(CoverletOptionNames.Coverage))
      .Returns(true);

    _mockConfiguration
      .Setup(x => x["TestModule"])
      .Returns(testModulePath);

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

    return (collector, mockCoverage, testModulePath);
  }
#pragma warning restore IDE0051 //  Private member 'name' is unused.
  #endregion
}

internal static class ConfigurationExtensions
{
  public static string? GetTestResultDirectory(this IConfiguration configuration)
  {
    return configuration["TestResultDirectory"];
  }
}

[CollectionDefinition("Coverlet Extension Collector Tests", DisableParallelization = true)]
public class CoverletExtensionCollectorTestsCollection
{
  // Marker class for collection definition
}
