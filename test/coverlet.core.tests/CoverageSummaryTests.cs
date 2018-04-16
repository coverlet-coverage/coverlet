using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using Coverlet.Core;
using Moq;
using Xunit;

namespace Coverlet.Core.Tests
{
    [ExcludeFromCodeCoverage]
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
            
            Assert.Equal(0.5, summary.CalculateLineCoverage(module.Value));
            Assert.Equal(0.5, summary.CalculateLineCoverage(document.Value));
            Assert.Equal(0.5, summary.CalculateLineCoverage(@class.Value));
            Assert.Equal(0.5, summary.CalculateLineCoverage(method.Value));
        }

        [Fact]
        public void TestCalculateBranchCoverage()
        {
            CoverageSummary summary = new CoverageSummary();

            var module = _modules.First();
            var document = module.Value.First();
            var @class = document.Value.First();
            var method = @class.Value.First();

            Assert.Equal(1, summary.CalculateBranchCoverage(module.Value));
            Assert.Equal(1, summary.CalculateBranchCoverage(document.Value));
            Assert.Equal(1, summary.CalculateBranchCoverage(@class.Value));
            Assert.Equal(1, summary.CalculateBranchCoverage(method.Value));
        }

        [Fact]
        public void TestCalculateMethodCoverage()
        {
            CoverageSummary summary = new CoverageSummary();

            var module = _modules.First();
            var document = module.Value.First();
            var @class = document.Value.First();
            var method = @class.Value.First();

            Assert.Equal(1, summary.CalculateMethodCoverage(module.Value));
            Assert.Equal(1, summary.CalculateMethodCoverage(document.Value));
            Assert.Equal(1, summary.CalculateMethodCoverage(@class.Value));
            Assert.Equal(1, summary.CalculateMethodCoverage(method.Value));
        }
    }
}