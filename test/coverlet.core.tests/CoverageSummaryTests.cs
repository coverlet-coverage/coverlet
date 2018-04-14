using System;
using System.Linq;

using Coverlet.Core;
using Moq;
using Xunit;

namespace Coverlet.Core.Tests
{
    public class CoverageSummaryTests
    {
        private Modules _modules;

        public CoverageSummaryTests()
        {
            Lines lines = new Lines();
            lines.Add(1, new LineInfo { Hits = 1, IsBranchPoint = true });
            lines.Add(2, new LineInfo { Hits = 0 });

            Methods methods = new Methods();
            methods.Add("System.Void Coverlet.Core.Tests.CoverageSummaryTests::TestCalculateSummary()", lines);

            Classes classes = new Classes();
            classes.Add("Coverlet.Core.Tests.CoverageSummaryTests", methods);

            Documents documents = new Documents();
            documents.Add("doc.cs", classes);

            _modules = new Modules();
            _modules.Add("module", documents);
        }

        [Fact]
        public void TestCalculateLineCoverage()
        {
            CoverageSummary summary = new CoverageSummary();

            var module = _modules.First();
            var document = module.Value.First();
            var @class = document.Value.First();
            var method = @class.Value.First();
            
            Assert.Equal(0.5, summary.CalculateLineCoverage(module));
            Assert.Equal(0.5, summary.CalculateLineCoverage(document));
            Assert.Equal(0.5, summary.CalculateLineCoverage(@class));
            Assert.Equal(0.5, summary.CalculateLineCoverage(method));
        }

        [Fact]
        public void TestCalculateBranchCoverage()
        {
            CoverageSummary summary = new CoverageSummary();

            var module = _modules.First();
            var document = module.Value.First();
            var @class = document.Value.First();
            var method = @class.Value.First();

            Assert.Equal(1, summary.CalculateBranchCoverage(module));
            Assert.Equal(1, summary.CalculateBranchCoverage(document));
            Assert.Equal(1, summary.CalculateBranchCoverage(@class));
            Assert.Equal(1, summary.CalculateBranchCoverage(method));
        }

        [Fact]
        public void TestCalculateMethodCoverage()
        {
            CoverageSummary summary = new CoverageSummary();

            var module = _modules.First();
            var document = module.Value.First();
            var @class = document.Value.First();
            var method = @class.Value.First();

            Assert.Equal(1, summary.CalculateMethodCoverage(module));
            Assert.Equal(1, summary.CalculateMethodCoverage(document));
            Assert.Equal(1, summary.CalculateMethodCoverage(@class));
            Assert.Equal(1, summary.CalculateMethodCoverage(method));
        }
    }
}