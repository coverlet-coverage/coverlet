using System;

namespace Coverlet.Core
{
    public class CoverageDetails
    {
        public double Covered { get; internal set; }
        public int Total { get; internal set; }
        public double Percent
        {
            get => Math.Round(Total == 0 ? Total : Covered / Total, 3);
        }
    }
}