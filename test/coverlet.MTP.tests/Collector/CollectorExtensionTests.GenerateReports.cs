// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Coverlet.Core;
using Coverlet.Core.Abstractions;
using Coverlet.MTP.CommandLine;
using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Configurations;
using Microsoft.Testing.Platform.Extensions.OutputDevice;
using Microsoft.Testing.Platform.Extensions.TestHostControllers;
using Microsoft.Testing.Platform.Logging;
using Microsoft.Testing.Platform.OutputDevice;
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
  private readonly Mock<IOutputDevice> _mockOutputDevice;
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

    _mockOutputDevice.Setup(x => x.DisplayAsync(
        It.IsAny<IOutputDeviceDataProducer>(),
        It.IsAny<IOutputDeviceData>(),
        It.IsAny<CancellationToken>()))
      .Returns(Task.CompletedTask);

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
      _mockOutputDevice.Object,
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

  #region GenerateCoverageReportFiles Tests

  [Fact]
  public void GenerateCoverageReportFilesWithJsonFormatCreatesJsonReport()
  {
    // Arrange
    var mockFileSystem = new Mock<IFileSystem>();
    var mockSourceRootTranslator = new Mock<ISourceRootTranslator>();
    var mockReporterFactory = new Mock<IReporterFactory>();
    var mockReporter = new Mock<IReporter>();

    // Setup mocks
    mockReporter.Setup(x => x.OutputType).Returns(ReporterOutputType.File);
    mockReporter.Setup(x => x.Extension).Returns("json");
    mockReporter.Setup(x => x.Report(It.IsAny<CoverageResult>(), It.IsAny<ISourceRootTranslator>()))
      .Returns("{\"coverage\":\"data\"}");

    mockReporterFactory.Setup(x => x.CreateReporter("json")).Returns(mockReporter.Object);

    var collector = CreateCollectorForTesting(fileSystem: mockFileSystem.Object);
    collector.ReporterFactoryOverride = mockReporterFactory.Object;

    var coverageResult = CreateTestCoverageResult();
    string outputDirectory = "/fake/reports";
    string[] formats = ["json"];

    // Act
    List<string> generatedReports = collector.GenerateCoverageReportFiles(
      coverageResult,
      mockSourceRootTranslator.Object,
      mockFileSystem.Object,
      outputDirectory,
      formats);

    // Assert
    Assert.Single(generatedReports);
    Assert.EndsWith("coverage.json", RemoveTimestamp(generatedReports[0]));

    mockFileSystem.Verify(
      x => x.WriteAllText(
        It.Is<string>(path => System.Text.RegularExpressions.Regex.IsMatch(path, @"coverage\.\d{15}\.json$")),
        It.Is<string>(content => content.Contains("coverage"))),
      Times.Once);
  }

  [Fact]
  public void GenerateCoverageReportFilesWithFilePrefixCreatesReportWithPrefix()
  {
    // Arrange
    var mockFileSystem = new Mock<IFileSystem>();
    var mockSourceRootTranslator = new Mock<ISourceRootTranslator>();
    var mockReporterFactory = new Mock<IReporterFactory>();
    var mockReporter = new Mock<IReporter>();

    // Setup mocks
    mockReporter.Setup(x => x.OutputType).Returns(ReporterOutputType.File);
    mockReporter.Setup(x => x.Extension).Returns("json");
    mockReporter.Setup(x => x.Report(It.IsAny<CoverageResult>(), It.IsAny<ISourceRootTranslator>()))
      .Returns("{\"coverage\":\"data\"}");

    mockReporterFactory.Setup(x => x.CreateReporter("json")).Returns(mockReporter.Object);

    var collector = CreateCollectorForTesting(fileSystem: mockFileSystem.Object);
    collector.ReporterFactoryOverride = mockReporterFactory.Object;

    var coverageResult = CreateTestCoverageResult();
    string outputDirectory = "/fake/reports";
    string[] formats = ["json"];
    string filePrefix = "MyProject";

    // Act
    List<string> generatedReports = collector.GenerateCoverageReportFiles(
      coverageResult,
      mockSourceRootTranslator.Object,
      mockFileSystem.Object,
      outputDirectory,
      formats,
      filePrefix);

    // Assert
    Assert.Single(generatedReports);
    Assert.EndsWith("MyProject.coverage.json", RemoveTimestamp(generatedReports[0]));

    mockFileSystem.Verify(
      x => x.WriteAllText(
        It.Is<string>(path => System.Text.RegularExpressions.Regex.IsMatch(path, @"MyProject\.coverage\.\d{15}\.json$")),
        It.Is<string>(content => content.Contains("coverage"))),
      Times.Once);
  }

  [Fact]
  public void GenerateCoverageReportFilesWithNullFilePrefixCreatesReportWithoutPrefix()
  {
    // Arrange
    var mockFileSystem = new Mock<IFileSystem>();
    var mockSourceRootTranslator = new Mock<ISourceRootTranslator>();
    var mockReporterFactory = new Mock<IReporterFactory>();
    var mockReporter = new Mock<IReporter>();

    // Setup mocks
    mockReporter.Setup(x => x.OutputType).Returns(ReporterOutputType.File);
    mockReporter.Setup(x => x.Extension).Returns("cobertura.xml");
    mockReporter.Setup(x => x.Report(It.IsAny<CoverageResult>(), It.IsAny<ISourceRootTranslator>()))
      .Returns("<coverage />");

    mockReporterFactory.Setup(x => x.CreateReporter("cobertura")).Returns(mockReporter.Object);

    var collector = CreateCollectorForTesting(fileSystem: mockFileSystem.Object);
    collector.ReporterFactoryOverride = mockReporterFactory.Object;

    var coverageResult = CreateTestCoverageResult();
    string outputDirectory = "/fake/reports";
    string[] formats = ["cobertura"];

    // Act
    List<string> generatedReports = collector.GenerateCoverageReportFiles(
      coverageResult,
      mockSourceRootTranslator.Object,
      mockFileSystem.Object,
      outputDirectory,
      formats,
      filePrefix: null);

    // Assert
    Assert.Single(generatedReports);
    Assert.EndsWith("coverage.cobertura.xml", RemoveTimestamp(generatedReports[0]));

    mockFileSystem.Verify(
      x => x.WriteAllText(
        It.Is<string>(path => System.Text.RegularExpressions.Regex.IsMatch(path, @"coverage\.cobertura\.\d{15}\.xml")),
        It.IsAny<string>()),
      Times.Once);
  }

  [Fact]
  public void GenerateCoverageReportFilesWithEmptyFilePrefixCreatesReportWithoutPrefix()
  {
    // Arrange
    var mockFileSystem = new Mock<IFileSystem>();
    var mockSourceRootTranslator = new Mock<ISourceRootTranslator>();
    var mockReporterFactory = new Mock<IReporterFactory>();
    var mockReporter = new Mock<IReporter>();

    // Setup mocks
    mockReporter.Setup(x => x.OutputType).Returns(ReporterOutputType.File);
    mockReporter.Setup(x => x.Extension).Returns("lcov.info");
    mockReporter.Setup(x => x.Report(It.IsAny<CoverageResult>(), It.IsAny<ISourceRootTranslator>()))
      .Returns("TN:coverage");

    mockReporterFactory.Setup(x => x.CreateReporter("lcov")).Returns(mockReporter.Object);

    var collector = CreateCollectorForTesting(fileSystem: mockFileSystem.Object);
    collector.ReporterFactoryOverride = mockReporterFactory.Object;

    var coverageResult = CreateTestCoverageResult();
    string outputDirectory = "/fake/reports";
    string[] formats = ["lcov"];

    // Act
    List<string> generatedReports = collector.GenerateCoverageReportFiles(
      coverageResult,
      mockSourceRootTranslator.Object,
      mockFileSystem.Object,
      outputDirectory,
      formats,
      filePrefix: "");

    // Assert
    Assert.Single(generatedReports);
    Assert.EndsWith("coverage.lcov.info", RemoveTimestamp(generatedReports[0]));

    mockFileSystem.Verify(
      x => x.WriteAllText(
        It.Is<string>(path => System.Text.RegularExpressions.Regex.IsMatch(path, @"coverage.lcov\.\d{15}\.info$")),
        It.IsAny<string>()),
      Times.Once);
  }

  [Fact]
  public void GenerateCoverageReportFilesWithFilePrefixAndMultipleFormatsCreatesAllReportsWithPrefix()
  {
    // Arrange
    var mockFileSystem = new Mock<IFileSystem>();
    var mockSourceRootTranslator = new Mock<ISourceRootTranslator>();
    var mockReporterFactory = new Mock<IReporterFactory>();

    var mockJsonReporter = new Mock<IReporter>();
    mockJsonReporter.Setup(x => x.OutputType).Returns(ReporterOutputType.File);
    mockJsonReporter.Setup(x => x.Extension).Returns("json");
    mockJsonReporter.Setup(x => x.Report(It.IsAny<CoverageResult>(), It.IsAny<ISourceRootTranslator>()))
      .Returns("{\"coverage\":\"data\"}");

    var mockCoberturaReporter = new Mock<IReporter>();
    mockCoberturaReporter.Setup(x => x.OutputType).Returns(ReporterOutputType.File);
    mockCoberturaReporter.Setup(x => x.Extension).Returns("cobertura.xml");
    mockCoberturaReporter.Setup(x => x.Report(It.IsAny<CoverageResult>(), It.IsAny<ISourceRootTranslator>()))
      .Returns("<coverage />");

    mockReporterFactory.Setup(x => x.CreateReporter("json")).Returns(mockJsonReporter.Object);
    mockReporterFactory.Setup(x => x.CreateReporter("cobertura")).Returns(mockCoberturaReporter.Object);

    var collector = CreateCollectorForTesting(fileSystem: mockFileSystem.Object);
    collector.ReporterFactoryOverride = mockReporterFactory.Object;

    var coverageResult = CreateTestCoverageResult();
    string outputDirectory = "/fake/reports";
    string[] formats = ["json", "cobertura"];
    string filePrefix = "UnitTests";

    // Act
    List<string> generatedReports = collector.GenerateCoverageReportFiles(
      coverageResult,
      mockSourceRootTranslator.Object,
      mockFileSystem.Object,
      outputDirectory,
      formats,
      filePrefix);

    // Assert
    Assert.Equal(2, generatedReports.Count);
    Assert.Contains(generatedReports, r => System.Text.RegularExpressions.Regex.IsMatch(r, @"UnitTests\.coverage\.\d{15}\.json$"));
    Assert.Contains(generatedReports, r => System.Text.RegularExpressions.Regex.IsMatch(r, @"UnitTests\.coverage.cobertura\.\d{15}\.xml"));

    mockFileSystem.Verify(
      x => x.WriteAllText(
        It.Is<string>(path => System.Text.RegularExpressions.Regex.IsMatch(path, @"UnitTests\.coverage\.\d{15}\.json$")),
        It.IsAny<string>()),
      Times.Once);

    mockFileSystem.Verify(
      x => x.WriteAllText(
        It.Is<string>(path => System.Text.RegularExpressions.Regex.IsMatch(path, @"UnitTests\.coverage.cobertura\.\d{15}\.xml")),
        It.IsAny<string>()),
      Times.Once);
  }

  [Theory]
  [InlineData("../malicious")]
  [InlineData("..\\malicious")]
  [InlineData("path/traversal")]
  [InlineData("path\\traversal")]
  [InlineData("..")]
  [InlineData("..hidden")]
  public void GenerateCoverageReportFilesWithPathTraversalPrefixFallsBackToDefault(string maliciousPrefix)
  {
    // Arrange
    var mockFileSystem = new Mock<IFileSystem>();
    var mockSourceRootTranslator = new Mock<ISourceRootTranslator>();
    var mockReporterFactory = new Mock<IReporterFactory>();
    var mockReporter = new Mock<IReporter>();

    mockReporter.Setup(x => x.OutputType).Returns(ReporterOutputType.File);
    mockReporter.Setup(x => x.Extension).Returns("json");
    mockReporter.Setup(x => x.Report(It.IsAny<CoverageResult>(), It.IsAny<ISourceRootTranslator>()))
      .Returns("{\"coverage\":\"data\"}");

    mockReporterFactory.Setup(x => x.CreateReporter("json")).Returns(mockReporter.Object);

    var collector = CreateCollectorForTesting(fileSystem: mockFileSystem.Object);
    collector.ReporterFactoryOverride = mockReporterFactory.Object;

    var coverageResult = CreateTestCoverageResult();
    string outputDirectory = "/fake/reports";
    string[] formats = ["json"];

    // Act - Pass a malicious prefix that should be rejected
    List<string> generatedReports = collector.GenerateCoverageReportFiles(
      coverageResult,
      mockSourceRootTranslator.Object,
      mockFileSystem.Object,
      outputDirectory,
      formats,
      maliciousPrefix);

    // Assert - Should fall back to default filename without the malicious prefix
    Assert.Single(generatedReports);
    Assert.EndsWith("coverage.json", RemoveTimestamp(generatedReports[0]));
    Assert.DoesNotContain(maliciousPrefix, generatedReports[0]);

    mockFileSystem.Verify(
      x => x.WriteAllText(
        It.Is<string>(path => System.Text.RegularExpressions.Regex.IsMatch(path, @"coverage\.\d{15}\.json$") && !path.Contains(maliciousPrefix)),
        It.IsAny<string>()),
      Times.Once);
  }

  [Theory]
  [InlineData("<test")]
  [InlineData("test>")]
  [InlineData("test|file")]
  [InlineData("test*file")]
  [InlineData("test?file")]
  [InlineData("test\"file")]
  public void GenerateCoverageReportFilesWithInvalidFilenameCharsFallsBackToDefault(string invalidPrefix)
  {
    // Arrange
    var mockFileSystem = new Mock<IFileSystem>();
    var mockSourceRootTranslator = new Mock<ISourceRootTranslator>();
    var mockReporterFactory = new Mock<IReporterFactory>();
    var mockReporter = new Mock<IReporter>();

    mockReporter.Setup(x => x.OutputType).Returns(ReporterOutputType.File);
    mockReporter.Setup(x => x.Extension).Returns("json");
    mockReporter.Setup(x => x.Report(It.IsAny<CoverageResult>(), It.IsAny<ISourceRootTranslator>()))
      .Returns("{\"coverage\":\"data\"}");

    mockReporterFactory.Setup(x => x.CreateReporter("json")).Returns(mockReporter.Object);

    var collector = CreateCollectorForTesting(fileSystem: mockFileSystem.Object);
    collector.ReporterFactoryOverride = mockReporterFactory.Object;

    var coverageResult = CreateTestCoverageResult();
    string outputDirectory = "/fake/reports";
    string[] formats = ["json"];

    // Act - Pass a prefix with invalid filename characters that should be rejected
    List<string> generatedReports = collector.GenerateCoverageReportFiles(
      coverageResult,
      mockSourceRootTranslator.Object,
      mockFileSystem.Object,
      outputDirectory,
      formats,
      invalidPrefix);

    // Assert - Should fall back to default filename without the invalid prefix
    Assert.Single(generatedReports);
    Assert.EndsWith("coverage.json", RemoveTimestamp(generatedReports[0]));

    mockFileSystem.Verify(
      x => x.WriteAllText(
        It.Is<string>(path => System.Text.RegularExpressions.Regex.IsMatch(path, @"coverage\.\d{15}\.json$")),
        It.IsAny<string>()),
      Times.Once);
  }

  [Theory]
  [InlineData(" ")]
  [InlineData("   ")]
  [InlineData("\t")]
  [InlineData("\n")]
  public void GenerateCoverageReportFilesWithWhitespaceOnlyPrefixFallsBackToDefault(string whitespacePrefix)
  {
    // Arrange
    var mockFileSystem = new Mock<IFileSystem>();
    var mockSourceRootTranslator = new Mock<ISourceRootTranslator>();
    var mockReporterFactory = new Mock<IReporterFactory>();
    var mockReporter = new Mock<IReporter>();

    mockReporter.Setup(x => x.OutputType).Returns(ReporterOutputType.File);
    mockReporter.Setup(x => x.Extension).Returns("json");
    mockReporter.Setup(x => x.Report(It.IsAny<CoverageResult>(), It.IsAny<ISourceRootTranslator>()))
      .Returns("{\"coverage\":\"data\"}");

    mockReporterFactory.Setup(x => x.CreateReporter("json")).Returns(mockReporter.Object);

    var collector = CreateCollectorForTesting(fileSystem: mockFileSystem.Object);
    collector.ReporterFactoryOverride = mockReporterFactory.Object;

    var coverageResult = CreateTestCoverageResult();
    string outputDirectory = "/fake/reports";
    string[] formats = ["json"];

    // Act - Pass whitespace-only prefix that should be rejected
    List<string> generatedReports = collector.GenerateCoverageReportFiles(
      coverageResult,
      mockSourceRootTranslator.Object,
      mockFileSystem.Object,
      outputDirectory,
      formats,
      whitespacePrefix);

    // Assert - Should fall back to default filename without prefix
    Assert.Single(generatedReports);
    Assert.EndsWith("coverage.json", RemoveTimestamp(generatedReports[0]));

    mockFileSystem.Verify(
      x => x.WriteAllText(
        It.Is<string>(path => System.Text.RegularExpressions.Regex.IsMatch(path, @"coverage\.\d{15}\.json$")),
        It.IsAny<string>()),
      Times.Once);
  }

  /// <summary>
  /// Creates a CollectorExtension instance for testing with minimal setup.
  /// </summary>
  private static CollectorExtension CreateCollectorForTesting(
    IFileSystem? fileSystem = null,
    IReporterFactory? reporterFactory = null)
  {
    // Setup minimal command line options
    var mockCommandLineOptions = new Mock<ICommandLineOptions>();
    mockCommandLineOptions
      .Setup(x => x.IsOptionSet(It.IsAny<string>()))
      .Returns(false);
    mockCommandLineOptions
      .Setup(x => x.TryGetOptionArgumentList(It.IsAny<string>(), out It.Ref<string[]?>.IsAny))
      .Returns(false);

    // Setup minimal configuration
    var mockConfiguration = new Mock<IConfiguration>();
    mockConfiguration
      .Setup(x => x[It.IsAny<string>()])
      .Returns((string?)null);

    // Setup minimal output device
    var mockOutputDevice = new Mock<IOutputDevice>();

    // Setup minimal logger factory
    var mockLoggerFactory = new Mock<ILoggerFactory>();
    var mockLogger = new Mock<Microsoft.Testing.Platform.Logging.ILogger>();
    mockLoggerFactory
      .Setup(x => x.CreateLogger(It.IsAny<string>()))
      .Returns(mockLogger.Object);

    // Use provided or create default mocks
    IFileSystem testFileSystem = fileSystem ?? new Mock<IFileSystem>().Object;
    IReporterFactory testReporterFactory = reporterFactory ?? new Mock<IReporterFactory>().Object;

    return new CollectorExtension(
      mockLoggerFactory.Object,
      mockCommandLineOptions.Object,
      mockOutputDevice.Object,
      mockConfiguration.Object,
      testFileSystem,
      testReporterFactory);
  }

  private static string RemoveTimestamp(string filename)
  {
    // Matches a dot, 15 digits, and a dot (e.g., .280326084634246.)
    return System.Text.RegularExpressions.Regex.Replace(filename, @"\.\d{15}\.", ".");
  }
  #endregion

}

