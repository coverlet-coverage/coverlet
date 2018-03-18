using System;
using Xunit;

namespace Coverlet.Core.Reporters.Tests
{
    public class OpenCoverReporterTests
    {
        [Fact]
        public void TestFormat()
        {
            CoverageResult result = new CoverageResult();
            result.Identifier = Guid.NewGuid().ToString();
            Lines lines = new Lines();
            lines.Add(1, 1);
            lines.Add(2, 0);
            Methods methods = new Methods();
            methods.Add("System.Void Coverlet.Core.Reporters.Tests.OpenCoverReporterTests.TestFormat()", lines);
            Classes classes = new Classes();
            classes.Add("Coverlet.Core.Reporters.Tests.OpenCoverReporterTests", methods);
            Documents documents = new Documents();
            documents.Add("doc.cs", classes);
            result.Modules = new Modules();
            result.Modules.Add("module", documents);

            OpenCoverReporter reporter = new OpenCoverReporter();
            Assert.NotEqual(string.Empty, reporter.Format(result));
        }
    }
}