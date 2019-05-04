using System;

namespace Coverlet.Core
{
    public class CoverageDetails
    {
        public double Covered { get; internal set; }
        public int Total { get; internal set; }
        public double Percent
        {
            get => Math.Round(Total == 0 ? 1 : Covered / Total, 4);
        }

        public double GetCoveragePercentage()
        {
            double percentage = Percent * 100;
            return RoundDown(percentage);
        }

        private double RoundDown(double percentage)
        {
            return Math.Floor(percentage * 100) / 100;
        }
    }
}