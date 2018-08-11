using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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
            using (var memoryStream = new MemoryStream())
            {
                using (var streamWriter = new StreamWriter(memoryStream))
                    reporter.Report(result, streamWriter);

                var reportString = Encoding.UTF8.GetString(memoryStream.ToArray());
                Assert.NotEqual(string.Empty, reportString);
            }
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
            using (var memoryStream = new MemoryStream())
            {
                using (var streamWriter = new StreamWriter(memoryStream))
                    reporter.Report(result, streamWriter);

                var reportString = Encoding.UTF8.GetString(memoryStream.ToArray());
                Assert.NotEqual(string.Empty, reportString);
                Assert.Contains(@"<FileRef uid=""1"" />", reportString);
                Assert.Contains(@"<FileRef uid=""2"" />", reportString);
            }
        }

        private static Documents CreateFirstDocuments()
        {
            Lines lines = new Lines();
            lines.Add(1, 1);
            lines.Add(2, 0);

            Branches branches = new Branches();
            branches.Add(new BranchInfo { Line = 1, Hits = 1, Offset = 23, EndOffset = 24, Path = 0, Ordinal = 1 });
            branches.Add(new BranchInfo { Line = 1, Hits = 0, Offset = 23, EndOffset = 27, Path = 1, Ordinal = 2 });

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
    }
}