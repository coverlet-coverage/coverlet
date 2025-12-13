// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CalculateClassLibrary;
using Xunit;

namespace CalculateTestProject;

public class UnitTest1
{
    [Fact]
    public void AddTest()
    {
        Assert.Equal(5, CalculateClass.Add(2, 3));
    }
}
