using Coverlet.Core;
using System;
using System.Text;
using Xunit;

namespace coverlet.core.tests
{
    public class TeamCityServiceMessageWriterTests
    {
        [Fact]
        public void TestOutputLineCoverage()
        {
            // Arrange
            var actual = new StringBuilder();
            var expectedDetails = new CoverageDetails
            {
                Covered = 75D,
                Total = 100
            };

            var expected = new[]
            {
                $"##teamcity[buildStatisticValue key='CodeCoverageL' value='{expectedDetails.Total}']",
                $"##teamcity[buildStatisticValue key='CodeCoverageAbsLCovered' value='{expectedDetails.Covered}']",
                $"##teamcity[buildStatisticValue key='CodeCoverageAbsLTotal' value='{expectedDetails.Percent * 100}']",

            };
            var teamCityServiceMessageWriter = new TeamCityServiceMessageWriter(s => actual.AppendLine(s));

            // Act
            teamCityServiceMessageWriter.OutputLineCoverage(expectedDetails);

            // Assert
            Assert.Equal($"{string.Join(Environment.NewLine, expected)}{Environment.NewLine}", actual.ToString());
        }

        [Fact]
        public void TestOutputBranchCoverage()
        {
            // Arrange
            var actual = new StringBuilder();
            var expectedDetails = new CoverageDetails
            {
                Covered = 75D,
                Total = 100
            };

            var expected = new[]
            {
                $"##teamcity[buildStatisticValue key='CodeCoverageR' value='{expectedDetails.Total}']",
                $"##teamcity[buildStatisticValue key='CodeCoverageAbsRCovered' value='{expectedDetails.Covered}']",
                $"##teamcity[buildStatisticValue key='CodeCoverageAbsRTotal' value='{expectedDetails.Percent * 100}']",

            };
            var teamCityServiceMessageWriter = new TeamCityServiceMessageWriter(s => actual.AppendLine(s));

            // Act
            teamCityServiceMessageWriter.OutputBranchCoverage(expectedDetails);

            // Assert
            Assert.Equal($"{string.Join(Environment.NewLine, expected)}{Environment.NewLine}", actual.ToString());
        }

        [Fact]
        public void TestOutputClassCoverage()
        {
            // Arrange
            var actual = new StringBuilder();
            var expectedDetails = new CoverageDetails
            {
                Covered = 75D,
                Total = 100
            };

            var expected = new[]
            {
                $"##teamcity[buildStatisticValue key='CodeCoverageC' value='{expectedDetails.Total}']",
                $"##teamcity[buildStatisticValue key='CodeCoverageAbsCCovered' value='{expectedDetails.Covered}']",
                $"##teamcity[buildStatisticValue key='CodeCoverageAbsCTotal' value='{expectedDetails.Percent * 100}']",

            };
            var teamCityServiceMessageWriter = new TeamCityServiceMessageWriter(s => actual.AppendLine(s));

            // Act
            teamCityServiceMessageWriter.OutputClassCoverage(expectedDetails);

            // Assert
            Assert.Equal($"{string.Join(Environment.NewLine, expected)}{Environment.NewLine}", actual.ToString());
        }

        [Fact]
        public void TestOutputMethodCoverage()
        {
            // Arrange
            var actual = new StringBuilder();
            var expectedDetails = new CoverageDetails
            {
                Covered = 75D,
                Total = 100
            };

            var expected = new[]
            {
                $"##teamcity[buildStatisticValue key='CodeCoverageM' value='{expectedDetails.Total}']",
                $"##teamcity[buildStatisticValue key='CodeCoverageAbsMCovered' value='{expectedDetails.Covered}']",
                $"##teamcity[buildStatisticValue key='CodeCoverageAbsMTotal' value='{expectedDetails.Percent * 100}']",

            };
            var teamCityServiceMessageWriter = new TeamCityServiceMessageWriter(s => actual.AppendLine(s));

            // Act
            teamCityServiceMessageWriter.OutputMethodCoverage(expectedDetails);

            // Assert
            Assert.Equal($"{string.Join(Environment.NewLine, expected)}{Environment.NewLine}", actual.ToString());
        }
    }
}
