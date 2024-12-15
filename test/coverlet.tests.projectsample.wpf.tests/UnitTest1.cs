// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Windows;
using Xunit;

namespace coverlet.tests.projectsample.wpf.tests;

public class UnitTest1
{
  [Fact]
  public void Test1()
  {
    var myCheck = new issue_1713();
    Assert.NotNull(myCheck);

    Assert.Equal(MessageBoxButton.OK, myCheck.Method_1713());
  }
}
