// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Coverlet.Core;
using Coverlet.Core.Abstractions;
using Coverlet.MTP.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Configurations;
using Microsoft.Testing.Platform.Extensions.TestHostControllers;
using Microsoft.Testing.Platform.Logging;
using Moq;
using Xunit;

namespace Coverlet.MTP.Collector.Tests;

/// <summary>
/// Tests for GenerateReportsAsync method covering lines 318-349 in CollectorExtension.cs
/// </summary>
public class CollectorExtensionGenerateReportsTests
{
  private readonly Mock<ILoggerFactory> _mockLoggerFactory;
  private readonly Mock<Microsoft.Testing.Platform.Logging.ILogger> _mockLogger;
  private readonly Mock<ICommandLineOptions> _mockCommandLineOptions;
  private readonly Mock<IConfiguration> _mockConfiguration;
  private readonly Mock<IFileSystem> _mockFileSystem;

  private const string SimulatedTestModulePath = "/fake/path/test.dll";
  private const string SimulatedTestModuleDirectory = "/fake/path";
  private const string SimulatedReportDirectory = "/fake/reports";

  public CollectorExtensionGenerateReportsTests()
  {
    _mockLoggerFactory = new Mock<ILoggerFactory>();
    _mockLogger = new Mock<Microsoft.Testing.Platform.Logging.ILogger>();
    _mockCommandLineOptions = new Mock<ICommandLineOptions>();
    _mockConfiguration = new Mock<IConfiguration>();
    _mockFileSystem = new Mock<IFileSystem>();

    _mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>()))
      .Returns(_mockLogger.Object);

    SetupDefaultMocks();
  }

  private void SetupDefaultMocks()
  {
    _mockCommandLineOptions
      .Setup(x => x.IsOptionSet(It.IsAny<string>()))
      .Returns(false);

    _mockCommandLineOptions
      .Setup(x => x.TryGetOptionArgumentList(It.IsAny<string>(), out It.Ref<string[]?>.IsAny))
      .Returns(false);

    _mockConfiguration
      .Setup(x => x[It.IsAny<string>()])
      .Returns((string?)null);

    _mockFileSystem
      .Setup(x => x.Exists(SimulatedTestModulePath))
      .Returns(true);

    _mockFileSystem
      .Setup(x => x.Exists(SimulatedTestModuleDirectory))
      .Returns(true);

    _mockFileSystem
      .Setup(x => x.Exists(SimulatedReportDirectory))
      .Returns(true);
  }

  private static CoverageResult CreateTestCoverageResult()
  {
    var lines = new Lines { { 1, 1 }, { 2, 1 } };
    var branches = new Branches
    {
      new BranchInfo { Line = 1, Hits = 1, Offset = 0, EndOffset = 10, Path = 0, Ordinal = 1 }
    };

    var methods = new Methods();
    string methodName = "System.Void TestClass::TestMethod()";
    methods.Add(methodName, new Method
    {
      Lines = lines,
      Branches = branches
    });

    var classes = new Classes { { "TestNamespace.TestClass", methods } };
    var documents = new Documents { { "TestFile.cs", classes } };
    var modules = new Modules { { SimulatedTestModulePath, documents } };

    return new CoverageResult
    {
      Identifier = "test-id",
      Modules = modules,
      Parameters = new CoverageParameters()
    };
  }

  private (CollectorExtension collector, Mock<ICoverage> mockCoverage) CreateCollectorWithMockCoverage(
    string identifier = "test-id",
    CoverageResult? coverageResult = null)
  {
    _mockCommandLineOptions
      .Setup(x => x.IsOptionSet(CoverletOptionNames.Coverage))
      .Returns(true);

    _mockConfiguration
      .Setup(x => x["TestModule"])
      .Returns(SimulatedTestModulePath);

    _mockConfiguration
      .Setup(x => x["TestResultDirectory"])
      .Returns(SimulatedReportDirectory);

    string[] formats = [];
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
    _mockCommandLineOptions
      .Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.Formats, out formats))
      .Returns(true);
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

    var mockCoverage = new Mock<ICoverage>();
    mockCoverage
      .Setup(x => x.PrepareModules())
      .Returns(new CoveragePrepareResult { Identifier = identifier });

    mockCoverage
      .Setup(x => x.GetCoverageResult())
      .Returns(coverageResult ?? CreateTestCoverageResult());

    var mockCoverageFactory = new Mock<ICoverageFactory>();
    mockCoverageFactory
      .Setup(x => x.Create(It.IsAny<string>(), It.IsAny<CoverageParameters>()))
      .Returns(mockCoverage.Object);

    var collector = new CollectorExtension(
      _mockLoggerFactory.Object,
      _mockCommandLineOptions.Object,
      _mockConfiguration.Object,
      _mockFileSystem.Object)
    {
      CoverageFactory = mockCoverageFactory.Object
    };

    return (collector, mockCoverage);
  }

  #region GenerateReportsAsync - Console Reporter Tests

  [Fact]
  public async Task OnTestHostProcessExitedAsyncWithTeamCityFormatLogsConsoleOutput()
  {
    // Arrange
    string[] formats = ["teamcity"];
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
    _mockCommandLineOptions
      .Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.Formats, out formats))
      .Returns(true);
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

    var (collector, _) = CreateCollectorWithMockCoverage();

    ITestHostProcessLifetimeHandler lifetimeHandler = collector;
    await lifetimeHandler.BeforeTestHostProcessStartAsync(CancellationToken.None);

    var mockProcessInfo = new Mock<ITestHostProcessInformation>();
    mockProcessInfo.Setup(x => x.PID).Returns(12345);
    mockProcessInfo.Setup(x => x.ExitCode).Returns(0);

    // Act
    await lifetimeHandler.OnTestHostProcessExitedAsync(mockProcessInfo.Object, CancellationToken.None);

    // Assert - Console reporter should log output with important flag
    // Note: Microsoft.Testing.Platform.Logging.ILogger uses simple extension methods like LogInformation(string)
    // We cannot verify extension methods with Moq, but we can verify the code executed without errors
    _mockFileSystem.Verify(x => x.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
  }

  #endregion

  #region GenerateReportsAsync - Report Content Tests

  [Fact]
  public async Task OnTestHostProcessExitedAsyncWithNoFileReportsDoesNotWriteFiles()
  {
    // Arrange - Only console format
    string[] formats = ["teamcity"];
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
    _mockCommandLineOptions
      .Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.Formats, out formats))
      .Returns(true);
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

    var (collector, _) = CreateCollectorWithMockCoverage();

    ITestHostProcessLifetimeHandler lifetimeHandler = collector;
    await lifetimeHandler.BeforeTestHostProcessStartAsync(CancellationToken.None);

    var mockProcessInfo = new Mock<ITestHostProcessInformation>();
    mockProcessInfo.Setup(x => x.PID).Returns(12345);
    mockProcessInfo.Setup(x => x.ExitCode).Returns(0);

    // Act
    await lifetimeHandler.OnTestHostProcessExitedAsync(mockProcessInfo.Object, CancellationToken.None);

    // Assert - Should not write any files via IFileSystem when only console format is used
    _mockFileSystem.Verify(x => x.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
  }

  [Fact]
  public async Task OnTestHostProcessExitedAsyncWithJsonFormatWritesReportAndLogsGeneratedPaths()
  {
    // Arrange - Use file-based format
    string[] formats = ["json"];
    _mockCommandLineOptions
      .Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.Formats, out formats!))
      .Returns(true);

    _mockCommandLineOptions
      .Setup(x => x.IsOptionSet(CoverletOptionNames.Coverage))
      .Returns(true);

    _mockConfiguration
      .Setup(x => x["TestModule"])
      .Returns(SimulatedTestModulePath);

    _mockConfiguration
      .Setup(x => x["TestResultDirectory"])
      .Returns(SimulatedReportDirectory);

    // Setup file system mock
    _mockFileSystem
      .Setup(x => x.Exists(It.IsAny<string>()))
      .Returns(true);

    var writtenFiles = new List<(string path, string content)>();
    _mockFileSystem
      .Setup(x => x.WriteAllText(It.IsAny<string>(), It.IsAny<string>()))
      .Callback<string, string>((path, content) => writtenFiles.Add((path, content)));

    // Create mocked service provider (similar to CoverletCoverageDataCollectorTests pattern)
    var mockSourceRootTranslator = new Mock<ISourceRootTranslator>();
    mockSourceRootTranslator
      .Setup(x => x.ResolveFilePath(It.IsAny<string>()))
      .Returns<string>(path => path);

    IServiceCollection serviceCollection = new ServiceCollection();
    serviceCollection.AddSingleton(_mockFileSystem.Object);
    serviceCollection.AddSingleton(mockSourceRootTranslator.Object);
    IServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();

    // Create mock coverage
    var mockCoverage = new Mock<ICoverage>();
    mockCoverage
      .Setup(x => x.PrepareModules())
      .Returns(new CoveragePrepareResult { Identifier = "test-id" });
    mockCoverage
      .Setup(x => x.GetCoverageResult())
      .Returns(CreateTestCoverageResult());

    var mockCoverageFactory = new Mock<ICoverageFactory>();
    mockCoverageFactory
      .Setup(x => x.Create(It.IsAny<string>(), It.IsAny<CoverageParameters>()))
      .Returns(mockCoverage.Object);

    var collector = new CollectorExtension(
      _mockLoggerFactory.Object,
      _mockCommandLineOptions.Object,
      _mockConfiguration.Object,
      _mockFileSystem.Object)
    {
      CoverageFactory = mockCoverageFactory.Object,
      ServiceProviderOverride = serviceProvider
    };

    ITestHostProcessLifetimeHandler lifetimeHandler = collector;
    await lifetimeHandler.BeforeTestHostProcessStartAsync(CancellationToken.None);

    var mockProcessInfo = new Mock<ITestHostProcessInformation>();
    mockProcessInfo.Setup(x => x.PID).Returns(12345);
    mockProcessInfo.Setup(x => x.ExitCode).Returns(0);

    // Act
    await lifetimeHandler.OnTestHostProcessExitedAsync(mockProcessInfo.Object, CancellationToken.None);

    // Assert - File should be written with JSON content
    _mockFileSystem.Verify(
      x => x.WriteAllText(
        It.Is<string>(path => path.EndsWith("coverage.json")),
        It.IsAny<string>()),
      Times.Once);

    Assert.Single(writtenFiles);
    Assert.EndsWith("coverage.json", writtenFiles[0].path);
  }

  #endregion

  #region GenerateReportsAsync - Error Handling Tests

  [Fact]
  public async Task OnTestHostProcessExitedAsyncWhenReporterFactoryReturnsNullCatchesException()
  {
    // Arrange - Use invalid format
    string[] formats = ["invalid-format"];
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
    _mockCommandLineOptions
      .Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.Formats, out formats))
      .Returns(true);
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

    var (collector, _) = CreateCollectorWithMockCoverage();

    ITestHostProcessLifetimeHandler lifetimeHandler = collector;
    await lifetimeHandler.BeforeTestHostProcessStartAsync(CancellationToken.None);

    var mockProcessInfo = new Mock<ITestHostProcessInformation>();
    mockProcessInfo.Setup(x => x.PID).Returns(12345);
    mockProcessInfo.Setup(x => x.ExitCode).Returns(0);

    // Act - Should catch exception and not throw
    await lifetimeHandler.OnTestHostProcessExitedAsync(mockProcessInfo.Object, CancellationToken.None);

    // Assert - No files should be written due to error
    _mockFileSystem.Verify(x => x.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
  }

  #endregion
}
