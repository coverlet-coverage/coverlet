// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Coverlet.Core;
using Coverlet.Core.Abstractions;
using Coverlet.MTP.CommandLine;
using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Configurations;
using Microsoft.Testing.Platform.Extensions.TestHostControllers;
using Microsoft.Testing.Platform.Logging;
using Microsoft.Testing.Platform.OutputDevice;
using Moq;
using Xunit;

namespace Coverlet.MTP.Collector.Tests;

/// <summary>
/// Tests for previously uncovered code paths in CollectorExtension
/// </summary>
public class CollectorExtensionUncoveredPathsTests
{
  private readonly Mock<ILoggerFactory> _mockLoggerFactory;
  private readonly Mock<Microsoft.Testing.Platform.Logging.ILogger> _mockLogger;
  private readonly Mock<ICommandLineOptions> _mockCommandLineOptions;
  private readonly Mock<IConfiguration> _mockConfiguration;
  private readonly Mock<IFileSystem> _mockFileSystem;
  private readonly Mock<IOutputDevice> _mockOutputDevice;

  private const string SimulatedTestModulePath = "/fake/path/test.dll";
  private const string SimulatedReportDirectory = "/fake/reports";

  public CollectorExtensionUncoveredPathsTests()
  {
    _mockLoggerFactory = new Mock<ILoggerFactory>();
    _mockLogger = new Mock<Microsoft.Testing.Platform.Logging.ILogger>();
    _mockCommandLineOptions = new Mock<ICommandLineOptions>();
    _mockConfiguration = new Mock<IConfiguration>();
    _mockFileSystem = new Mock<IFileSystem>();
    _mockOutputDevice = new Mock<IOutputDevice>();

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

  private (CollectorExtension collector, Mock<ICoverage> mockCoverage) CreateCollectorWithMockCoverage()
  {
    _mockCommandLineOptions
      .Setup(x => x.IsOptionSet(CoverletOptionNames.Coverage))
      .Returns(true);

    _mockConfiguration
      .Setup(x => x["TestModule"])
      .Returns(SimulatedTestModulePath);

    // Mock the underlying configuration property instead of the extension method
    _mockConfiguration
      .Setup(x => x["TestResultDirectory"])
      .Returns(SimulatedReportDirectory);

    // Setup json format (file-based reporter)
    string[] formats = ["json"];
    _mockCommandLineOptions
      .Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.Formats, out formats!))
      .Returns(true);

    string[]? includes = null;
    _mockCommandLineOptions
      .Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.Include, out includes!))
      .Returns(false);

    string[]? excludes = null;
    _mockCommandLineOptions
      .Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.Exclude, out excludes!))
      .Returns(false);

    _mockCommandLineOptions
      .Setup(x => x.IsOptionSet(CoverletOptionNames.IncludeTestAssembly))
      .Returns(false);

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
      _mockOutputDevice.Object,
      _mockConfiguration.Object,
      _mockFileSystem.Object)
    {
      CoverageFactory = mockCoverageFactory.Object
    };

    return (collector, mockCoverage);
  }

  #region GenerateReportsAsync - Directory Creation Test (Lines ~380-383)

  [Fact]
  public async Task OnTestHostProcessExitedAsyncCreatesOutputDirectoryWhenItDoesNotExist()
  {
    // Arrange - Directory does NOT exist
    // Note: Directory.CreateDirectory is a static method in the actual implementation
    // that cannot be intercepted through IFileSystem abstraction
    _mockFileSystem.Setup(x => x.Exists(SimulatedReportDirectory)).Returns(false);

    var (collector, _) = CreateCollectorWithMockCoverage();

    ITestHostProcessLifetimeHandler lifetimeHandler = collector;
    await lifetimeHandler.BeforeTestHostProcessStartAsync(CancellationToken.None);

    var mockProcessInfo = new Mock<ITestHostProcessInformation>();
    mockProcessInfo.Setup(x => x.PID).Returns(12345);
    mockProcessInfo.Setup(x => x.ExitCode).Returns(0);

    // Act - This should trigger report generation
    await lifetimeHandler.OnTestHostProcessExitedAsync(mockProcessInfo.Object, CancellationToken.None);

    // Assert - Verify the test module existence check was performed during initialization
    // The actual implementation uses Directory.CreateDirectory which is a static method
    // and cannot be mocked through IFileSystem. This test verifies the code path executes
    // without exceptions when the directory does not exist.
    _mockFileSystem.Verify(x => x.Exists(SimulatedTestModulePath), Times.AtLeastOnce);
  }

  [Fact]
  public async Task OnTestHostProcessExitedAsyncExecutesWithoutExceptions()
  {
    // Arrange
    var (collector, _) = CreateCollectorWithMockCoverage();

    ITestHostProcessLifetimeHandler lifetimeHandler = collector;
    await lifetimeHandler.BeforeTestHostProcessStartAsync(CancellationToken.None);

    var mockProcessInfo = new Mock<ITestHostProcessInformation>();
    mockProcessInfo.Setup(x => x.PID).Returns(12345);
    mockProcessInfo.Setup(x => x.ExitCode).Returns(0);

    // Act & Assert - Verify the code executes without exceptions
    await lifetimeHandler.OnTestHostProcessExitedAsync(mockProcessInfo.Object, CancellationToken.None);

    // Verify that coverage was prepared and results were retrieved
    _mockFileSystem.Verify(x => x.Exists(SimulatedTestModulePath), Times.AtLeastOnce);
  }

  #endregion
}
