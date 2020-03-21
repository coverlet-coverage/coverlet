using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Xunit;

namespace Coverlet.Core.Reporters.Tests
{
    public class OpenCoverReporterTests
    {
        [Fact]
        public void TestReport()
        {
            CoverageResult result = new CoverageResult();
            result.Identifier = Guid.NewGuid().ToString();

            result.Modules = new Modules();
            result.Modules.Add("Coverlet.Core.Reporters.Tests", CreateFirstDocuments());

            OpenCoverReporter reporter = new OpenCoverReporter();
            string report = reporter.Report(result);
            Assert.NotEmpty(report);
            XDocument doc = XDocument.Load(new MemoryStream(Encoding.UTF8.GetBytes(report)));
            Assert.Empty(doc.Descendants().Attributes("sequenceCoverage").Where(v => v.Value != "33.33"));
            Assert.Empty(doc.Descendants().Attributes("branchCoverage").Where(v => v.Value != "25"));
        }

        [Fact]
        public void TestFilesHaveUniqueIdsOverMultipleModules()
        {
            CoverageResult result = new CoverageResult();
            result.Identifier = Guid.NewGuid().ToString();

            result.Modules = new Modules();
            result.Modules.Add("Coverlet.Core.Reporters.Tests", CreateFirstDocuments());
            result.Modules.Add("Some.Other.Module", CreateSecondDocuments());

            OpenCoverReporter reporter = new OpenCoverReporter();
            var xml = reporter.Report(result);
            Assert.NotEqual(string.Empty, xml);

            Assert.Contains(@"<FileRef uid=""1"" />", xml);
            Assert.Contains(@"<FileRef uid=""2"" />", xml);
        }

        [Fact]
        public void TestLineBranchCoverage()
        {
            var result = new CoverageResult
            {
                Identifier = Guid.NewGuid().ToString(),
                Modules = new Modules { {"Coverlet.Core.Reporters.Tests", CreateBranchCoverageDocuments()} }
            };
            
            var xml = new OpenCoverReporter().Report(result);
            
            // Line 1: Two branches, no coverage (bec = 2, bev = 0)
            Assert.Contains(@"<SequencePoint vc=""1"" uspid=""1"" ordinal=""0"" sl=""1"" sc=""1"" el=""1"" ec=""2"" bec=""2"" bev=""0"" fileid=""1"" />", xml);
            
            // Line 2: Two branches, one covered (bec = 2, bev = 1)
            Assert.Contains(@"<SequencePoint vc=""1"" uspid=""2"" ordinal=""1"" sl=""2"" sc=""1"" el=""2"" ec=""2"" bec=""2"" bev=""1"" fileid=""1"" />", xml);
            
            // Line 3: Two branches, all covered (bec = 2, bev = 2)
            Assert.Contains(@"<SequencePoint vc=""1"" uspid=""3"" ordinal=""2"" sl=""3"" sc=""1"" el=""3"" ec=""2"" bec=""2"" bev=""2"" fileid=""1"" />", xml);
            
            // Line 4: Three branches, two covered (bec = 3, bev = 2)
            Assert.Contains(@"<SequencePoint vc=""1"" uspid=""4"" ordinal=""3"" sl=""4"" sc=""1"" el=""4"" ec=""2"" bec=""3"" bev=""2"" fileid=""1"" />", xml);
        }

        private static Documents CreateFirstDocuments()
        {
            Lines lines = new Lines();
            lines.Add(1, 1);
            lines.Add(2, 0);
            lines.Add(3, 0);

            Branches branches = new Branches();
            branches.Add(new BranchInfo { Line = 1, Hits = 1, Offset = 23, EndOffset = 24, Path = 0, Ordinal = 1 });
            branches.Add(new BranchInfo { Line = 1, Hits = 0, Offset = 23, EndOffset = 27, Path = 1, Ordinal = 2 });
            branches.Add(new BranchInfo { Line = 1, Hits = 0, Offset = 40, EndOffset = 41, Path = 0, Ordinal = 3 });
            branches.Add(new BranchInfo { Line = 1, Hits = 0, Offset = 40, EndOffset = 44, Path = 1, Ordinal = 4 });

            Methods methods = new Methods();
            var methodString = "System.Void Coverlet.Core.Reporters.Tests.OpenCoverReporterTests.TestReport()";
            methods.Add(methodString, new Method());
            methods[methodString].Lines = lines;
            methods[methodString].Branches = branches;

            Classes classes = new Classes();
            classes.Add("Coverlet.Core.Reporters.Tests.OpenCoverReporterTests", methods);

            Documents documents = new Documents();
            documents.Add("doc.cs", classes);

            return documents;
        }

        private static Documents CreateSecondDocuments()
        {
            Lines lines = new Lines();
            lines.Add(1, 1);
            lines.Add(2, 0);

            Methods methods = new Methods();
            var methodString = "System.Void Some.Other.Module.TestClass.TestMethod()";
            methods.Add(methodString, new Method());
            methods[methodString].Lines = lines;

            Classes classes2 = new Classes();
            classes2.Add("Some.Other.Module.TestClass", methods);

            var documents = new Documents();
            documents.Add("TestClass.cs", classes2);

            return documents;
        }

        private static Documents CreateBranchCoverageDocuments()
        {
            var lines = new Lines
            {
                {1, 1}, 
                {2, 1}, 
                {3, 1},
                {4, 1},
            };

            var branches = new Branches
            {
                // Two branches, no coverage
                new BranchInfo {Line = 1, Hits = 0, Offset = 23, EndOffset = 24, Path = 0, Ordinal = 1},
                new BranchInfo {Line = 1, Hits = 0, Offset = 23, EndOffset = 27, Path = 1, Ordinal = 2},
                
                // Two branches, one covered
                new BranchInfo {Line = 2, Hits = 1, Offset = 40, EndOffset = 41, Path = 0, Ordinal = 3},
                new BranchInfo {Line = 2, Hits = 0, Offset = 40, EndOffset = 44, Path = 1, Ordinal = 4},
                
                // Two branches, all covered
                new BranchInfo {Line = 3, Hits = 1, Offset = 40, EndOffset = 41, Path = 0, Ordinal = 3},
                new BranchInfo {Line = 3, Hits = 3, Offset = 40, EndOffset = 44, Path = 1, Ordinal = 4},
                
                // Three branches, two covered
                new BranchInfo {Line = 4, Hits = 5, Offset = 40, EndOffset = 44, Path = 1, Ordinal = 4},
                new BranchInfo {Line = 4, Hits = 2, Offset = 40, EndOffset = 44, Path = 1, Ordinal = 4},
                new BranchInfo {Line = 4, Hits = 0, Offset = 40, EndOffset = 44, Path = 1, Ordinal = 4}
            };
            
            const string methodString = "System.Void Coverlet.Core.Reporters.Tests.OpenCoverReporterTests.TestReport()";
            var methods = new Methods
            {
                {methodString, new Method { Lines = lines, Branches = branches}}
            };

            return new Documents
            {
                {"doc.cs", new Classes {{"Coverlet.Core.Reporters.Tests.OpenCoverReporterTests", methods}}}
            };
        }
    }
}