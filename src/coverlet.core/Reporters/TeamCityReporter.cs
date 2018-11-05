using Coverlet.Core;
using Coverlet.Core.Reporters;
using System.Text;

namespace coverlet.core.Reporters
{
    public class TeamCityReporter : IReporter
    {
        public bool UseConsoleOutput => true;

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
            var teamCityServiceMessageWriter = new TeamCityServiceMessageWriter(s => stringBuilder.AppendLine(s));
            teamCityServiceMessageWriter.OutputLineCoverage(overallLineCoverage);
            teamCityServiceMessageWriter.OutputBranchCoverage(overallBranchCoverage);
            teamCityServiceMessageWriter.OutputMethodCoverage(overallMethodCoverage);

            // Return a placeholder
            return stringBuilder.ToString();
        }
    }
}
