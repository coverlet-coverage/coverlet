using Coverlet.Core;
using Coverlet.Core.Reporters;
using System.Text;

namespace coverlet.core.Reporters
{
    public class TeamCityReporter : IReporter
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
            // The total number of lines
            OutputTeamCityServiceMessage("CodeCoverageL", coverageDetails.Total, builder);

            // The number of covered lines
            OutputTeamCityServiceMessage("CodeCoverageAbsLCovered", coverageDetails.Covered, builder);

            // Line-level code coverage
            OutputTeamCityServiceMessage("CodeCoverageAbsLTotal", coverageDetails.Percent * 100, builder);
        }

        private void OutputBranchCoverage(CoverageDetails coverageDetails, StringBuilder builder)
        {
            // The total number of branches
            OutputTeamCityServiceMessage("CodeCoverageR", coverageDetails.Total, builder);

            // The number of covered branches
            OutputTeamCityServiceMessage("CodeCoverageAbsRCovered", coverageDetails.Covered, builder);

            // Branch-level code coverage
            OutputTeamCityServiceMessage("CodeCoverageAbsRTotal", coverageDetails.Percent * 100, builder);
        }

        private void OutputMethodCoverage(CoverageDetails coverageDetails, StringBuilder builder)
        {
            // The total number of methods
            OutputTeamCityServiceMessage("CodeCoverageM", coverageDetails.Total, builder);

            // The number of covered methods
            OutputTeamCityServiceMessage("CodeCoverageAbsMCovered", coverageDetails.Covered, builder);

            // Method-level code coverage
            OutputTeamCityServiceMessage("CodeCoverageAbsMTotal", coverageDetails.Percent * 100, builder);
        }

        private void OutputTeamCityServiceMessage(string key, object value, StringBuilder builder)
        {
            builder.AppendLine($"##teamcity[buildStatisticValue key='{key}' value='{value}']");
        }
    }
}
