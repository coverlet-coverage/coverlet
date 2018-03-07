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
            lines.Add(1, 1);
            lines.Add(2, 0);
            Documents documents = new Documents();
            documents.Add("doc.cs", lines);
            result.Modules = new Modules();
            result.Modules.Add("module", documents);

            LcovReporter reporter = new LcovReporter();
            Assert.NotEqual(string.Empty, reporter.Format(result));
        }
    }
}