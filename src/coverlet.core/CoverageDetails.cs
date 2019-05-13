using System;

namespace Coverlet.Core
{
    public class CoverageDetails
    {
        public double Covered { get; internal set; }
        public int Total { get; internal set; }
        public double Percent => Total == 0 ? 100D : Math.Floor((Covered / Total) * 10000) / 100;
    }
}