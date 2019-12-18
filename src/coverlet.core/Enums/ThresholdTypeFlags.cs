using System;

namespace Coverlet.Core.Enums
{
    [Flags]
    internal enum ThresholdTypeFlags
    {
        None = 0,
        Line = 2,
        Branch = 4,
        Method = 8
    }
}