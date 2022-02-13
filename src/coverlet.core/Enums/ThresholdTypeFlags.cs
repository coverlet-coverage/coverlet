// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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