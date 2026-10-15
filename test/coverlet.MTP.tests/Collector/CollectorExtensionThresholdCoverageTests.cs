// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Coverlet.Core;
using Coverlet.Core.Abstractions;
using Coverlet.Core.Enums;
using Coverlet.MTP.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Configurations;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Extensions.OutputDevice;
using Microsoft.Testing.Platform.Logging;
using Microsoft.Testing.Platform.Messages;
using Microsoft.Testing.Platform.OutputDevice;
using Moq;
using Xunit;

namespace Coverlet.MTP.Collector.Tests;

public class CollectorExtensionThresholdCoverageTests
{
  private const string SimulatedTestModulePath = "/fake/path/test.dll";
  private const string SimulatedReportDirectory = "/fake/reports";

  [Fact]
  public void InitializeCoverageWhenServiceProviderIsNullKeepsCoverageUnset()
  {
    CollectorExtension collector = CreateCollector();
    ConfigureCollectorState(collector, formats: ["json"], threshold: null, thresholdTypes: ["line"]);

    SetPrivateField(collector, "_serviceProvider", null);

    InvokeInitializeCoverage(collector);

    Assert.Null(GetPrivateField<ICoverage>(collector, "_coverage"));
  }

  [Fact]
  public void InitializeCoverageWhenServiceProviderIsAvailableCreatesCoverageInstance()
  {
    CollectorExtension collector = CreateCollector();
    ConfigureCollectorState(collector, formats: ["json"], threshold: null, thresholdTypes: ["line"]);

    var services = new ServiceCollection();
    services.AddSingleton(CreateMockFileSystem().Object);
    services.AddSingleton(Mock.Of<ISourceRootTranslator>());
    services.AddSingleton(Mock.Of<IInstrumentationHelper>());
    services.AddSingleton(Mock.Of<ICecilSymbolHelper>());
    SetPrivateField(collector, "_serviceProvider", services.BuildServiceProvider());

    InvokeInitializeCoverage(collector);

    Assert.NotNull(GetPrivateField<ICoverage>(collector, "_coverage"));
  }

  [Fact]
  public async Task PublishCoverageDataAsyncPublishesReportMessagesForDetectedFormatsAndSkipsEmptyFormat()
  {
    Mock<IMessageBus> messageBus = CreateMockMessageBus();
    var publishedReportMessages = new List<TestCoverageReportMessage>();
    messageBus.Setup(bus => bus.PublishAsync(It.IsAny<IDataProducer>(), It.IsAny<IData>()))
      .Callback<IDataProducer, IData>((_, data) =>
      {
        if (data is TestCoverageReportMessage reportMessage)
        {
          publishedReportMessages.Add(reportMessage);
        }
      })
      .Returns(Task.CompletedTask);

    CollectorExtension collector = CreateCollector(messageBus: messageBus.Object);
    ConfigureCollectorState(collector, formats: [], threshold: null, thresholdTypes: ["line"]);

    CoverageResult coverageResult = CreateCoverageResult(hits: 1);
    var generatedReports = new List<string>
    {
      "/fake/reports/coverage.cobertura.123.xml",
      "/fake/reports/coverage.opencover.123.xml",
      "/fake/reports/coverage.info",
      "/fake/reports/coverage.json",
      "/fake/reports/coverage.",
    };

    await InvokePublishCoverageDataAsync(collector, coverageResult, generatedReports);

    Assert.Equal(4, publishedReportMessages.Count);
    Assert.Contains(publishedReportMessages, message => message.Format == CoverageReportFormat.Cobertura);
    Assert.Contains(publishedReportMessages, message => message.Format == CoverageReportFormat.OpenCover);
    Assert.Contains(publishedReportMessages, message => message.Format == CoverageReportFormat.Lcov);
    Assert.Contains(publishedReportMessages, message =>
      message.Format == CoverageReportFormat.Custom && message.CustomFormatName == "coverlet.json");
  }

  [Theory]
  [InlineData("line")]
  [InlineData("branch")]
  [InlineData("method")]
  public void GetCoveragePercentReturnsExpectedCoverageForThresholdType(string thresholdTypeName)
  {
    Documents module = CreateCoverageResult(hits: 1).Modules.Values.Single();

    ThresholdTypeFlags thresholdType = thresholdTypeName switch
    {
      "line" => ThresholdTypeFlags.Line,
      "branch" => ThresholdTypeFlags.Branch,
      "method" => ThresholdTypeFlags.Method,
      _ => throw new ArgumentOutOfRangeException(nameof(thresholdTypeName), thresholdTypeName, "Unsupported threshold type."),
    };

    double actualCoveragePercent = InvokeGetCoveragePercent(module, thresholdType);
    double expectedCoveragePercent = thresholdType switch
    {
      ThresholdTypeFlags.Line => CoverageSummary.CalculateLineCoverage(module).Percent,
      ThresholdTypeFlags.Branch => CoverageSummary.CalculateBranchCoverage(module).Percent,
      ThresholdTypeFlags.Method => CoverageSummary.CalculateMethodCoverage(module).Percent,
      _ => throw new InvalidOperationException($"Unsupported threshold type {thresholdType}"),
    };

    Assert.Equal(expectedCoveragePercent, actualCoveragePercent);
  }

  [Fact]
  public void GetCoveragePercentWhenThresholdTypeIsInvalidThrowsArgumentOutOfRangeException()
  {
    Documents module = CreateCoverageResult(hits: 1).Modules.Values.Single();

    TargetInvocationException ex = Assert.Throws<TargetInvocationException>(() => InvokeGetCoveragePercent(module, ThresholdTypeFlags.None));
    Assert.IsType<ArgumentOutOfRangeException>(ex.InnerException);
  }

  [Theory]
  [InlineData(null, false)]
  [InlineData(new[] { "1, 14 ; 7" }, true)]
  [InlineData(new[] { "1,13;7" }, false)]
  public void IsCoverageThresholdExitCodeIgnoredReturnsExpectedValue(string[]? ignoredExitCodes, bool expected)
  {
    Mock<ICommandLineOptions> commandLineOptions = CreateMockCommandLineOptions();

    if (ignoredExitCodes is null)
    {
      commandLineOptions
        .Setup(x => x.TryGetOptionArgumentList("ignore-exit-code", out It.Ref<string[]?>.IsAny))
        .Returns(false);
    }
    else
    {
      commandLineOptions
        .Setup(x => x.TryGetOptionArgumentList("ignore-exit-code", out ignoredExitCodes))
        .Returns(true);
    }

    CollectorExtension collector = CreateCollector(commandLineOptions: commandLineOptions.Object);

    bool isIgnored = InvokeIsCoverageThresholdExitCodeIgnored(collector);

    Assert.Equal(expected, isIgnored);
  }

  [Fact]
  public async Task GenerateReportsAsyncWhenThresholdFailsAndExitCodeIsIgnoredQueriesIgnoreExitCodeOption()
  {
    string[]? ignoredExitCodes = ["14"];
    Mock<ICommandLineOptions> commandLineOptions = CreateMockCommandLineOptions();
    commandLineOptions
      .Setup(x => x.TryGetOptionArgumentList("ignore-exit-code", out ignoredExitCodes))
      .Returns(true);

    CollectorExtension collector = CreateCollector(commandLineOptions: commandLineOptions.Object);
    ConfigureCollectorState(collector, formats: [], threshold: 80, thresholdTypes: ["line"]);

    await InvokeGenerateReportsAsync(collector, CreateCoverageResult(hits: 0));

    commandLineOptions.Verify(x => x.TryGetOptionArgumentList("ignore-exit-code", out ignoredExitCodes), Times.Once);
  }

  [Fact]
  public async Task GenerateReportsAsyncWhenThresholdFailsAndExitCodeIsNotIgnoredStillQueriesIgnoreExitCodeOption()
  {
    string[]? ignoredExitCodes = ["13"];
    Mock<ICommandLineOptions> commandLineOptions = CreateMockCommandLineOptions();
    commandLineOptions
      .Setup(x => x.TryGetOptionArgumentList("ignore-exit-code", out ignoredExitCodes))
      .Returns(true);

    CollectorExtension collector = CreateCollector(commandLineOptions: commandLineOptions.Object);
    ConfigureCollectorState(collector, formats: [], threshold: 80, thresholdTypes: ["line"]);

    await InvokeGenerateReportsAsync(collector, CreateCoverageResult(hits: 0));

    commandLineOptions.Verify(x => x.TryGetOptionArgumentList("ignore-exit-code", out ignoredExitCodes), Times.Once);
  }

  private static CollectorExtension CreateCollector(
    ICommandLineOptions? commandLineOptions = null,
    IFileSystem? fileSystem = null,
    IMessageBus? messageBus = null)
  {
    Mock<ILoggerFactory> loggerFactory = new();
    loggerFactory.Setup(factory => factory.CreateLogger(It.IsAny<string>()))
      .Returns(new Mock<Microsoft.Testing.Platform.Logging.ILogger>().Object);

    ICommandLineOptions commandLine = commandLineOptions ?? CreateMockCommandLineOptions().Object;
    IFileSystem mockedFileSystem = fileSystem ?? CreateMockFileSystem().Object;

    Mock<IOutputDevice> outputDevice = new();
    outputDevice.Setup(device => device.DisplayAsync(
      It.IsAny<IOutputDeviceDataProducer>(),
      It.IsAny<IOutputDeviceData>(),
      It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

    Mock<IConfiguration> configuration = new();
    configuration.Setup(x => x["TestResultDirectory"]).Returns(SimulatedReportDirectory);

    return new CollectorExtension(
      loggerFactory.Object,
      commandLine,
      outputDevice.Object,
      configuration.Object,
      mockedFileSystem,
      messageBus: messageBus);
  }

  private static Mock<ICommandLineOptions> CreateMockCommandLineOptions()
  {
    Mock<ICommandLineOptions> commandLineOptions = new();
    commandLineOptions.Setup(x => x.IsOptionSet(It.IsAny<string>())).Returns(false);
    commandLineOptions.Setup(x => x.TryGetOptionArgumentList(It.IsAny<string>(), out It.Ref<string[]?>.IsAny)).Returns(false);
    return commandLineOptions;
  }

  private static Mock<IMessageBus> CreateMockMessageBus()
  {
    Mock<IMessageBus> messageBus = new();
    messageBus.Setup(bus => bus.PublishAsync(It.IsAny<IDataProducer>(), It.IsAny<IData>())).Returns(Task.CompletedTask);
    return messageBus;
  }

  private static Mock<IFileSystem> CreateMockFileSystem()
  {
    Mock<IFileSystem> fileSystem = new();
    fileSystem.Setup(x => x.Exists(It.IsAny<string>())).Returns(true);
    fileSystem.Setup(x => x.CreateDirectory(It.IsAny<string>()));
    fileSystem.Setup(x => x.WriteAllText(It.IsAny<string>(), It.IsAny<string>()));
    return fileSystem;
  }

  private static void ConfigureCollectorState(
    CollectorExtension collector,
    string[] formats,
    int? threshold,
    List<string> thresholdTypes)
  {
    SetPrivateField(collector, "_testModulePath", SimulatedTestModulePath);
    SetPrivateField(collector, "_platformConfiguration", new TestPlatformConfiguration(SimulatedReportDirectory));

    var services = new ServiceCollection();
    services.AddSingleton(CreateMockFileSystem().Object);
    services.AddSingleton(Mock.Of<ISourceRootTranslator>());
    SetPrivateField(collector, "_serviceProvider", services.BuildServiceProvider());

    CoverletExtensionConfiguration extensionConfiguration = GetPrivateField<CoverletExtensionConfiguration>(collector, "_configuration")!;
    extensionConfiguration.TestModule = SimulatedTestModulePath;
    extensionConfiguration.formats = formats;
    extensionConfiguration.Threshold = threshold;
    extensionConfiguration.ThresholdType = thresholdTypes;
    extensionConfiguration.ThresholdStat = ThresholdStatistic.Total;
  }

  private static void InvokeInitializeCoverage(CollectorExtension collector)
  {
    MethodInfo method = typeof(CollectorExtension)
      .GetMethod("InitializeCoverage", BindingFlags.NonPublic | BindingFlags.Instance)!;
    method.Invoke(collector, null);
  }

  private static async Task InvokePublishCoverageDataAsync(
    CollectorExtension collector,
    CoverageResult coverageResult,
    IReadOnlyList<string> generatedReports)
  {
    MethodInfo method = typeof(CollectorExtension)
      .GetMethod("PublishCoverageDataAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;

    await (Task)method.Invoke(collector, [coverageResult, generatedReports, CancellationToken.None])!;
  }

  private static async Task InvokeGenerateReportsAsync(CollectorExtension collector, CoverageResult coverageResult)
  {
    MethodInfo method = typeof(CollectorExtension)
      .GetMethod("GenerateReportsAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;

    await (Task)method.Invoke(collector, [coverageResult, 0, CancellationToken.None])!;
  }

  private static double InvokeGetCoveragePercent(Documents module, ThresholdTypeFlags thresholdType)
  {
    MethodInfo method = typeof(CollectorExtension)
      .GetMethod("GetCoveragePercent", BindingFlags.NonPublic | BindingFlags.Static)!;

    return (double)method.Invoke(null, [module, thresholdType])!;
  }

  private static bool InvokeIsCoverageThresholdExitCodeIgnored(CollectorExtension collector)
  {
    MethodInfo method = typeof(CollectorExtension)
      .GetMethod("IsCoverageThresholdExitCodeIgnored", BindingFlags.NonPublic | BindingFlags.Instance)!;

    return (bool)method.Invoke(collector, null)!;
  }

  private static T? GetPrivateField<T>(CollectorExtension collector, string fieldName)
  {
    FieldInfo field = typeof(CollectorExtension)
      .GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)!;

    return (T?)field.GetValue(collector);
  }

  private static void SetPrivateField(CollectorExtension collector, string fieldName, object? value)
  {
    FieldInfo field = typeof(CollectorExtension)
      .GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)!;

    field.SetValue(collector, value);
  }

  private sealed class TestPlatformConfiguration : IConfiguration
  {
    private readonly string? _resultDirectory;

    public TestPlatformConfiguration(string? resultDirectory)
    {
      _resultDirectory = resultDirectory;
    }

    public string? this[string key] => key switch
    {
      "platformOptions:resultDirectory" => _resultDirectory,
      "platformOptions:currentWorkingDirectory" => Path.GetDirectoryName(SimulatedTestModulePath),
      _ => null,
    };
  }

  private static CoverageResult CreateCoverageResult(int hits)
  {
    var methods = new Methods
    {
      ["System.Void TestClass::TestMethod()"] = new Method
      {
        Lines = new Lines { { 1, hits }, { 2, 0 } },
        Branches =
        [
          new BranchInfo { Line = 1, Hits = hits, Path = 0, Ordinal = 0 },
          new BranchInfo { Line = 1, Hits = 0, Path = 1, Ordinal = 1 },
        ],
      },
    };

    var classes = new Classes { ["TestClass"] = methods };
    var documents = new Documents { ["TestClass.cs"] = classes };

    return new CoverageResult
    {
      Identifier = "test-id",
      Modules = new Modules { [SimulatedTestModulePath] = documents },
      Parameters = new CoverageParameters(),
    };
  }
}
