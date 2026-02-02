// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Coverlet.Core;
using Coverlet.Core.Abstractions;
using Coverlet.MTP.CommandLine;
using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Configurations;
using Microsoft.Testing.Platform.Extensions.OutputDevice;
using Microsoft.Testing.Platform.Logging;
using Microsoft.Testing.Platform.OutputDevice;
using Moq;
using Xunit;

namespace Coverlet.MTP.Collector.Tests;

/// <summary>
/// Comprehensive tests for report generation methods in CollectorExtension:
/// - GenerateCoverageReportFiles
/// - DisplayGeneratedReportsAsync
/// - GenerateReportsAsync
/// - GetHitsFilePath
/// </summary>
public class CollectorExtensionReportMethodsTests
{
  private readonly Mock<ILoggerFactory> _mockLoggerFactory;
  private readonly Mock<Microsoft.Testing.Platform.Logging.ILogger> _mockLogger;
  private readonly Mock<ICommandLineOptions> _mockCommandLineOptions;
  private readonly Mock<IConfiguration> _mockConfiguration;
  private readonly Mock<IOutputDevice> _mockOutputDevice;
  private readonly Mock<IFileSystem> _mockFileSystem;
  private readonly Mock<ISourceRootTranslator> _mockSourceRootTranslator;

  private static readonly string s_simulatedTestModulePath = CreatePlatformPath("fake", "path", "test.dll");
  private static readonly string s_simulatedTestModuleDirectory = CreatePlatformPath("fake", "path");
  private static readonly string s_simulatedReportDirectory = CreatePlatformPath("fake", "reports");

  /// <summary>
  /// Creates a platform-specific path from path segments.
  /// This ensures paths work correctly on Windows, Linux, and macOS.
  /// </summary>
  /// <param name="segments">Path segments to combine</param>
  /// <returns>Platform-specific path</returns>
  private static string CreatePlatformPath(params string[] segments)
  {
    // For simulated/fake paths, start with directory separator to make it absolute
    string basePath = Path.DirectorySeparatorChar.ToString();
    return Path.Combine(basePath, Path.Combine(segments));
  }

  public CollectorExtensionReportMethodsTests()
  {
    _mockLoggerFactory = new Mock<ILoggerFactory>();
    _mockLogger = new Mock<Microsoft.Testing.Platform.Logging.ILogger>();
    _mockCommandLineOptions = new Mock<ICommandLineOptions>();
    _mockConfiguration = new Mock<IConfiguration>();
    _mockFileSystem = new Mock<IFileSystem>();
    _mockOutputDevice = new Mock<IOutputDevice>();
    _mockSourceRootTranslator = new Mock<ISourceRootTranslator>();

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
      .Setup(x => x.Exists(s_simulatedTestModulePath))
      .Returns(true);

    _mockFileSystem
      .Setup(x => x.Exists(s_simulatedTestModuleDirectory))
      .Returns(true);

    _mockFileSystem
      .Setup(x => x.Exists(s_simulatedReportDirectory))
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
    var modules = new Modules { { s_simulatedTestModulePath, documents } };

    return new CoverageResult
    {
      Identifier = "test-id",
      Modules = modules,
      Parameters = new CoverageParameters()
    };
  }

  private CollectorExtension CreateCollectorWithCoverageEnabled()
  {
    _mockCommandLineOptions
      .Setup(x => x.IsOptionSet(CoverletOptionNames.Coverage))
      .Returns(true);

    _mockConfiguration
      .Setup(x => x["TestModule"])
      .Returns(s_simulatedTestModulePath);

    _mockConfiguration
      .Setup(x => x["TestResultDirectory"])
      .Returns(s_simulatedReportDirectory);

    return new CollectorExtension(
      _mockLoggerFactory.Object,
      _mockCommandLineOptions.Object,
      _mockOutputDevice.Object,
      _mockConfiguration.Object,
      _mockFileSystem.Object);
  }

  #region GenerateCoverageReportFiles Tests

  [Fact]
  public void GenerateCoverageReportFilesWithJsonFormatWritesJsonFile()
  {
    // Arrange
    var collector = CreateCollectorWithCoverageEnabled();
    var result = CreateTestCoverageResult();
    string[] formats = ["json"];
    string expectedReportPath = Path.Combine(s_simulatedReportDirectory, "coverage.json");

    var mockReporter = new Mock<IReporter>();
    mockReporter.Setup(x => x.OutputType).Returns(ReporterOutputType.File);
    mockReporter.Setup(x => x.Extension).Returns("json");
    mockReporter.Setup(x => x.Report(result, _mockSourceRootTranslator.Object))
      .Returns("{\"coverage\":\"data\"}");

    var mockReporterFactory = new Mock<IReporterFactory>();
    mockReporterFactory.Setup(x => x.CreateReporter("json"))
      .Returns(mockReporter.Object);

    collector.ReporterFactoryOverride = mockReporterFactory.Object;

    // Act
    List<string> generatedReports = collector.GenerateCoverageReportFiles(
      result,
      _mockSourceRootTranslator.Object,
      _mockFileSystem.Object,
      s_simulatedReportDirectory,
      formats);

    // Assert
    Assert.Single(generatedReports);
    Assert.Equal(expectedReportPath, generatedReports[0]);
    _mockFileSystem.Verify(
      x => x.WriteAllText(expectedReportPath, "{\"coverage\":\"data\"}"),
      Times.Once);
  }

  [Fact]
  public void GenerateCoverageReportFilesWithMultipleFormatsWritesMultipleFiles()
  {
    // Arrange
    var collector = CreateCollectorWithCoverageEnabled();
    var result = CreateTestCoverageResult();
    string[] formats = ["json", "lcov", "cobertura"];

    var mockJsonReporter = new Mock<IReporter>();
    mockJsonReporter.Setup(x => x.OutputType).Returns(ReporterOutputType.File);
    mockJsonReporter.Setup(x => x.Extension).Returns("json");
    mockJsonReporter.Setup(x => x.Report(result, _mockSourceRootTranslator.Object))
      .Returns("{\"json\":\"data\"}");

    var mockLcovReporter = new Mock<IReporter>();
    mockLcovReporter.Setup(x => x.OutputType).Returns(ReporterOutputType.File);
    mockLcovReporter.Setup(x => x.Extension).Returns("info");
    mockLcovReporter.Setup(x => x.Report(result, _mockSourceRootTranslator.Object))
      .Returns("lcov data");

    var mockCoberturaReporter = new Mock<IReporter>();
    mockCoberturaReporter.Setup(x => x.OutputType).Returns(ReporterOutputType.File);
    mockCoberturaReporter.Setup(x => x.Extension).Returns("xml");
    mockCoberturaReporter.Setup(x => x.Report(result, _mockSourceRootTranslator.Object))
      .Returns("<coverage/>");

    var mockReporterFactory = new Mock<IReporterFactory>();
    mockReporterFactory.Setup(x => x.CreateReporter("json")).Returns(mockJsonReporter.Object);
    mockReporterFactory.Setup(x => x.CreateReporter("lcov")).Returns(mockLcovReporter.Object);
    mockReporterFactory.Setup(x => x.CreateReporter("cobertura")).Returns(mockCoberturaReporter.Object);

    collector.ReporterFactoryOverride = mockReporterFactory.Object;

    // Act
    List<string> generatedReports = collector.GenerateCoverageReportFiles(
      result,
      _mockSourceRootTranslator.Object,
      _mockFileSystem.Object,
      s_simulatedReportDirectory,
      formats);

    // Assert
    Assert.Equal(3, generatedReports.Count);
    Assert.Contains(Path.Combine(s_simulatedReportDirectory, "coverage.json"), generatedReports);
    Assert.Contains(Path.Combine(s_simulatedReportDirectory, "coverage.info"), generatedReports);
    Assert.Contains(Path.Combine(s_simulatedReportDirectory, "coverage.xml"), generatedReports);

    _mockFileSystem.Verify(x => x.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(3));
  }

  [Fact]
  public void GenerateCoverageReportFilesWithConsoleFormatDoesNotWriteFile()
  {
    // Arrange
    var collector = CreateCollectorWithCoverageEnabled();
    var result = CreateTestCoverageResult();
    string[] formats = ["teamcity"];

    var mockReporter = new Mock<IReporter>();
    mockReporter.Setup(x => x.OutputType).Returns(ReporterOutputType.Console);
    mockReporter.Setup(x => x.Report(result, _mockSourceRootTranslator.Object))
      .Returns("Console output");

    var mockReporterFactory = new Mock<IReporterFactory>();
    mockReporterFactory.Setup(x => x.CreateReporter("teamcity"))
      .Returns(mockReporter.Object);

    collector.ReporterFactoryOverride = mockReporterFactory.Object;

    // Act
    List<string> generatedReports = collector.GenerateCoverageReportFiles(
      result,
      _mockSourceRootTranslator.Object,
      _mockFileSystem.Object,
      s_simulatedReportDirectory,
      formats);

    // Assert
    Assert.Empty(generatedReports);
    _mockFileSystem.Verify(x => x.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
  }

  [Fact]
  public void GenerateCoverageReportFilesWithMixedFormatTypesWritesOnlyFileReports()
  {
    // Arrange
    var collector = CreateCollectorWithCoverageEnabled();
    var result = CreateTestCoverageResult();
    string[] formats = ["json", "teamcity"];

    var mockJsonReporter = new Mock<IReporter>();
    mockJsonReporter.Setup(x => x.OutputType).Returns(ReporterOutputType.File);
    mockJsonReporter.Setup(x => x.Extension).Returns("json");
    mockJsonReporter.Setup(x => x.Report(result, _mockSourceRootTranslator.Object))
      .Returns("{\"json\":\"data\"}");

    var mockTeamCityReporter = new Mock<IReporter>();
    mockTeamCityReporter.Setup(x => x.OutputType).Returns(ReporterOutputType.Console);
    mockTeamCityReporter.Setup(x => x.Report(result, _mockSourceRootTranslator.Object))
      .Returns("Console output");

    var mockReporterFactory = new Mock<IReporterFactory>();
    mockReporterFactory.Setup(x => x.CreateReporter("json")).Returns(mockJsonReporter.Object);
    mockReporterFactory.Setup(x => x.CreateReporter("teamcity")).Returns(mockTeamCityReporter.Object);

    collector.ReporterFactoryOverride = mockReporterFactory.Object;

    // Act
    List<string> generatedReports = collector.GenerateCoverageReportFiles(
      result,
      _mockSourceRootTranslator.Object,
      _mockFileSystem.Object,
      s_simulatedReportDirectory,
      formats);

    // Assert
    Assert.Single(generatedReports);
    _mockFileSystem.Verify(x => x.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
  }

  [Fact]
  public void GenerateCoverageReportFilesWithInvalidFormatThrowsInvalidOperationException()
  {
    // Arrange
    var collector = CreateCollectorWithCoverageEnabled();
    var result = CreateTestCoverageResult();
    string[] formats = ["invalid-format"];

    var mockReporterFactory = new Mock<IReporterFactory>();
    mockReporterFactory.Setup(x => x.CreateReporter("invalid-format"))
      .Returns((IReporter?)null);

    collector.ReporterFactoryOverride = mockReporterFactory.Object;

    // Act & Assert
    InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
      collector.GenerateCoverageReportFiles(
        result,
        _mockSourceRootTranslator.Object,
        _mockFileSystem.Object,
        s_simulatedReportDirectory,
        formats));

    Assert.Contains("invalid-format", ex.Message);
    Assert.Contains("not supported", ex.Message);
  }

  [Fact]
  public void GenerateCoverageReportFilesWithEmptyFormatsArrayReturnsEmptyList()
  {
    // Arrange
    var collector = CreateCollectorWithCoverageEnabled();
    var result = CreateTestCoverageResult();
    string[] formats = [];

    // Act
    List<string> generatedReports = collector.GenerateCoverageReportFiles(
      result,
      _mockSourceRootTranslator.Object,
      _mockFileSystem.Object,
      s_simulatedReportDirectory,
      formats);

    // Assert
    Assert.Empty(generatedReports);
    _mockFileSystem.Verify(x => x.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
  }

  #endregion

  #region DisplayGeneratedReportsAsync Tests

  [Fact]
  public async Task DisplayGeneratedReportsAsyncWithEmptyListDoesNotCallOutputDevice()
  {
    // Arrange
    var collector = CreateCollectorWithCoverageEnabled();
    var emptyReports = new List<string>();

    // Use reflection to call private method
    System.Reflection.MethodInfo? method = typeof(CollectorExtension)
      .GetMethod("DisplayGeneratedReportsAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

    Assert.NotNull(method);

    // Act
    await (Task)method.Invoke(collector, [emptyReports, CancellationToken.None])!;

    // Assert
    _mockOutputDevice.Verify(
      x => x.DisplayAsync(It.IsAny<IOutputDeviceDataProducer>(), It.IsAny<IOutputDeviceData>(), It.IsAny<CancellationToken>()),
      Times.Never);
  }

  [Fact]
  public async Task DisplayGeneratedReportsAsyncWithSingleReportCallsOutputDevice()
  {
    // Arrange
    var collector = CreateCollectorWithCoverageEnabled();
    var reports = new List<string> { CreatePlatformPath("fake", "reports", "coverage.json") };

    _mockOutputDevice.Setup(x => x.DisplayAsync(
      It.IsAny<IOutputDeviceDataProducer>(),
      It.IsAny<IOutputDeviceData>(),
      It.IsAny<CancellationToken>()))
      .Returns(Task.CompletedTask);

    // Use reflection to call private method
    System.Reflection.MethodInfo? method = typeof(CollectorExtension)
      .GetMethod("DisplayGeneratedReportsAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

    Assert.NotNull(method);

    // Act
    await (Task)method.Invoke(collector, [reports, CancellationToken.None])!;

    // Assert
    _mockOutputDevice.Verify(
      x => x.DisplayAsync(
        It.Is<IOutputDeviceDataProducer>(p => p == collector),
        It.Is<IOutputDeviceData>(data => data is TextOutputDeviceData),
        It.IsAny<CancellationToken>()),
      Times.Once);
  }

  [Fact]
  public async Task DisplayGeneratedReportsAsyncWithMultipleReportsCallsOutputDevice()
  {
    // Arrange
    var collector = CreateCollectorWithCoverageEnabled();
    var reports = new List<string>
    {
      CreatePlatformPath("fake", "reports", "coverage.json"),
      CreatePlatformPath("fake", "reports", "coverage.xml"),
      CreatePlatformPath("fake", "reports", "coverage.info")
    };

    _mockOutputDevice.Setup(x => x.DisplayAsync(
      It.IsAny<IOutputDeviceDataProducer>(),
      It.IsAny<IOutputDeviceData>(),
      It.IsAny<CancellationToken>()))
      .Returns(Task.CompletedTask);

    // Use reflection to call private method
    System.Reflection.MethodInfo? method = typeof(CollectorExtension)
      .GetMethod("DisplayGeneratedReportsAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

    Assert.NotNull(method);

    // Act
    await (Task)method.Invoke(collector, [reports, CancellationToken.None])!;

    // Assert
    _mockOutputDevice.Verify(
      x => x.DisplayAsync(
        It.IsAny<IOutputDeviceDataProducer>(),
        It.IsAny<IOutputDeviceData>(),
        It.IsAny<CancellationToken>()),
      Times.Once);
  }

  [Fact]
  public async Task DisplayGeneratedReportsAsyncRespectsCancellationToken()
  {
    // Arrange
    var collector = CreateCollectorWithCoverageEnabled();
    var reports = new List<string> { CreatePlatformPath("fake", "reports", "coverage.json") };
    using var cts = new CancellationTokenSource();
    CancellationToken token = cts.Token;

    _mockOutputDevice.Setup(x => x.DisplayAsync(
      It.IsAny<IOutputDeviceDataProducer>(),
      It.IsAny<IOutputDeviceData>(),
      token))
      .Returns(Task.CompletedTask);

    // Use reflection to call private method
    System.Reflection.MethodInfo? method = typeof(CollectorExtension)
      .GetMethod("DisplayGeneratedReportsAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

    Assert.NotNull(method);

    // Act
    await (Task)method.Invoke(collector, [reports, token])!;

    // Assert
    _mockOutputDevice.Verify(
      x => x.DisplayAsync(
        It.IsAny<IOutputDeviceDataProducer>(),
        It.IsAny<IOutputDeviceData>(),
        token),
      Times.Once);
  }

  #endregion

  #region GetHitsFilePath Tests

  [Fact]
  public void GetHitsFilePathReturnsDirectoryOfTestModule()
  {
    // Arrange
    _mockCommandLineOptions
      .Setup(x => x.IsOptionSet(CoverletOptionNames.Coverage))
      .Returns(true);

    _mockConfiguration
      .Setup(x => x["TestModule"])
      .Returns(s_simulatedTestModulePath);

    var collector = new CollectorExtension(
      _mockLoggerFactory.Object,
      _mockCommandLineOptions.Object,
      _mockOutputDevice.Object,
      _mockConfiguration.Object,
      _mockFileSystem.Object);

    // Use reflection to access private field _testModulePath
    System.Reflection.FieldInfo? field = typeof(CollectorExtension)
      .GetField("_testModulePath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
    Assert.NotNull(field);
    field.SetValue(collector, s_simulatedTestModulePath);

    // Use reflection to call private method GetHitsFilePath
    System.Reflection.MethodInfo? method = typeof(CollectorExtension)
      .GetMethod("GetHitsFilePath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
    Assert.NotNull(method);

    // Act
    string? hitsPath = (string?)method.Invoke(collector, null);

    // Assert
    Assert.NotNull(hitsPath);
    Assert.Equal(s_simulatedTestModuleDirectory, hitsPath);
  }

  [Fact]
  public void GetHitsFilePathWithNullTestModulePathReturnsEmpty()
  {
    // Arrange
    var collector = CreateCollectorWithCoverageEnabled();

    // Use reflection to access private field _testModulePath and set it to null
    System.Reflection.FieldInfo? field = typeof(CollectorExtension)
      .GetField("_testModulePath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
    Assert.NotNull(field);
    field.SetValue(collector, null);

    // Use reflection to call private method GetHitsFilePath
    System.Reflection.MethodInfo? method = typeof(CollectorExtension)
      .GetMethod("GetHitsFilePath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
    Assert.NotNull(method);

    // Act
    string? hitsPath = (string?)method.Invoke(collector, null);

    // Assert
    Assert.NotNull(hitsPath);
    Assert.Equal(string.Empty, hitsPath);
  }

  [Fact]
  public void GetHitsFilePathWithEmptyTestModulePathReturnsEmpty()
  {
    // Arrange
    var collector = CreateCollectorWithCoverageEnabled();

    // Use reflection to access private field _testModulePath and set it to empty
    System.Reflection.FieldInfo? field = typeof(CollectorExtension)
      .GetField("_testModulePath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
    Assert.NotNull(field);
    field.SetValue(collector, string.Empty);

    // Use reflection to call private method GetHitsFilePath
    System.Reflection.MethodInfo? method = typeof(CollectorExtension)
      .GetMethod("GetHitsFilePath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
    Assert.NotNull(method);

    // Act
    string? hitsPath = (string?)method.Invoke(collector, null);

    // Assert
    Assert.NotNull(hitsPath);
    Assert.Equal(string.Empty, hitsPath);
  }

  [Theory]
  [MemberData(nameof(GetPathTestData))]
  public void GetHitsFilePathReturnsCorrectDirectoryForVariousPaths(string modulePath, string expectedDirectory)
  {
    // Arrange
    var collector = CreateCollectorWithCoverageEnabled();

    // Use reflection to access private field _testModulePath
    System.Reflection.FieldInfo? field = typeof(CollectorExtension)
      .GetField("_testModulePath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
    Assert.NotNull(field);
    field.SetValue(collector, modulePath);

    // Use reflection to call private method GetHitsFilePath
    System.Reflection.MethodInfo? method = typeof(CollectorExtension)
      .GetMethod("GetHitsFilePath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
    Assert.NotNull(method);

    // Act
    string? hitsPath = (string?)method.Invoke(collector, null);

    // Assert
    Assert.NotNull(hitsPath);
    Assert.Equal(expectedDirectory, hitsPath);
  }

  /// <summary>
  /// Provides platform-specific test data for path testing.
  /// This ensures tests work correctly on Windows, Linux, and macOS.
  /// </summary>
  public static TheoryData<string, string> GetPathTestData()
  {
    return new TheoryData<string, string>
    {
      {
        CreatePlatformPath("fake", "path", "to", "assembly", "test.dll"),
        CreatePlatformPath("fake", "path", "to", "assembly")
      },
      {
        CreatePlatformPath("another", "directory", "mytest.dll"),
        CreatePlatformPath("another", "directory")
      },
      {
        CreatePlatformPath("deeply", "nested", "folder", "structure", "test.dll"),
        CreatePlatformPath("deeply", "nested", "folder", "structure")
      }
    };
  }

  #endregion
}
