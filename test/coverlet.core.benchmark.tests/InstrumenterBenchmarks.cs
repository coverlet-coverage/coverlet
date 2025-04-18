// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using BenchmarkDotNet.Attributes;
using Coverlet.Core;
using Coverlet.Core.Abstractions;
using Coverlet.Core.Helpers;
using Coverlet.Core.Instrumentation;
using Coverlet.Core.Symbols;
using Moq;

namespace coverlet.core.benchmark.tests
{
  public class InstrumenterBenchmarks
  {
    Mock<ILogger> _mockLogger;
    Mock<FileSystem> _partialMockFileSystem;
    readonly string[] _files = new[]
    {
                "System.Private.CoreLib.dll",
                "System.Private.CoreLib.pdb"
            };
    Instrumenter _instrumenter;
    DirectoryInfo _directory;
    SourceRootTranslator _sourceRootTranslator;
    CoverageParameters _parameters;
    InstrumentationHelper _instrumentationHelper;

    [GlobalCleanup]
    public void IterationCleanup()
    {
      _directory.Delete(true);
    }

    [Benchmark]
    public void InstrumenterBenchmark()
    {
      _mockLogger = new Mock<ILogger>();
      _directory = Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), nameof(InstrumenterBenchmark)));

      foreach (string file in _files)
      {
        File.Copy(Path.Combine(Directory.GetCurrentDirectory(), "TestAssets", file), Path.Combine(_directory.FullName, file), overwrite: true);
      }

      _partialMockFileSystem = new Mock<FileSystem>();
      _partialMockFileSystem.CallBase = true;
      _partialMockFileSystem.Setup(fs => fs.OpenRead(It.IsAny<string>())).Returns((string path) =>
      {
        if (Path.GetFileName(path.Replace(@"\", @"/")) == _files[1])
        {
          return File.OpenRead(Path.Combine(Path.Combine(Directory.GetCurrentDirectory(), "TestAssets"), _files[1]));
        }
        else
        {
          return File.OpenRead(path);
        }
      });
      _partialMockFileSystem.Setup(fs => fs.Exists(It.IsAny<string>())).Returns((string path) =>
      {
        if (Path.GetFileName(path.Replace(@"\", @"/")) == _files[1])
        {
          return File.Exists(Path.Combine(Path.Combine(Directory.GetCurrentDirectory(), "TestAssets"), _files[1]));
        }
        else
        {
          if (path.Contains(@":\git\runtime"))
          {
            return true;
          }
          else
          {
            return File.Exists(path);
          }
        }
      });
      _sourceRootTranslator = new SourceRootTranslator(_mockLogger.Object, new FileSystem());
      _parameters = new CoverageParameters();
      _instrumentationHelper =
          new InstrumentationHelper(new ProcessExitHandler(), new RetryHelper(), _partialMockFileSystem.Object, _mockLogger.Object, _sourceRootTranslator);
      _instrumenter = new Instrumenter(Path.Combine(_directory.FullName, _files[0]), "_coverlet_instrumented", _parameters, _mockLogger.Object, _instrumentationHelper, _partialMockFileSystem.Object, _sourceRootTranslator, new CecilSymbolHelper());

      // implement your benchmark here
      InstrumenterResult result = _instrumenter.Instrument();
    }

  }
}
