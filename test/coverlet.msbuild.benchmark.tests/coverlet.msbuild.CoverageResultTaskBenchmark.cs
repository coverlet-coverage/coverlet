// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Coverlet.MSbuild.Tasks;

using Microsoft.Build.Framework;
using Moq;
using Microsoft.Build.Locator;
using Coverlet.Core.Abstractions;
using Coverlet.Core.Helpers;
using Microsoft.Build.Utilities;
using Microsoft.Extensions.DependencyInjection;
using BenchmarkDotNet.Attributes;
using Xunit;

namespace coverlet.msbuild.benchmark.tests
{
  public class MSBuildFixture
  {
    public MSBuildFixture()
    {
      MSBuildLocator.RegisterDefaults();
    }
  }

  [MemoryDiagnoser]
  public class CoverageResultTaskBenchmarks : IAssemblyFixture<MSBuildFixture>
  {
    private readonly Mock<IBuildEngine> _buildEngine;
    CoverageResultTask? _coverageResultTask;
    private readonly List<BuildErrorEventArgs> _errors;
    private readonly Mock<IAssemblyAdapter> _mockAssemblyAdapter;

    public CoverageResultTaskBenchmarks()
    {
      _buildEngine = new Mock<IBuildEngine>();
      _errors = new List<BuildErrorEventArgs>();
      _mockAssemblyAdapter = new Mock<IAssemblyAdapter>();
      _mockAssemblyAdapter.Setup(x => x.GetAssemblyName(It.IsAny<string>())).Returns("abc");
    }

    [GlobalSetup(Target = nameof(CoverageResultTaskBenchmark))]
    public void CoverageResultTaskSingleSetup()
    {
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

#pragma warning disable CS8604 // Possible null reference argument for parameter..
#pragma warning disable CS8602 // Dereference of a possibly null reference.
      var InstrumenterState = new TaskItem(Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, "TestAssets\\InstrumenterState.ItemSpec.data1.xml"));
#pragma warning restore CS8602 // Dereference of a possibly null reference.
#pragma warning restore C8S604 // Possible null reference argument for parameter.
      _coverageResultTask = new()
      {
        OutputFormat = "cobertura",
        Output = "coverageDir",
        Threshold = "50",
        ThresholdType = "total",
        ThresholdStat = "total",
        InstrumenterState = null
      };
      _coverageResultTask.BuildEngine = _buildEngine.Object;
    }

    //[GlobalSetup(Target = nameof(CoverageResultTaskBenchmarkInitialTargetsOuterAndInner))]
    //public void PreprocessorBenchmarkInitialTargetsOuterAndInnerSetup()
    //{

    //  _coverageResultTask = new()
    //  {
    //    OutputFormat = "cobertura",
    //    Output = "coverageDir",
    //    Threshold = "50",
    //    ThresholdType = "total",
    //    ThresholdStat = "total",
    //    InstrumenterState = null
    //  };
    //  _coverageResultTask.BuildEngine = _buildEngine.Object;
    //}

    [Benchmark]
    public void CoverageResultTaskBenchmark()
    {
      bool success = _coverageResultTask!.Execute();
    }

    //[Benchmark]
    //public void CoverageResultTaskBenchmarkInitialTargetsOuterAndInner()
    //{
    //  bool success = _coverageResultTask!.Execute();
    //}
  }
  class InstrumentationHelperForDebugging : InstrumentationHelper
  {
    public InstrumentationHelperForDebugging(IProcessExitHandler processExitHandler, IRetryHelper retryHelper, IFileSystem fileSystem, Coverlet.Core.Abstractions.ILogger logger, ISourceRootTranslator sourceTranslator)
        : base(processExitHandler, retryHelper, fileSystem, logger, sourceTranslator)
    {

    }
  }
}
