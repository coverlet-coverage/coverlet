// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using BenchmarkDotNet.Attributes;
using Coverlet.Core;
using Coverlet.Core.Abstractions;
using Coverlet.Core.Helpers;
using Coverlet.Core.Reporters;
using Coverlet.Core.Symbols;
using Microsoft.Extensions.DependencyInjection;

namespace coverlet.core.benchmark.tests
{
  /// <summary>
  /// Benchmarks that vary individual <see cref="CoverageParameters"/> options so that the
  /// performance impact of each option can be measured and compared independently.
  ///
  /// Run the suite in Release mode as described in HowTo.md:
  ///   dotnet build test/coverlet.core.benchmark.tests -c release
  ///   cd artifacts/bin/coverlet.core.benchmark.tests/release_net10.0
  ///   ./coverlet.core.benchmark.tests.exe
  /// </summary>
  [MemoryDiagnoser]
  public class InstrumentationOptionsBenchmarks
  {
    // -----------------------------------------------------------------------
    // Parameterised option values
    // -----------------------------------------------------------------------

    /// <summary>SingleHit=true eliminates the Interlocked.Increment call per line and replaces it
    /// with a simple boolean flag write, potentially reducing instrumented code size.</summary>
    [Params(false, true)]
    public bool SingleHit { get; set; }

    /// <summary>SkipAutoProps=true skips instrumentation of compiler-generated auto-property
    /// accessors, reducing the number of IL injection points.</summary>
    [Params(false, true)]
    public bool SkipAutoProps { get; set; }

    /// <summary>IncludeTestAssembly controls whether the test host assembly itself is also
    /// instrumented, which doubles the instrumentation work in a self-hosted benchmark.</summary>
    [Params(false, true)]
    public bool IncludeTestAssembly { get; set; }

    // -----------------------------------------------------------------------
    // Infrastructure shared across all iterations
    // -----------------------------------------------------------------------

    private string _testSubjectDllPath;
    private string _testSubjectPdbPath;
    private string _artifactPath;
    private string _workDir;
    private string _workDllPath;
    private string _workPdbPath;
    private IInstrumentationHelper _instrumentationHelper;
    private ICecilSymbolHelper _cecilSymbolHelper;
    private ISourceRootTranslator _sourceRootTranslator;
    private IFileSystem _fileSystem;
    private Coverlet.Core.Abstractions.ILogger _logger;

    [GlobalSetup]
    public void Setup()
    {
      _logger = new ConsoleLogger();
      _artifactPath = AppContext.BaseDirectory;
      _testSubjectDllPath = Path.Combine(_artifactPath, "coverlet.testsubject.dll");

      if (!File.Exists(_testSubjectDllPath))
      {
        throw new FileNotFoundException($"Test subject DLL not found: {_testSubjectDllPath}");
      }

      // Isolated working directory: the benchmark copies the clean DLL here before each
      // iteration so that PrepareModules always instruments an un-modified binary.
      _workDir = Path.Combine(Path.GetTempPath(), "coverlet_bench_opts_" + Guid.NewGuid().ToString("N"));
      Directory.CreateDirectory(_workDir);
      _workDllPath = Path.Combine(_workDir, "coverlet.testsubject.dll");
      _workPdbPath = Path.ChangeExtension(_workDllPath, ".pdb");

      // Copy both DLL and PDB so Mono.Cecil can match symbols.
      // Both are also required before BDN's internal JIT phase (runs the body once before [IterationSetup]).
      _testSubjectPdbPath = Path.ChangeExtension(_testSubjectDllPath, ".pdb");
      File.Copy(_testSubjectDllPath, _workDllPath, overwrite: true);
      if (File.Exists(_testSubjectPdbPath))
      {
        File.Copy(_testSubjectPdbPath, _workPdbPath, overwrite: true);
      }

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

    // -----------------------------------------------------------------------
    // Benchmark: PrepareModules (instrumentation phase)
    // -----------------------------------------------------------------------

    /// <summary>
    /// Measures the instrumentation (PrepareModules) duration for each combination of
    /// <see cref="SingleHit"/>, <see cref="SkipAutoProps"/> and <see cref="IncludeTestAssembly"/>.
    /// A fresh un-instrumented DLL copy is used each iteration (<see cref="IterationSetup"/>).
    /// </summary>
    [Benchmark(Description = "Instrumentation - PrepareModules")]
    public void InstrumentWithOptions()
    {
      var parameters = BuildParameters();
      var coverage = BuildCoverage(parameters);
      _ = coverage.PrepareModules();
    }

    // -----------------------------------------------------------------------
    // Helper methods
    // -----------------------------------------------------------------------

    private CoverageParameters BuildParameters() => new()
    {
      IncludeFilters = ["[coverlet.testsubject]*"],
      IncludeDirectories = [_workDir],
      ExcludeFilters = [],
      ExcludedSourceFiles = [],
      ExcludeAttributes = [],
      IncludeTestAssembly = IncludeTestAssembly,
      SingleHit = SingleHit,
      MergeWith = string.Empty,
      UseSourceLink = false,
      SkipAutoProps = SkipAutoProps,
      DeterministicReport = false,
      ExcludeAssembliesWithoutSources = "None",
    };

    private Coverage BuildCoverage(CoverageParameters parameters) =>
        new(
            _workDir,
            parameters,
            _logger,
            _instrumentationHelper,
            _fileSystem,
            _sourceRootTranslator,
            _cecilSymbolHelper);
  }

  /// <summary>
  /// Benchmarks that measure <see cref="Coverage.GetCoverageResult"/> for each supported
  /// report format.  The assembly is instrumented once in the global setup and then
  /// only the report-generation phase is timed per iteration.
  ///
  /// Supported formats: json, lcov, opencover, cobertura, teamcity
  /// </summary>
  [MemoryDiagnoser]
  public class ReportFormatBenchmarks
  {
    /// <summary>Coverage report format passed to <see cref="ReporterFactory"/>.</summary>
    [Params("json", "lcov", "opencover", "cobertura", "teamcity")]
    public string ReportFormat { get; set; }

    private Coverage _coverage;
    private ISourceRootTranslator _sourceRootTranslator;
    private DirectoryInfo _workDir;

    [GlobalSetup]
    public void Setup()
    {
      string module = GetType().Assembly.Location;
      string pdb = Path.Combine(
          Path.GetDirectoryName(module)!,
          Path.GetFileNameWithoutExtension(module) + ".pdb");

      _workDir = Directory.CreateDirectory(
          Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));

      string destModule = Path.Combine(_workDir.FullName, Path.GetFileName(module));
      File.Copy(module, destModule, overwrite: true);
      File.Copy(pdb, Path.Combine(_workDir.FullName, Path.GetFileName(pdb)), overwrite: true);

      var logger = new ConsoleLogger();
      var fileSystem = new FileSystem();

      var instrumentationHelper = new InstrumentationHelper(
          new ProcessExitHandler(),
          new RetryHelper(),
          fileSystem,
          logger,
          new SourceRootTranslator(module, logger, fileSystem, new AssemblyAdapter()));

      _sourceRootTranslator = new SourceRootTranslator(logger, fileSystem);

      var parameters = new CoverageParameters
      {
        IncludeFilters = ["[coverlet.tests.projectsample.excludedbyattribute*]*"],
        IncludeDirectories = [],
        ExcludeFilters = [],
        ExcludedSourceFiles = [],
        ExcludeAttributes = [],
        IncludeTestAssembly = false,
        SingleHit = false,
        MergeWith = string.Empty,
        UseSourceLink = false,
        SkipAutoProps = false,
        DeterministicReport = false,
        ExcludeAssembliesWithoutSources = "None",
      };

      _coverage = new Coverage(
          destModule,
          parameters,
          logger,
          instrumentationHelper,
          fileSystem,
          _sourceRootTranslator,
          new CecilSymbolHelper());

      _coverage.PrepareModules();
    }

    [GlobalCleanup]
    public void Cleanup() => _workDir?.Delete(recursive: true);

    /// <summary>
    /// Measures the end-to-end <c>GetCoverageResult</c> plus serialisation via a reporter.
    /// </summary>
    [Benchmark(Description = "GetCoverageResult + Report")]
    public string GetCoverageAndReport()
    {
      CoverageResult result = _coverage.GetCoverageResult();

      IReporter reporter = new ReporterFactory(ReportFormat).CreateReporter()
          ?? throw new InvalidOperationException($"Unknown report format: {ReportFormat}");

      return reporter.Report(result, _sourceRootTranslator);
    }
  }

  // DeterministicAndSourceLinkBenchmarks removed: UseSourceLink only has an effect when the
  // assembly contains an embedded source-link JSON blob. coverlet.testsubject does not, so all
  // four flag combinations measure identical paths and produce results within noise range.
  // Reintroduce this class once a source-link-enabled test subject is available.

  // ExcludeAssembliesHeuristicBenchmarks removed: with a single small assembly the heuristic
  // scans zero additional files regardless of the MissingAll/MissingAny/None setting. The
  // measured 20–45 ms range reflects lock/GC variance, not the heuristic. Reintroduce when
  // benchmarking against a directory containing multiple assemblies without PDBs.
}
