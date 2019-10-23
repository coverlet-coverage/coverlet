using System;
using Coverlet.Core.Reporters;
using Xunit;

namespace Coverlet.Core.Reporters.Tests
{
    public class TestCreateReporterTests
    {
        private readonly CoverageResult _result;
        private readonly TeamCityReporter _reporter;

        public TestCreateReporterTests()
        {
            _reporter = new TeamCityReporter();
            _result = new CoverageResult();
            _result.Identifier = Guid.NewGuid().ToString();

            var lines = new Lines { { 1, 1 }, { 2, 0 } };

            var branches = new Branches
            {
                new BranchInfo
                {
                    Line = 1,
                    Hits = 1,
                    Offset = 23,
                    EndOffset = 24,
                    Path = 0,
                    Ordinal = 1
                },
                new BranchInfo
                {
                    Line = 1,
                    Hits = 0,
                    Offset = 23,
                    EndOffset = 27,
                    Path = 1,
                    Ordinal = 2
                },
                new BranchInfo
                {
                    Line = 1,
                    Hits = 0,
                    Offset = 23,
                    EndOffset = 27,
                    Path = 1,
                    Ordinal = 2
                }
            };

            var methods = new Methods();
            var methodString = "System.Void Coverlet.Core.Reporters.Tests.CoberturaReporterTests::TestReport()";
            methods.Add(methodString, new Method());
            methods[methodString].Lines = lines;
            methods[methodString].Branches = branches;

            var classes = new Classes { { "Coverlet.Core.Reporters.Tests.CoberturaReporterTests", methods } };

            var documents = new Documents { { "doc.cs", classes } };

            _result.Modules = new Modules { { "module", documents } };
        }

        [Fact]
        public void OutputType_IsConsoleOutputType()
        {
            // Assert
            Assert.Equal(ReporterOutputType.Console, _reporter.OutputType);
        }

        [Fact]
        public void Format_IsExpectedValue()
        {
            // Assert
            Assert.Equal("teamcity", _reporter.Format);
        }

        [Fact]
        public void Format_IsNull()
        {
            // Assert
            Assert.Null(_reporter.Extension);
        }

        [Fact]
        public void Report_ReturnsNonNullString()
        {
            // Act
            var output = _reporter.Report(_result);

            // Assert
            Assert.False(string.IsNullOrWhiteSpace(output), "Output is not null or whitespace");
        }

        [Fact]
        public void Report_ReportsLineCoverage()
        {
            // Act
            var output = _reporter.Report(_result);

            // Assert
            Assert.Contains("##teamcity[buildStatisticValue key='CodeCoverageAbsLCovered' value='1']", output);
            Assert.Contains("##teamcity[buildStatisticValue key='CodeCoverageAbsLTotal' value='2']", output);
        }

        [Fact]
        public void Report_ReportsBranchCoverage()
        {
            // Act
            var output = _reporter.Report(_result);

            // Assert
            Assert.Contains("##teamcity[buildStatisticValue key='CodeCoverageAbsBCovered' value='1']", output);
            Assert.Contains("##teamcity[buildStatisticValue key='CodeCoverageAbsBTotal' value='3']", output);
        }

        [Fact]
        public void Report_ReportsMethodCoverage()
        {
            // Act
            var output = _reporter.Report(_result);

            // Assert
            Assert.Contains("##teamcity[buildStatisticValue key='CodeCoverageAbsMCovered' value='1']", output);
            Assert.Contains("##teamcity[buildStatisticValue key='CodeCoverageAbsMTotal' value='1']", output);
        }
    }
}