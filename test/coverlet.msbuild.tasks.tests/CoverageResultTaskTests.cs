// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using coverlet.msbuild.tasks.tests;
using Coverlet.Core.Abstractions;
using Coverlet.Core.Helpers;
using Coverlet.MSbuild.Tasks;
using Microsoft.Build.Framework;
using Microsoft.Build.Locator;
using Microsoft.Build.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

[assembly: AssemblyFixture(typeof(MSBuildFixture))]

namespace coverlet.msbuild.tasks.tests
{

  public class MSBuildFixture
  {
    public MSBuildFixture()
    {
      MSBuildLocator.RegisterDefaults();
    }
  }

  public class CoverageResultTaskTests
  {
    private readonly Mock<IBuildEngine> _buildEngine;
    private readonly List<BuildErrorEventArgs> _errors;
    private readonly Mock<IAssemblyAdapter> _mockAssemblyAdapter;

    public CoverageResultTaskTests()
    {
      _buildEngine = new Mock<IBuildEngine>();
      _errors = new List<BuildErrorEventArgs>();
      _mockAssemblyAdapter = new Mock<IAssemblyAdapter>();
      _mockAssemblyAdapter.Setup(x => x.GetAssemblyName(It.IsAny<string>())).Returns("abc");
    }

    [Fact]
    public void Execute_StateUnderTest_MissingInstrumentationState()
    {
      // Arrange
      IServiceCollection serviceCollection = new ServiceCollection();
      serviceCollection.AddTransient<IFileSystem, FileSystem>();

      ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();
      BaseTask.ServiceProvider = serviceProvider;

      _buildEngine.Setup(x => x.LogErrorEvent(It.IsAny<BuildErrorEventArgs>())).Callback<BuildErrorEventArgs>(e => _errors.Add(e));

      var coverageResultTask = new CoverageResultTask
      {
        OutputFormat = "cobertura",
        Output = "coverageDir",
        Threshold = "50",
        ThresholdType = "total",
        ThresholdStat = "total",
        InstrumenterState = null
      };
      coverageResultTask.BuildEngine = _buildEngine.Object;

      // Act
      bool success = coverageResultTask.Execute();

      // Assert
      Assert.Equal("[coverlet] Result of instrumentation task not found", _errors.FirstOrDefault()?.Message);
    }

    [Fact]
    public void Execute_StateUnderTest_WithInstrumentationState_ThresholdType()
    {
      // Arrange
      var mockFileSystem = new Mock<IFileSystem>();
      mockFileSystem.Setup(x => x.Exists(It.IsAny<string>())).Returns(true);
      mockFileSystem.Setup(x => x.Copy(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()));
      var log = new TaskLoggingHelper(_buildEngine.Object, "CoverageResultTask");

      IServiceCollection serviceCollection = new ServiceCollection();
      serviceCollection.AddTransient<IFileSystem, FileSystem>();
      serviceCollection.AddTransient<IProcessExitHandler, ProcessExitHandler>();
      serviceCollection.AddTransient<Coverlet.Core.Abstractions.ILogger, MSBuildLogger>(_ => new MSBuildLogger(log));
      serviceCollection.AddTransient<IRetryHelper, RetryHelper>();
      serviceCollection.AddSingleton<IInstrumentationHelper, InstrumentationHelperForDebugging>();
      serviceCollection.AddSingleton<ISourceRootTranslator, SourceRootTranslator>(serviceProvider => new SourceRootTranslator("empty", serviceProvider.GetRequiredService<Coverlet.Core.Abstractions.ILogger>(), mockFileSystem.Object, _mockAssemblyAdapter.Object));
      ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();
      BaseTask.ServiceProvider = serviceProvider;
      _buildEngine.Setup(x => x.LogErrorEvent(It.IsAny<BuildErrorEventArgs>())).Callback<BuildErrorEventArgs>(e => _errors.Add(e));

      var baseDirectory = AppDomain.CurrentDomain.SetupInformation.ApplicationBase ?? string.Empty;
      var InstrumenterState = new TaskItem(Path.Combine(baseDirectory, "TestAssets\\InstrumenterState.ItemSpec.data1.xml"));

      var coverageResultTask = new CoverageResultTask
      {
        OutputFormat = "cobertura",
        Output = "coverageDir",
        Threshold = "50,60,70",
        ThresholdType = "line,branch,method",
        ThresholdStat = "total",
        InstrumenterState = InstrumenterState
      };
      coverageResultTask.BuildEngine = _buildEngine.Object;

      // Act
      bool success = coverageResultTask.Execute();

      // Assert
      Assert.False(success);
      Assert.True(coverageResultTask.Log.HasLoggedErrors);

      // Verify the error message
      string expectedErrorMessage = $"The total line coverage is below the specified 50{Environment.NewLine}" +
                           $"The total branch coverage is below the specified 60{Environment.NewLine}" +
                           "The total method coverage is below the specified 70";

      Assert.Contains(expectedErrorMessage, _errors[0].Message);

      Assert.Contains("coverageDir.cobertura.xml", coverageResultTask.ReportItems[0].ItemSpec);
      Assert.Equal(16, coverageResultTask.ReportItems[0].MetadataCount);
    }

  }
  class InstrumentationHelperForDebugging : InstrumentationHelper
  {
    public InstrumentationHelperForDebugging(IProcessExitHandler processExitHandler, IRetryHelper retryHelper, IFileSystem fileSystem, Coverlet.Core.Abstractions.ILogger logger, ISourceRootTranslator sourceTranslator)
        : base(processExitHandler, retryHelper, fileSystem, logger, sourceTranslator)
    {

    }

    public override void RestoreOriginalModule(string module, string identifier)
    {
      // DO NOT RESTORE
    }

    public override void RestoreOriginalModules()
    {
      // DO NOT RESTORE
    }
  }
}
