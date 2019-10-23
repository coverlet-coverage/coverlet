using System;

namespace Coverlet.Core
{
    public class CoverageDetails
    {
        private double _averageModulePercent;
        public double Covered { get; internal set; }
        public int Total { get; internal set; }
        public double AverageModulePercent
        {
            get { return Math.Floor(_averageModulePercent * 100) / 100; }
            internal set { _averageModulePercent = value; }
        }

        public double Percent => Total == 0 ? 100D : Math.Floor((Covered / Total) * 10000) / 100;
    }
}