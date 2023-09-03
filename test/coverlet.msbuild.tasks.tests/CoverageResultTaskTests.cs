// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Coverlet.Core.Abstractions;
using Coverlet.Core.Helpers;
using Coverlet.MSbuild.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Build.Framework;
using Microsoft.Build.Locator;
using Microsoft.Build.Utilities;
using Moq;
using Xunit;

namespace coverlet.msbuild.tasks.tests
{

  public class MSBuildFixture
  {
    public MSBuildFixture()
    {
      MSBuildLocator.RegisterDefaults();
    }
  }
  public class CoverageResultTaskTests : IAssemblyFixture<MSBuildFixture>
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
        OutputFormat = "opencover",
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
      Assert.False(success);
      Assert.True(coverageResultTask.Log.HasLoggedErrors);
      // check error message "Result of instrumentation task not found"
    }

    [Fact]
    public void Execute_StateUnderTest_WithInstrumentationState_Fake()
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
      serviceCollection.AddTransient<IConsole, SystemConsole>();
      serviceCollection.AddSingleton<IInstrumentationHelper, InstrumentationHelperForDebugging>();
      serviceCollection.AddSingleton<ISourceRootTranslator, SourceRootTranslator>(serviceProvider => new SourceRootTranslator("empty", serviceProvider.GetRequiredService<Coverlet.Core.Abstractions.ILogger>(), mockFileSystem.Object, _mockAssemblyAdapter.Object));
      ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();
      BaseTask.ServiceProvider = serviceProvider;
      _buildEngine.Setup(x => x.LogErrorEvent(It.IsAny<BuildErrorEventArgs>())).Callback<BuildErrorEventArgs>(e => _errors.Add(e));

#pragma warning disable CS8604 // Possible null reference argument for parameter..
#pragma warning disable CS8602 // Dereference of a possibly null reference.
      var InstrumenterState = new TaskItem(Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, "TestAssets\\InstrumenterState.ItemSpec.data1.xml")) ;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
#pragma warning restore C8S604 // Possible null reference argument for parameter.

      var coverageResultTask = new CoverageResultTask
      {
        OutputFormat = "opencover",
        Output = "coverageDir",
        Threshold = "50",
        ThresholdType = "total",
        ThresholdStat = "total",
        InstrumenterState = InstrumenterState
      };
      coverageResultTask.BuildEngine = _buildEngine.Object;

      // Act
      bool success = coverageResultTask.Execute();

      // Assert
      Assert.True(success);
      Assert.False(coverageResultTask.Log.HasLoggedErrors);

      Assert.Contains("coverageDir.opencover.xml", coverageResultTask.ReportItems[0].ItemSpec);
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
