using System.IO;
using Coverlet.Core.Abstractions;
using Coverlet.Core.Instrumentation;
using Moq;
using Xunit;

namespace Coverlet.Core.Tests
{
    public partial class CoverageTests
    {
        [Fact]
        public void CoverageResult_NegativeLineCoverage_TranslatedToMaxValueOfInt32()
        {
            InstrumenterResult instrumenterResult = new InstrumenterResult
            {
                HitsFilePath = "HitsFilePath", 
                SourceLink = "SourceLink", 
                ModulePath = "ModulePath"
            };

            instrumenterResult.HitCandidates.Add(new HitCandidate(false, 0, 1, 1));

            var document = new Document
            {
                Index = 0,
                Path = "Path0"
            };

            document.Lines.Add(1, new Line
            {
                Class = "Class0",
                Hits = 0,
                Method = "Method0",
                Number = 1
            });

            instrumenterResult.Documents.Add("document", document);

            CoveragePrepareResult coveragePrepareResult = new CoveragePrepareResult
            {
                UseSourceLink = true, 
                Results = new[] {instrumenterResult}, 
                Parameters = new CoverageParameters()
            };

            Stream memoryStream = new MemoryStream();
            BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
            binaryWriter.Write(1);
            binaryWriter.Write(-1);
            memoryStream.Position = 0;

            var fileSystemMock = new Mock<IFileSystem>();
            fileSystemMock.Setup(x => x.Exists(It.IsAny<string>())).Returns(true);
            fileSystemMock.Setup(x => x.NewFileStream(It.IsAny<string>(), FileMode.Open, FileAccess.Read))
                .Returns(memoryStream);

            var coverage = new Coverage(coveragePrepareResult, new Mock<ILogger>().Object, new Mock<IInstrumentationHelper>().Object,
                fileSystemMock.Object, new Mock<ISourceRootTranslator>().Object);

            var coverageResult = coverage.GetCoverageResult();
            coverageResult.Document("document").AssertLinesCovered(BuildConfiguration.Debug, (1, int.MaxValue));

        }
    }
}
