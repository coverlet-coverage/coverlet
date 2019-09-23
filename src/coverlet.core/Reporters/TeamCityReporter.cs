using System.Globalization;
using Coverlet.Core;
using Coverlet.Core.Reporters;
using System.Text;

namespace Coverlet.Core.Reporters
{
    internal class TeamCityReporter : IReporter
    {
        public ReporterOutputType OutputType => ReporterOutputType.Console;

        public string Format => "teamcity";

        public string Extension => null;

        public string Report(CoverageResult result)
        {
            // Calculate coverage
            var summary = new CoverageSummary();
            var overallLineCoverage = summary.CalculateLineCoverage(result.Modules);
            var overallBranchCoverage = summary.CalculateBranchCoverage(result.Modules);
            var overallMethodCoverage = summary.CalculateMethodCoverage(result.Modules);

            // Report coverage
            var stringBuilder = new StringBuilder();
            OutputLineCoverage(overallLineCoverage, stringBuilder);
            OutputBranchCoverage(overallBranchCoverage, stringBuilder);
            OutputMethodCoverage(overallMethodCoverage, stringBuilder);

            // Return a placeholder
            return stringBuilder.ToString();
        }

        private void OutputLineCoverage(CoverageDetails coverageDetails, StringBuilder builder)
        {
            // The number of covered lines
            OutputTeamCityServiceMessage("CodeCoverageAbsLCovered", coverageDetails.Covered, builder);

            // Line-level code coverage
            OutputTeamCityServiceMessage("CodeCoverageAbsLTotal", coverageDetails.Total, builder);
        }

        private void OutputBranchCoverage(CoverageDetails coverageDetails, StringBuilder builder)
        {
            // The number of covered branches
            OutputTeamCityServiceMessage("CodeCoverageAbsBCovered", coverageDetails.Covered, builder);

            // Branch-level code coverage
            OutputTeamCityServiceMessage("CodeCoverageAbsBTotal", coverageDetails.Total, builder);
        }

        private void OutputMethodCoverage(CoverageDetails coverageDetails, StringBuilder builder)
        {
            // The number of covered methods
            OutputTeamCityServiceMessage("CodeCoverageAbsMCovered", coverageDetails.Covered, builder);

            // Method-level code coverage
            OutputTeamCityServiceMessage("CodeCoverageAbsMTotal", coverageDetails.Total, builder);
        }

        private void OutputTeamCityServiceMessage(string key, double value, StringBuilder builder)
        {
            builder.AppendLine($"##teamcity[buildStatisticValue key='{key}' value='{value.ToString("0.##", new CultureInfo("en-US"))}']");
        }
    }
}
