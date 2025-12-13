// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace BasicTestProject;

public class DummyTests
{
  [Fact]
  public void DummyTest_Passes()
  {
    Assert.True(true);
  }

  [Fact]
  public void SimpleMath_Works()
  {
    int result = Add(2, 3);
    Assert.Equal(5, result);
  }

  private int Add(int a, int b)
  {
    return a + b;
  }
}
