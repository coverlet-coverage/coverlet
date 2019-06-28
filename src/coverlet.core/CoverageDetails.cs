using System;

namespace Coverlet.Core
{
    public class CoverageDetails
    {
        public double Covered { get; internal set; }
        public int Total { get; internal set; }
        private double averageModulePercent;
        public double AverageModulePercent
        {
            get { return Math.Round(averageModulePercent, 2); ; }
            internal set { averageModulePercent = value; }
        }

        public double Percent => Total == 0 ? 100D : Math.Floor((Covered / Total) * 10000) / 100;
    }
}