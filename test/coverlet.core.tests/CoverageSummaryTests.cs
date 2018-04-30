using System;
using System.Collections.Generic;
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
            lines.Add(1, new LineInfo { Hits = 1 });
            lines.Add(2, new LineInfo { Hits = 0 });
            Branches branches = new Branches();
            branches.Add(1, new List<BranchInfo>());
            branches[1].Add(new BranchInfo { Hits = 1, Offset = 1, Path = 0, Ordinal = 1 });
            branches[1].Add(new BranchInfo { Hits = 1, Offset = 1, Path = 1, Ordinal = 2 });

            Methods methods = new Methods();
            var methodString = "System.Void Coverlet.Core.Tests.CoverageSummaryTests::TestCalculateSummary()";
            methods.Add(methodString, new Method());
            methods[methodString].Lines = lines;
            methods[methodString].Branches = branches;

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
            Assert.Equal(0.5, summary.CalculateLineCoverage(method.Value.Lines));
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
            Assert.Equal(1, summary.CalculateBranchCoverage(method.Value.Branches));
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
            Assert.Equal(1, summary.CalculateMethodCoverage(method.Value.Lines));
        }
    }
}