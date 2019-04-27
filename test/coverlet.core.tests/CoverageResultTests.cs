using System;
using System.Collections.Generic;
using System.Linq;

using Coverlet.Core;
using Coverlet.Core.Enums;
using Moq;
using Xunit;

namespace Coverlet.Core.Tests
{
    using ThresholdTypeFlagValues = Dictionary<ThresholdTypeFlags, double>;

    public class CoverageResultTests
    {
        private Modules _modules;

        public CoverageResultTests()
        {
            Lines lines = new Lines();
            lines.Add(1, 1);
            lines.Add(2, 1);
            lines.Add(3, 1);
            Branches branches = new Branches();
            branches.Add(new BranchInfo { Line = 1, Hits = 1, Offset = 1, Path = 0, Ordinal = 1 });
            branches.Add(new BranchInfo { Line = 1, Hits = 1, Offset = 1, Path = 1, Ordinal = 2 });
            branches.Add(new BranchInfo { Line = 2, Hits = 0, Offset = 1, Path = 0, Ordinal = 1 });

            Methods methods = new Methods();
            var methodString = "System.Void Coverlet.Core.Tests.CoverageResultTests::CoverageResultTests()";
            methods.Add(methodString, new Method());
            methods[methodString].Lines = lines;
            methods[methodString].Branches = branches;


            methodString = "System.Void Coverlet.Core.Tests.CoverageResultTests::GetThresholdTypesBelowThreshold()";
            methods.Add(methodString, new Method());
            methods[methodString].Lines = new Lines()
            {
                {1, 0},
                {2, 0},
            };
            methods[methodString].Branches = branches;

            Classes classes = new Classes();
            classes.Add("Coverlet.Core.Tests.CoverageResultTests", methods);

            Documents documents = new Documents();
            documents.Add("doc.cs", classes);

            _modules = new Modules();
            _modules.Add("module", documents);
        }

        [Fact]
        public void TestGetThresholdTypesBelowThresholdLine()
        {
            CoverageResult result = new CoverageResult();
            result.Modules = _modules;

            CoverageSummary summary = new CoverageSummary();
            ThresholdTypeFlagValues thresholdTypeFlagValues = new ThresholdTypeFlagValues()
            {
                {  ThresholdTypeFlags.Line, 90 },
                {  ThresholdTypeFlags.Method, 10 },
                {  ThresholdTypeFlags.Branch, 10 },
            };

            ThresholdTypeFlags thresholdTypeFlags = ThresholdTypeFlags.Line | ThresholdTypeFlags.Branch | ThresholdTypeFlags.Method;
            ThresholdStatistic thresholdStatic = ThresholdStatistic.Minimum;

            ThresholdTypeFlags resThresholdTypeFlags = result.GetThresholdTypesBelowThreshold(summary, thresholdTypeFlagValues, thresholdTypeFlags, thresholdStatic);
            Assert.Equal(ThresholdTypeFlags.Line, resThresholdTypeFlags);
        }

        [Fact]
        public void TestGetThresholdTypesBelowThresholdMethod()
        {
            CoverageResult result = new CoverageResult();
            result.Modules = _modules;

            CoverageSummary summary = new CoverageSummary();
            ThresholdTypeFlagValues thresholdTypeFlagValues = new ThresholdTypeFlagValues()
            {
                {  ThresholdTypeFlags.Line, 50 },
                {  ThresholdTypeFlags.Method, 75 },
                {  ThresholdTypeFlags.Branch, 10 },
            };

            ThresholdTypeFlags thresholdTypeFlags = ThresholdTypeFlags.Line | ThresholdTypeFlags.Branch | ThresholdTypeFlags.Method;
            ThresholdStatistic thresholdStatic = ThresholdStatistic.Minimum;

            ThresholdTypeFlags resThresholdTypeFlags = result.GetThresholdTypesBelowThreshold(summary, thresholdTypeFlagValues, thresholdTypeFlags, thresholdStatic);
            Assert.Equal(ThresholdTypeFlags.Method, resThresholdTypeFlags);
        }

        [Fact]
        public void TestGetThresholdTypesBelowThresholdBranch()
        {
            CoverageResult result = new CoverageResult();
            result.Modules = _modules;

            CoverageSummary summary = new CoverageSummary();
            ThresholdTypeFlagValues thresholdTypeFlagValues = new ThresholdTypeFlagValues()
            {
                {  ThresholdTypeFlags.Line, 50 },
                {  ThresholdTypeFlags.Method, 50 },
                {  ThresholdTypeFlags.Branch, 90 },
            };

            ThresholdTypeFlags thresholdTypeFlags = ThresholdTypeFlags.Line | ThresholdTypeFlags.Branch | ThresholdTypeFlags.Method;
            ThresholdStatistic thresholdStatic = ThresholdStatistic.Total;

            ThresholdTypeFlags resThresholdTypeFlags = result.GetThresholdTypesBelowThreshold(summary, thresholdTypeFlagValues, thresholdTypeFlags, thresholdStatic);
            Assert.Equal(ThresholdTypeFlags.Branch, resThresholdTypeFlags);
        }

        [Fact]
        public void TestGetThresholdTypesBelowThresholdAllGood()
        {
            CoverageResult result = new CoverageResult();
            result.Modules = _modules;

            CoverageSummary summary = new CoverageSummary();
            ThresholdTypeFlagValues thresholdTypeFlagValues = new ThresholdTypeFlagValues()
            {
                {  ThresholdTypeFlags.Line, 50 },
                {  ThresholdTypeFlags.Method, 50 },
                {  ThresholdTypeFlags.Branch, 50 },
            };

            ThresholdTypeFlags thresholdTypeFlags = ThresholdTypeFlags.Line | ThresholdTypeFlags.Branch | ThresholdTypeFlags.Method;
            ThresholdStatistic thresholdStatic = ThresholdStatistic.Average;

            ThresholdTypeFlags resThresholdTypeFlags = result.GetThresholdTypesBelowThreshold(summary, thresholdTypeFlagValues, thresholdTypeFlags, thresholdStatic);
            Assert.Equal(ThresholdTypeFlags.None, resThresholdTypeFlags);
        }

        [Fact]
        public void TestGetThresholdTypesBelowThresholdAllFail()
        {
            CoverageResult result = new CoverageResult();
            result.Modules = _modules;

            CoverageSummary summary = new CoverageSummary();
            ThresholdTypeFlagValues thresholdTypeFlagValues = new ThresholdTypeFlagValues()
            {
                {  ThresholdTypeFlags.Line, 100 },
                {  ThresholdTypeFlags.Method, 100 },
                {  ThresholdTypeFlags.Branch, 100 },
            };

            ThresholdTypeFlags thresholdTypeFlags = ThresholdTypeFlags.Line | ThresholdTypeFlags.Branch | ThresholdTypeFlags.Method;
            ThresholdStatistic thresholdStatic = ThresholdStatistic.Minimum;

            ThresholdTypeFlags resThresholdTypeFlags = result.GetThresholdTypesBelowThreshold(summary, thresholdTypeFlagValues, thresholdTypeFlags, thresholdStatic);
            Assert.Equal(thresholdTypeFlags, resThresholdTypeFlags);
        }
    }
}