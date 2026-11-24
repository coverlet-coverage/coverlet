// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using BenchmarkDotNet.Attributes;
using Coverlet.Core;
using Coverlet.Core.Abstractions;
using Coverlet.Core.Instrumentation;
using Moq;

namespace coverlet.core.benchmark.tests
{
  /// <summary>
  /// Coverage calculation for one large module: many documents, each with methods whose statement
  /// ranges nest inside the method's own range. Finding the nested ranges compares every line range
  /// with the other ranges of its document.
  /// </summary>
  [MemoryDiagnoser]
  public class NestedRangesBenchmarks
  {
    private const int MethodsPerDocument = 25;
    private const int LinesPerMethod = 20;

    private CoveragePrepareResult _prepareResult;
    private byte[] _hits;
    private readonly Mock<ILogger> _logger = new();
    private readonly Mock<IInstrumentationHelper> _instrumentationHelper = new();
    private readonly Mock<ISourceRootTranslator> _sourceRootTranslator = new();
    private Mock<IFileSystem> _fileSystem;

    [Params(10, 40)]
    public int Documents { get; set; }

    [IterationSetup]
    public void Setup()
    {
      var result = new InstrumenterResult { HitsFilePath = "HitsFilePath", SourceLink = "SourceLink", ModulePath = "ModulePath" };
      for (int doc = 0; doc < Documents; doc++)
      {
        var document = new Document { Index = doc, Path = $"Path{doc}" };
        for (int method = 0; method < MethodsPerDocument; method++)
        {
          int first = method * LinesPerMethod + 1;
          int last = first + LinesPerMethod - 1;
          result.HitCandidates.Add(new HitCandidate(false, doc, first, last));
          for (int line = first + 1; line + 1 < last; line += 2)
          {
            result.HitCandidates.Add(new HitCandidate(false, doc, line, line + 1));
          }

          result.HitCandidates.Add(new HitCandidate(true, doc, first + 1, 0));
          for (int line = first; line <= last; line++)
          {
            document.Lines.Add(line, new Line { Class = "Class" + doc, Method = "Method" + method, Number = line });
          }
        }

        result.Documents.Add($"document{doc}", document);
      }

      _prepareResult = new CoveragePrepareResult { UseSourceLink = true, Results = new[] { result }, Parameters = new CoverageParameters() };

      // Every line range hit once; branches not hit, so they need no branch entries.
      using var stream = new MemoryStream();
      using var writer = new BinaryWriter(stream);
      writer.Write(result.HitCandidates.Count);
      foreach (HitCandidate candidate in result.HitCandidates)
      {
        writer.Write(candidate.isBranch ? 0 : 1);
      }

      writer.Flush();
      _hits = stream.ToArray();

      _fileSystem = new Mock<IFileSystem>();
      _fileSystem.Setup(x => x.Exists(It.IsAny<string>())).Returns(true);
      _fileSystem.Setup(x => x.NewFileStream(It.IsAny<string>(), FileMode.Open, FileAccess.Read)).Returns(() => new MemoryStream(_hits));
    }

    [Benchmark]
    public void GetCoverageResult() =>
        new Coverage(_prepareResult, _logger.Object, _instrumentationHelper.Object, _fileSystem.Object, _sourceRootTranslator.Object)
            .GetCoverageResult();
  }
}
