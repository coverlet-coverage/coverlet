// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable disable

namespace Coverlet.Tests.Xunit.Extensions
{
    public interface ITestCondition
    {
        bool IsMet { get; }
        string SkipReason { get; }
    }
}
