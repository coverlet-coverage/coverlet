using Coverlet.Core;
using System;

namespace coverlet.core
{
    public class TeamCityServiceMessageWriter
    {
        private readonly Action<string> _writer;

        public TeamCityServiceMessageWriter(Action<string> writer)
        {
            _writer = writer;
        }

        public void OutputLineCoverage(CoverageDetails coverageDetails)
        {
            // The total number of lines
            OutputTeamCityServiceMessage("CodeCoverageL", coverageDetails.Total);

            // The number of covered lines
            OutputTeamCityServiceMessage("CodeCoverageAbsLCovered", coverageDetails.Covered);

            // Line-level code coverage
            OutputTeamCityServiceMessage("CodeCoverageAbsLTotal", coverageDetails.Percent * 100);
        }

        public void OutputBranchCoverage(CoverageDetails coverageDetails)
        {
            // The total number of branches
            OutputTeamCityServiceMessage("CodeCoverageR", coverageDetails.Total);

            // The number of covered branches
            OutputTeamCityServiceMessage("CodeCoverageAbsRCovered", coverageDetails.Covered);

            // Branch-level code coverage
            OutputTeamCityServiceMessage("CodeCoverageAbsRTotal", coverageDetails.Percent * 100);
        }

        public void OutputClassCoverage(CoverageDetails coverageDetails)
        {
            // The total number of classes
            OutputTeamCityServiceMessage("CodeCoverageC", coverageDetails.Total);

            // The number of covered classes
            OutputTeamCityServiceMessage("CodeCoverageAbsCCovered", coverageDetails.Covered);

            // Class-level code coverage
            OutputTeamCityServiceMessage("CodeCoverageAbsCTotal", coverageDetails.Percent * 100);
        }

        public void OutputMethodCoverage(CoverageDetails coverageDetails)
        {
            // The total number of methods
            OutputTeamCityServiceMessage("CodeCoverageM", coverageDetails.Total);

            // The number of covered methods
            OutputTeamCityServiceMessage("CodeCoverageAbsMCovered", coverageDetails.Covered);

            // Method-level code coverage
            OutputTeamCityServiceMessage("CodeCoverageAbsMTotal", coverageDetails.Percent * 100);
        }

        private void OutputTeamCityServiceMessage(string key, object value)
        {
            _writer?.Invoke($"##teamcity[buildStatisticValue key='{key}' value='{value}']");
        }
    }
}
