// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using BenchmarkDotNet.Attributes;
using Coverlet.Core;
using Coverlet.Core.Abstractions;
using Coverlet.Core.Helpers;
using Coverlet.Core.Symbols;
using Microsoft.Extensions.DependencyInjection;

namespace coverlet.core.benchmark.tests
{
  /// <summary>
  /// Benchmarks the instrumentation phase (<see cref="Coverage.PrepareModules"/>) using the
  /// <c>coverlet.benchmark.subject</c> assembly which contains several types.
  ///
  /// Service infrastructure is constructed once in <see cref="Setup"/> so that DI container
  /// creation cost is excluded from timing. The DLL is copied to a temp working directory
  /// and refreshed before each iteration so BenchmarkDotNet always instruments a clean binary.
  /// </summary>
  [MemoryDiagnoser]
  public class InstrumenterBenchmarks
  {
    private string _testSubjectDllPath;
    private string _testSubjectPdbPath;
    private string _workDir;
    private string _workDllPath;
    private string _workPdbPath;

    private Coverlet.Core.Abstractions.ILogger _logger;
    private IFileSystem _fileSystem;
    private IInstrumentationHelper _instrumentationHelper;
    private ISourceRootTranslator _sourceRootTranslator;
    private ICecilSymbolHelper _cecilSymbolHelper;
    private CoverageParameters _coverageParameters;

    [GlobalSetup]
    public void Setup()
    {
      // Source DLL (output copy placed next to this executable by the build)
      _testSubjectDllPath = Path.Combine(AppContext.BaseDirectory, "coverlet.benchmark.subject.dll");
      if (!File.Exists(_testSubjectDllPath))
      {
        throw new FileNotFoundException($"Test subject DLL not found: {_testSubjectDllPath}");
      }

      // Shared working directory; a fresh copy of the DLL is placed here per iteration
      _workDir = Path.Combine(Path.GetTempPath(), "coverlet_bench_" + Guid.NewGuid().ToString("N"));
      Directory.CreateDirectory(_workDir);
      _workDllPath = Path.Combine(_workDir, "coverlet.benchmark.subject.dll");
      _workPdbPath = Path.ChangeExtension(_workDllPath, ".pdb");

      // Copy both DLL and PDB so Mono.Cecil can match symbols.
      // Both are also required before BDN's internal JIT phase (runs the body once before [IterationSetup]).
      _testSubjectPdbPath = Path.ChangeExtension(_testSubjectDllPath, ".pdb");
      File.Copy(_testSubjectDllPath, _workDllPath, overwrite: true);
      if (File.Exists(_testSubjectPdbPath))
      {
        File.Copy(_testSubjectPdbPath, _workPdbPath, overwrite: true);
      }

      _logger = new ConsoleLogger();

      IServiceCollection services = new ServiceCollection();
      services.AddTransient<IRetryHelper, RetryHelper>();
      services.AddTransient<IProcessExitHandler, ProcessExitHandler>();
      services.AddTransient<IFileSystem, FileSystem>();
      services.AddTransient<Coverlet.Core.Abstractions.ILogger>(_ => _logger);
      services.AddSingleton<ICecilSymbolHelper, CecilSymbolHelper>();
      services.AddSingleton<IAssemblyAdapter, AssemblyAdapter>();
      services.AddSingleton<ISourceRootTranslator, SourceRootTranslator>(
          p => new SourceRootTranslator(
              "",
              p.GetRequiredService<Coverlet.Core.Abstractions.ILogger>(),
              p.GetRequiredService<IFileSystem>()));
      services.AddSingleton<IInstrumentationHelper>(p =>
          new InstrumentationHelper(
              p.GetRequiredService<IProcessExitHandler>(),
              p.GetRequiredService<IRetryHelper>(),
              p.GetRequiredService<IFileSystem>(),
              p.GetRequiredService<Coverlet.Core.Abstractions.ILogger>(),
              p.GetRequiredService<ISourceRootTranslator>()));

      var provider = services.BuildServiceProvider();
      _fileSystem = provider.GetRequiredService<IFileSystem>();
      _instrumentationHelper = provider.GetRequiredService<IInstrumentationHelper>();
      _sourceRootTranslator = provider.GetRequiredService<ISourceRootTranslator>();
      _cecilSymbolHelper = provider.GetRequiredService<ICecilSymbolHelper>();

      _coverageParameters = new CoverageParameters
      {
        IncludeFilters = ["[coverlet.benchmark.subject]*"],
        IncludeDirectories = [_workDir],
        ExcludeFilters = [],
        ExcludedSourceFiles = [],
        ExcludeAttributes = [],
        IncludeTestAssembly = false,
        SingleHit = false,
        MergeWith = string.Empty,
        UseSourceLink = false,
        SkipAutoProps = true,
        DeterministicReport = false,
        ExcludeAssembliesWithoutSources = "None",
      };
    }

    /// <summary>
    /// Restores both the un-instrumented DLL and its matching PDB before each BDN iteration.
    /// The PDB must be restored alongside the DLL because <c>Instrument()</c> writes a new PDB
    /// in place; leaving a stale instrumented PDB causes <c>SymbolsNotMatchingException</c> on
    /// subsequent iterations.
    /// </summary>
    [IterationSetup]
    public void IterationSetup()
    {
      File.Copy(_testSubjectDllPath, _workDllPath, overwrite: true);
      if (File.Exists(_testSubjectPdbPath))
      {
        File.Copy(_testSubjectPdbPath, _workPdbPath, overwrite: true);
      }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
      try { Directory.Delete(_workDir, recursive: true); }
      catch { /* best-effort */ }
    }

    /// <summary>
    /// Measures only the instrumentation phase (<see cref="Coverage.PrepareModules"/>).
    /// Service construction is excluded – it is performed once in <see cref="Setup"/>.
    /// </summary>
    [Benchmark]
    public void InstrumenterBenchmark()
    {
      var coverage = new Coverage(
          _workDir,
          _coverageParameters,
          _logger,
          _instrumentationHelper,
          _fileSystem,
          _sourceRootTranslator,
          _cecilSymbolHelper);

      CoveragePrepareResult result = coverage.PrepareModules();

      if (result.Results.Length == 0)
      {
        throw new InvalidOperationException("Instrumentation failed: PrepareModules returned no results");
      }
    }
  }
}
