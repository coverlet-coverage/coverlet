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
            lines.Add(1, 1);
            lines.Add(2, 0);
            Documents documents = new Documents();
            documents.Add("doc.cs", lines);
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