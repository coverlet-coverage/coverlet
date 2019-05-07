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
        private Modules _moduleArithmeticPrecision;

        public CoverageSummaryTests()
        {
            SetupData();
            SetupDataForArithmeticPrecision();
        }

        private void SetupDataForArithmeticPrecision()
        {
            Lines lines = new Lines();
            lines.Add(1, 1);
            for (int i = 2; i <= 6; i++)
            {
                lines.Add(i, 0);
            }
            Branches branches = new Branches();
            branches.Add(new BranchInfo { Line = 1, Hits = 1, Offset = 1, Path = 0, Ordinal = 1 });
            for (int i = 2; i <= 6; i++)
            {
                branches.Add(new BranchInfo { Line = 1, Hits = 0, Offset = 1, Path = 1, Ordinal = (uint)i });
            }

            Methods methods = new Methods();
            var methodString = "System.Void Coverlet.Core.Tests.CoverageSummaryTests::TestCalculateSummary()";
            methods.Add(methodString, new Method());
            methods[methodString].Lines = lines;
            methods[methodString].Branches = branches;

            Classes classes = new Classes();
            classes.Add("Coverlet.Core.Tests.CoverageSummaryTests", methods);

            Documents documents = new Documents();
            documents.Add("doc.cs", classes);

            _moduleArithmeticPrecision = new Modules();
            _moduleArithmeticPrecision.Add("module", documents);
        }

        private void SetupData()
        {
            Lines lines = new Lines();
            lines.Add(1, 1);
            lines.Add(2, 0);
            Branches branches = new Branches();
            branches.Add(new BranchInfo { Line = 1, Hits = 1, Offset = 1, Path = 0, Ordinal = 1 });
            branches.Add(new BranchInfo { Line = 1, Hits = 1, Offset = 1, Path = 1, Ordinal = 2 });

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

            Assert.Equal(50, summary.CalculateLineCoverage(module.Value).Percent);
            Assert.Equal(50, summary.CalculateLineCoverage(document.Value).Percent);
            Assert.Equal(50, summary.CalculateLineCoverage(@class.Value).Percent);
            Assert.Equal(50, summary.CalculateLineCoverage(method.Value.Lines).Percent);
        }

        [Fact]
        public void TestCalculateBranchCoverage()
        {
            CoverageSummary summary = new CoverageSummary();

            var module = _modules.First();
            var document = module.Value.First();
            var @class = document.Value.First();
            var method = @class.Value.First();

            Assert.Equal(100, summary.CalculateBranchCoverage(module.Value).Percent);
            Assert.Equal(100, summary.CalculateBranchCoverage(document.Value).Percent);
            Assert.Equal(100, summary.CalculateBranchCoverage(@class.Value).Percent);
            Assert.Equal(100, summary.CalculateBranchCoverage(method.Value.Branches).Percent);
        }

        [Fact]
        public void TestCalculateMethodCoverage()
        {
            CoverageSummary summary = new CoverageSummary();

            var module = _modules.First();
            var document = module.Value.First();
            var @class = document.Value.First();
            var method = @class.Value.First();

            Assert.Equal(100, summary.CalculateMethodCoverage(module.Value).Percent);
            Assert.Equal(100, summary.CalculateMethodCoverage(document.Value).Percent);
            Assert.Equal(100, summary.CalculateMethodCoverage(@class.Value).Percent);
            Assert.Equal(100, summary.CalculateMethodCoverage(method.Value.Lines).Percent);
        }

        [Fact]
        public void TestCalculateLineCoveragePercentage_ArithmeticPrecisionCheck()
        {
            CoverageSummary summary = new CoverageSummary();

            var module = _moduleArithmeticPrecision.First();
            var document = module.Value.First();
            var @class = document.Value.First();
            var method = @class.Value.First();

            Assert.Equal(16.66, summary.CalculateLineCoverage(module.Value).Percent);
            Assert.Equal(16.66, summary.CalculateLineCoverage(document.Value).Percent);
            Assert.Equal(16.66, summary.CalculateLineCoverage(@class.Value).Percent);
            Assert.Equal(16.66, summary.CalculateLineCoverage(method.Value.Lines).Percent);
        }

        [Fact]
        public void TestCalculateBranchCoveragePercentage_ArithmeticPrecisionCheck()
        {
            CoverageSummary summary = new CoverageSummary();

            var module = _moduleArithmeticPrecision.First();
            var document = module.Value.First();
            var @class = document.Value.First();
            var method = @class.Value.First();

            Assert.Equal(16.66, summary.CalculateBranchCoverage(module.Value).Percent);
            Assert.Equal(16.66, summary.CalculateBranchCoverage(document.Value).Percent);
            Assert.Equal(16.66, summary.CalculateBranchCoverage(@class.Value).Percent);
            Assert.Equal(16.66, summary.CalculateBranchCoverage(method.Value.Branches).Percent);
        }
    }
}