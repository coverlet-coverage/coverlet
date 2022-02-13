
using Xunit;
using Coverlet.Core.Instrumentation;
using System.Collections.Generic;
using System.Linq;

namespace Coverlet.Core.Instrumentation.Tests
{
    public class InstrumenterResultTests
    {
        [Fact]
        public void TestEnsureDocumentsPropertyNotNull()
        {
            InstrumenterResult result = new InstrumenterResult();
            Assert.NotNull(result.Documents);
        }

        [Fact]
        public void TestEnsureLinesAndBranchesPropertyNotNull()
        {
            Document document = new Document();
            Assert.NotNull(document.Lines);
            Assert.NotNull(document.Branches);
        }

        [Fact]
        public void CoveragePrepareResult_SerializationRoundTrip()
        {
            CoveragePrepareResult cpr = new CoveragePrepareResult();
            cpr.Identifier = "Identifier";
            cpr.MergeWith = "MergeWith";
            cpr.ModuleOrDirectory = "Module";
            cpr.UseSourceLink = true;

            InstrumenterResult ir = new InstrumenterResult();
            ir.HitsFilePath = "HitsFilePath";
            ir.Module = "Module";
            ir.ModulePath = "ModulePath";
            ir.SourceLink = "SourceLink";

            ir.HitCandidates.Add(new HitCandidate(true, 1, 2, 3));
            ir.HitCandidates.Add(new HitCandidate(false, 4, 5, 6));

            var doc = new Document()
            {
                Index = 0,
                Path = "Path0"
            };
            doc.Lines.Add(0, new Line()
            {
                Class = "Class0",
                Hits = 0,
                Method = "Method0",
                Number = 0
            });
            doc.Branches.Add(new BranchKey(0, 0),
            new Branch()
            {
                Class = "Class0",
                EndOffset = 0,
                Hits = 0,
                Method = "Method",
                Number = 0,
                Offset = 0,
                Ordinal = 0,
                Path = 0
            });

            var doc2 = new Document()
            {
                Index = 1,
                Path = "Path1"
            };
            doc2.Lines.Add(1, new Line()
            {
                Class = "Class1",
                Hits = 1,
                Method = "Method1",
                Number = 1
            });
            doc2.Branches.Add(new BranchKey(1, 1),
            new Branch()
            {
                Class = "Class1",
                EndOffset = 1,
                Hits = 1,
                Method = "Method1",
                Number = 1,
                Offset = 1,
                Ordinal = 1,
                Path = 1
            });

            ir.Documents.Add("key", doc);
            ir.Documents.Add("key2", doc2);
            cpr.Results = new InstrumenterResult[] { ir };

            CoveragePrepareResult roundTrip = CoveragePrepareResult.Deserialize(CoveragePrepareResult.Serialize(cpr));

            Assert.Equal(cpr.Identifier, roundTrip.Identifier);
            Assert.Equal(cpr.MergeWith, roundTrip.MergeWith);
            Assert.Equal(cpr.ModuleOrDirectory, roundTrip.ModuleOrDirectory);
            Assert.Equal(cpr.UseSourceLink, roundTrip.UseSourceLink);

            for (int i = 0; i < cpr.Results.Length; i++)
            {
                Assert.Equal(cpr.Results[i].HitsFilePath, roundTrip.Results[i].HitsFilePath);
                Assert.Equal(cpr.Results[i].Module, roundTrip.Results[i].Module);
                Assert.Equal(cpr.Results[i].ModulePath, roundTrip.Results[i].ModulePath);
                Assert.Equal(cpr.Results[i].SourceLink, roundTrip.Results[i].SourceLink);

                for (int k = 0; k < cpr.Results[i].HitCandidates.Count; k++)
                {
                    Assert.Equal(cpr.Results[i].HitCandidates[k].start, roundTrip.Results[i].HitCandidates[k].start);
                    Assert.Equal(cpr.Results[i].HitCandidates[k].isBranch, roundTrip.Results[i].HitCandidates[k].isBranch);
                    Assert.Equal(cpr.Results[i].HitCandidates[k].end, roundTrip.Results[i].HitCandidates[k].end);
                    Assert.Equal(cpr.Results[i].HitCandidates[k].docIndex, roundTrip.Results[i].HitCandidates[k].docIndex);
                }

                for (int k = 0; k < cpr.Results[i].Documents.Count; k++)
                {
                    var documents = cpr.Results[i].Documents.ToArray();
                    var documentsRoundTrip = roundTrip.Results[i].Documents.ToArray();
                    for (int j = 0; j < documents.Length; j++)
                    {
                        Assert.Equal(documents[j].Key, documentsRoundTrip[j].Key);
                        Assert.Equal(documents[j].Value.Index, documentsRoundTrip[j].Value.Index);
                        Assert.Equal(documents[j].Value.Path, documentsRoundTrip[j].Value.Path);

                        for (int v = 0; v < documents[j].Value.Lines.Count; v++)
                        {
                            var lines = documents[j].Value.Lines.ToArray();
                            var linesRoundTrip = documentsRoundTrip[j].Value.Lines.ToArray();

                            Assert.Equal(lines[v].Key, linesRoundTrip[v].Key);
                            Assert.Equal(lines[v].Value.Class, lines[v].Value.Class);
                            Assert.Equal(lines[v].Value.Hits, lines[v].Value.Hits);
                            Assert.Equal(lines[v].Value.Method, lines[v].Value.Method);
                            Assert.Equal(lines[v].Value.Number, lines[v].Value.Number);
                        }

                        for (int v = 0; v < documents[j].Value.Branches.Count; v++)
                        {
                            var branches = documents[j].Value.Branches.ToArray();
                            var branchesRoundTrip = documentsRoundTrip[j].Value.Branches.ToArray();

                            Assert.Equal(branches[v].Key, branchesRoundTrip[v].Key);
                            Assert.Equal(branches[v].Value.Class, branchesRoundTrip[v].Value.Class);
                            Assert.Equal(branches[v].Value.EndOffset, branchesRoundTrip[v].Value.EndOffset);
                            Assert.Equal(branches[v].Value.Hits, branchesRoundTrip[v].Value.Hits);
                            Assert.Equal(branches[v].Value.Method, branchesRoundTrip[v].Value.Method);
                            Assert.Equal(branches[v].Value.Number, branchesRoundTrip[v].Value.Number);
                            Assert.Equal(branches[v].Value.Offset, branchesRoundTrip[v].Value.Offset);
                            Assert.Equal(branches[v].Value.Ordinal, branchesRoundTrip[v].Value.Ordinal);
                            Assert.Equal(branches[v].Value.Path, branchesRoundTrip[v].Value.Path);
                        }
                    }
                }
            }
        }
    }
}