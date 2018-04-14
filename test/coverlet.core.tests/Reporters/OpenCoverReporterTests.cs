using System;
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
            Assert.NotEqual(string.Empty, reporter.Report(result));
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

        private static Documents CreateFirstDocuments()
        {
            Lines lines = new Lines();
            lines.Add(1, new LineInfo { Hits = 1 });
            lines.Add(2, new LineInfo { Hits = 0 });
            Methods methods = new Methods();
            methods.Add("System.Void Coverlet.Core.Reporters.Tests.OpenCoverReporterTests.TestReport()", lines);
            Classes classes = new Classes();
            classes.Add("Coverlet.Core.Reporters.Tests.OpenCoverReporterTests", methods);
            Documents documents = new Documents();
            documents.Add("doc.cs", classes);
            return documents;
        }

        private static Documents CreateSecondDocuments()
        {            
            Lines lines = new Lines();
            lines.Add(1, new LineInfo { Hits = 1 });
            lines.Add(2, new LineInfo { Hits = 0 });

            Methods methods = new Methods();
            methods.Add("System.Void Some.Other.Module.TestClass.TestMethod()", lines);

            Classes classes2 = new Classes();
            classes2.Add("Some.Other.Module.TestClass", methods);

            var documents = new Documents();
            documents.Add("TestClass.cs", classes2);

            return documents;
        }
    }
}