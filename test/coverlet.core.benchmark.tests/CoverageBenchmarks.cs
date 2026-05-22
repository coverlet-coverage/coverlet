// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using BenchmarkDotNet.Attributes;
using Coverlet.Core;
using Coverlet.Core.Abstractions;
using Coverlet.Core.Helpers;
using Coverlet.Core.Symbols;
using Moq;

namespace coverlet.core.benchmark.tests
{
  [MemoryDiagnoser]
  public class CoverageBenchmarks
  {
    private Coverage _coverage;
    private readonly Mock<ILogger> _mockLogger = new();
    private DirectoryInfo _directory;

    [GlobalSetup(Target = nameof(GetCoverageBenchmark))]
    public void GetCoverageBenchmarkSetup()
    {
      string module = GetType().Assembly.Location;
      string pdb = Path.Combine(Path.GetDirectoryName(module), Path.GetFileNameWithoutExtension(module) + ".pdb");

      _directory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));

      File.Copy(module, Path.Combine(_directory.FullName, Path.GetFileName(module)), true);
      File.Copy(pdb, Path.Combine(_directory.FullName, Path.GetFileName(pdb)), true);

      // TODO: Find a way to mimick hits
      var instrumentationHelper =
          new InstrumentationHelper(new ProcessExitHandler(), new RetryHelper(), new FileSystem(), new Mock<ILogger>().Object,
                                    new SourceRootTranslator(module, new Mock<ILogger>().Object, new FileSystem(), new AssemblyAdapter()));

      var parameters = new CoverageParameters
      {
        IncludeFilters = new string[] { "[coverlet.tests.projectsample.excludedbyattribute*]*" },
        IncludeDirectories = Array.Empty<string>(),
        ExcludeFilters = Array.Empty<string>(),
        ExcludedSourceFiles = Array.Empty<string>(),
        ExcludeAttributes = Array.Empty<string>(),
        IncludeTestAssembly = false,
        SingleHit = false,
        MergeWith = string.Empty,
        UseSourceLink = false
      };

      _coverage = new Coverage(Path.Combine(_directory.FullName, Path.GetFileName(module)), parameters, _mockLogger.Object, instrumentationHelper, new FileSystem(), new SourceRootTranslator(_mockLogger.Object, new FileSystem()), new CecilSymbolHelper());
      _coverage.PrepareModules();

    }

    [GlobalCleanup]
    public void IterationCleanup()
    {
      _directory.Delete(true);
    }

    [Benchmark]
    public void GetCoverageBenchmark()
    {
      CoverageResult result = _coverage.GetCoverageResult();
    }
  }
}
