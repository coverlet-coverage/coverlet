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
using Microsoft.VSDiagnostics;

namespace coverlet.core.benchmark.tests
{
  /// <summary>
  /// Benchmarks that specifically target the performance of
  /// <c>SkipInlineAssignedAutoProperty</c> / <c>SkipGeneratedBackingFieldAssignment</c>
  /// inside <see cref="CecilSymbolHelper"/>, which are exercised by the
  /// auto-property and record types added to <c>coverlet.benchmark.subject</c>
  /// (issue-1633 / PR-1941).
  ///
  /// The suite isolates three dimensions that affect this code path:
  /// <list type="bullet">
  ///   <item><term>SkipAutoProps</term><description>
  ///     Toggles the guard condition — when <c>false</c> the entire scan is
  ///     bypassed, making it the ideal baseline to quantify the net cost of
  ///     the backing-field enumeration when <c>true</c>.
  ///   </description></item>
  ///   <item><term>RecordScenario</term><description>
  ///     Selects the filter applied to the benchmark subject so that only the
  ///     record/auto-prop types are included (high-density scenario) or the full
  ///     assembly (real-world baseline).
  ///   </description></item>
  /// </list>
  ///
  /// Run the suite in Release mode as described in HowTo.md:
  /// <code>
  ///   dotnet build test/coverlet.core.benchmark.tests -c release
  ///   cd artifacts/bin/coverlet.core.benchmark.tests/release_net10.0
  ///   ./coverlet.core.benchmark.tests.exe --filter "*AutoProps*"
  /// </code>
  /// </summary>
  [MemoryDiagnoser]
  [CPUUsageDiagnoser]
  public class AutoPropsBenchmarks
  {
    // -----------------------------------------------------------------------
    // Parameters
    // -----------------------------------------------------------------------

    /// <summary>
    /// When <c>false</c> the entire <c>SkipGeneratedBackingFieldAssignment</c> scan is
    /// bypassed by the early-return guard, giving a pure instrumentation baseline.
    /// When <c>true</c> the backing-field enumeration runs on every eligible instruction.
    /// </summary>
    [Params(false, true)]
    public bool SkipAutoProps { get; set; }

    /// <summary>
    /// Controls whether only the record/auto-prop types in the benchmark subject are
    /// targeted (<c>true</c>) or the full assembly (<c>false</c>).
    /// The focused filter maximises the ratio of backing-field-scan work relative to
    /// total instrumentation cost and exposes regressions in the inner loop more clearly.
    /// </summary>
    [Params(false, true)]
    public bool FocusRecordTypes { get; set; }

    // -----------------------------------------------------------------------
    // Infrastructure
    // -----------------------------------------------------------------------

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

    [GlobalSetup]
    public void Setup()
    {
      _testSubjectDllPath = Path.Combine(AppContext.BaseDirectory, "coverlet.benchmark.subject.dll");
      if (!File.Exists(_testSubjectDllPath))
      {
        throw new FileNotFoundException($"Test subject DLL not found: {_testSubjectDllPath}");
      }

      _workDir = Path.Combine(Path.GetTempPath(), "coverlet_bench_autoprops_" + Guid.NewGuid().ToString("N"));
      Directory.CreateDirectory(_workDir);
      _workDllPath = Path.Combine(_workDir, "coverlet.benchmark.subject.dll");
      _workPdbPath = Path.ChangeExtension(_workDllPath, ".pdb");

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
    }

    /// <summary>
    /// Restores the clean, un-instrumented DLL and PDB before each BDN iteration so
    /// <c>PrepareModules</c> always processes a fresh binary.
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
    // Benchmarks
    // -----------------------------------------------------------------------

    /// <summary>
    /// Measures <see cref="Coverage.PrepareModules"/> for the auto-property and record
    /// workload, with <see cref="SkipAutoProps"/> and <see cref="FocusRecordTypes"/> as
    /// parameters.
    ///
    /// <para>
    /// With <c>SkipAutoProps=true, FocusRecordTypes=true</c> this directly exercises the
    /// <c>SkipGeneratedBackingFieldAssignment</c> LINQ path on types with many
    /// <c>k__BackingField</c> entries (up to 25 per class, plus records).
    /// </para>
    ///
    /// <para>
    /// With <c>SkipAutoProps=false</c> the call is short-circuited, providing the baseline
    /// cost without the backing-field scan.
    /// </para>
    /// </summary>
    [Benchmark(Description = "AutoProps + Records - PrepareModules")]
    public void InstrumentAutoPropsAndRecords()
    {
      var parameters = BuildParameters();
      var coverage = new Coverage(
          _workDir,
          parameters,
          _logger,
          _instrumentationHelper,
          _fileSystem,
          _sourceRootTranslator,
          _cecilSymbolHelper);

      CoveragePrepareResult result = coverage.PrepareModules();

      if (result.Results.Length == 0)
      {
        throw new InvalidOperationException("Instrumentation returned no results — check subject DLL path and filters");
      }
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private CoverageParameters BuildParameters()
    {
      // When FocusRecordTypes=true only include the auto-prop and record types so that the
      // ratio of SkipGeneratedBackingFieldAssignment work is maximised. When false, the full
      // assembly is included to match the real-world scenario.
      string[] includeFilters = FocusRecordTypes
          ? [
              "[coverlet.benchmark.subject]coverlet.benchmark.subject.AutoProps*",
              "[coverlet.benchmark.subject]coverlet.benchmark.subject.Record*",
              "[coverlet.benchmark.subject]coverlet.benchmark.subject.AbstractShape*",
              "[coverlet.benchmark.subject]coverlet.benchmark.subject.Order*",
              "[coverlet.benchmark.subject]coverlet.benchmark.subject.Product*",
            ]
          : ["[coverlet.benchmark.subject]*"];

      return new CoverageParameters
      {
        IncludeFilters = includeFilters,
        IncludeDirectories = [_workDir],
        ExcludeFilters = [],
        ExcludedSourceFiles = [],
        ExcludeAttributes = [],
        IncludeTestAssembly = false,
        SingleHit = false,
        MergeWith = string.Empty,
        UseSourceLink = false,
        SkipAutoProps = SkipAutoProps,
        DeterministicReport = false,
        ExcludeAssembliesWithoutSources = "None",
      };
    }
  }
}
