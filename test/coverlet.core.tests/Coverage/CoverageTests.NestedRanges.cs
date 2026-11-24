// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using Coverlet.Core.Abstractions;
using Coverlet.Core.Instrumentation;
using Coverlet.Tests.Utils;
using Moq;
using Xunit;

namespace Coverlet.Core.Tests
{
  public partial class CoverageTests
  {
    [Fact]
    public void CoverageResult_NestedRanges_AreSkippedOnlyWithinTheirOwnDocument()
    {
      var instrumenterResult = new InstrumenterResult
      {
        HitsFilePath = "HitsFilePath",
        SourceLink = "SourceLink",
        ModulePath = "ModulePath"
      };

      // Document 0: an outer range, a range nested in it, and a branch inside it. The nested range's
      // lines count its own hits, not the outer range's; a branch never takes lines from a range.
      instrumenterResult.HitCandidates.Add(new HitCandidate(false, 0, 1, 10));
      instrumenterResult.HitCandidates.Add(new HitCandidate(false, 0, 3, 4));
      instrumenterResult.HitCandidates.Add(new HitCandidate(true, 0, 6, 7));
      // Document 1: a range on lines that sit inside document 0's outer range. It must not take
      // lines away from document 0.
      instrumenterResult.HitCandidates.Add(new HitCandidate(false, 1, 2, 3));

      instrumenterResult.Documents.Add("document0", NewDocument(0, "Path0", 10));
      instrumenterResult.Documents.Add("document1", NewDocument(1, "Path1", 3));

      var coveragePrepareResult = new CoveragePrepareResult
      {
        UseSourceLink = true,
        Results = new[] { instrumenterResult },
        Parameters = new CoverageParameters()
      };

      // Hits per candidate, in candidate order. The branch has none, so it needs no branch entry.
      Stream memoryStream = HitsFile(1, 5, 0, 7);

      var fileSystemMock = new Mock<IFileSystem>();
      fileSystemMock.Setup(x => x.Exists(It.IsAny<string>())).Returns(true);
      fileSystemMock.Setup(x => x.NewFileStream(It.IsAny<string>(), FileMode.Open, FileAccess.Read))
          .Returns(memoryStream);

      var coverage = new Coverage(coveragePrepareResult, new Mock<ILogger>().Object, new Mock<IInstrumentationHelper>().Object,
          fileSystemMock.Object, new Mock<ISourceRootTranslator>().Object);

      CoverageResult coverageResult = coverage.GetCoverageResult();

      coverageResult.Document("document0").AssertLinesCovered(BuildConfiguration.Debug,
          (1, 1), (2, 1), (3, 5), (4, 5), (5, 1), (6, 1), (7, 1), (8, 1), (9, 1), (10, 1));
      coverageResult.Document("document1").AssertLinesCovered(BuildConfiguration.Debug, (2, 7), (3, 7));
    }

    private static Document NewDocument(int index, string path, int lines)
    {
      var document = new Document { Index = index, Path = path };
      for (int line = 1; line <= lines; line++)
      {
        document.Lines.Add(line, new Line { Class = "Class" + index, Hits = 0, Method = "Method" + index, Number = line });
      }

      return document;
    }

    private static Stream HitsFile(params int[] hits)
    {
      var memoryStream = new MemoryStream();
      var binaryWriter = new BinaryWriter(memoryStream);
      binaryWriter.Write(hits.Length);
      foreach (int hit in hits)
      {
        binaryWriter.Write(hit);
      }

      memoryStream.Position = 0;
      return memoryStream;
    }
  }
}
