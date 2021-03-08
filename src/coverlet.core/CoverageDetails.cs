using System;

namespace Coverlet.Core
{
    internal class CoverageDetails
    {
        private double _averageModulePercent;
        private double _covered;

        public double Covered
        {
            get => _covered;
            internal set
            {
                _covered = value;
                Percent = Total == 0 ? 100D : Math.Floor((_covered / Total) * 10000) / 100;
            }
        }

        public int Total { get; internal set; }
        public double Percent { get; internal set; }
        public double AverageModulePercent
        {
            get { return Math.Floor(_averageModulePercent * 100) / 100; }
            internal set { _averageModulePercent = value; }
        }
    }
}