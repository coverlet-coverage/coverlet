using System;
using Xunit;

namespace Coverlet.Core.Reporters.Tests
{
    public class LcovReporterTests
    {
        [Fact]
        public void TestFormat()
        {
            CoverageResult result = new CoverageResult();
            result.Identifier = Guid.NewGuid().ToString();
            Lines lines = new Lines();
            lines.Add(1, new LineInfo { Hits = 1 });
            lines.Add(2, new LineInfo { Hits = 0 });
            Methods methods = new Methods();
            methods.Add("System.Void Coverlet.Core.Reporters.Tests.LcovReporterTests.TestFormat()", lines);
            Classes classes = new Classes();
            classes.Add("Coverlet.Core.Reporters.Tests.LcovReporterTests", methods);
            Documents documents = new Documents();
            documents.Add("doc.cs", classes);
            result.Modules = new Modules();
            result.Modules.Add("module", documents);

            LcovReporter reporter = new LcovReporter();
            Assert.NotEqual(string.Empty, reporter.Report(result));
        }
    }
}