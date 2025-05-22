// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using BenchmarkDotNet.Attributes;
using Coverlet.Core;
using Coverlet.Core.Abstractions;
using Coverlet.Core.Helpers;
using Coverlet.Core.Instrumentation;
using Coverlet.Core.Symbols;
using Microsoft.Extensions.DependencyInjection;

namespace coverlet.core.benchmark.tests
{
  public class InstrumenterBenchmarks
  {
    private Coverlet.Core.Abstractions.ILogger _logger;
    private IFileSystem _fileSystem;
    private Instrumenter _instrumenter;
    private Coverage _coverage;
    private CoverageParameters _coverageParameters;
    private CoveragePrepareResult _coveragePrepareResult;
    private ISourceRootTranslator _sourceRootTranslator;
    private CoverageParameters _parameters;
    private IInstrumentationHelper _instrumentationHelper;
    private ICecilSymbolHelper _cecilSymbolHelper;

    [GlobalCleanup]
    public void IterationCleanup()
    {

    }

    [Benchmark]
    public void InstrumenterBigClassBenchmark()
    {
      string bigClassFilePath = Path.Combine(Directory.GetCurrentDirectory(), "coverlet.testsubject.dll");
      string _coverletTestSubjectArtifactPath = Directory.GetCurrentDirectory();
      _logger = new ConsoleLogger();

      _coverageParameters = new CoverageParameters
      {
        Module = bigClassFilePath,
        IncludeFilters = ["[coverlet.testsubject]*"],
        IncludeDirectories = [_coverletTestSubjectArtifactPath],
        ExcludeFilters = null,
        ExcludedSourceFiles = null,
        ExcludeAttributes = null,
        IncludeTestAssembly = true,
        SingleHit = false,
        MergeWith = string.Empty,
        UseSourceLink = false,
        SkipAutoProps = true,
        DeterministicReport = false,
        ExcludeAssembliesWithoutSources = "None",
      };

      // Set up service collection like in InstrumentationTask
      IServiceCollection serviceCollection = new ServiceCollection();
      // These can stay transient
      serviceCollection.AddTransient<IRetryHelper, RetryHelper>();
      serviceCollection.AddTransient<IProcessExitHandler, ProcessExitHandler>();
      serviceCollection.AddTransient<IFileSystem, FileSystem>();
      serviceCollection.AddTransient<Coverlet.Core.Abstractions.ILogger>(_ => _logger);

      // Make all symbol and instrumentation related services singletons
      serviceCollection.AddSingleton<ICecilSymbolHelper, CecilSymbolHelper>();
      serviceCollection.AddSingleton<IAssemblyAdapter, AssemblyAdapter>();
      serviceCollection.AddSingleton<ISourceRootTranslator, SourceRootTranslator>(provider => new SourceRootTranslator("", provider.GetRequiredService<Coverlet.Core.Abstractions.ILogger>(), provider.GetRequiredService<IFileSystem>()));

      serviceCollection.AddSingleton<IInstrumentationHelper>(serviceProvider =>
          new InstrumentationHelper(
              serviceProvider.GetRequiredService<IProcessExitHandler>(),
              serviceProvider.GetRequiredService<IRetryHelper>(),
              serviceProvider.GetRequiredService<IFileSystem>(),
              serviceProvider.GetRequiredService<Coverlet.Core.Abstractions.ILogger>(),
              serviceProvider.GetRequiredService<ISourceRootTranslator>()));

      var serviceProvider = serviceCollection.BuildServiceProvider();

      // Initialize helpers using the service provider
      _fileSystem = serviceProvider.GetRequiredService<IFileSystem>();
      _instrumentationHelper = serviceProvider.GetRequiredService<IInstrumentationHelper>();
      _sourceRootTranslator = serviceProvider.GetRequiredService<ISourceRootTranslator>();
      _cecilSymbolHelper = serviceProvider.GetRequiredService<ICecilSymbolHelper>();

      _sourceRootTranslator = new SourceRootTranslator(_logger, new FileSystem());
      _parameters = new CoverageParameters();
      _instrumentationHelper =
          new InstrumentationHelper(new ProcessExitHandler(), new RetryHelper(), _fileSystem, _logger, _sourceRootTranslator);
      _instrumenter = new Instrumenter(bigClassFilePath, "_coverlet_instrumented", _parameters, _logger, _instrumentationHelper, _fileSystem, _sourceRootTranslator, new CecilSymbolHelper());

      _coverage = new Coverage(
          bigClassFilePath,
          _coverageParameters,
          _logger,
          _instrumentationHelper,
          _fileSystem,
          _sourceRootTranslator,
          _cecilSymbolHelper
          );

      // Prepare modules for instrumentation
      _coveragePrepareResult = _coverage.PrepareModules();

      if (_coveragePrepareResult.Results.Length == 0)
      {
        throw (new InvalidOperationException("Instrumentation failed: _coveragePrepareResult.Results missing"));
      }

    }
  }
}
