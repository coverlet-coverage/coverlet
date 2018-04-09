using System;

using Coverlet.Core;
using Moq;
using Xunit;

namespace Coverlet.Core.Tests
{
    public class CoverageSummaryTests
    {
        [Fact]
        public void TestCalculateSummary()
        {
            CoverageResult result = new CoverageResult();
            result.Identifier = Guid.NewGuid().ToString();
            Lines lines = new Lines();
            lines.Add(1, new LineInfo { Hits = 1 });
            lines.Add(2, new LineInfo { Hits = 0 });
            Methods methods = new Methods();
            methods.Add("System.Void Coverlet.Core.Tests.CoverageSummaryTests.TestCalculateSummary()", lines);
            Classes classes = new Classes();
            classes.Add("Coverlet.Core.Tests.CoverageSummaryTests", methods);
            Documents documents = new Documents();
            documents.Add("doc.cs", classes);
            result.Modules = new Modules();
            result.Modules.Add("module", documents);

            CoverageSummary summary = new CoverageSummary(result);
            CoverageSummaryResult summaryResult = summary.CalculateSummary();

            Assert.NotEmpty(summaryResult);
            Assert.True(summaryResult.ContainsKey("module"));
            Assert.Equal(50, summaryResult["module"]);
        }
    }
}