// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace coverlet.MTP.validation.tests;

public class Tests
{
  [Test]
  public void Basic()
  {
    Console.WriteLine("This is a basic test");
  }

  [Test]
  [Arguments(1, 2, 3)]
  [Arguments(2, 3, 5)]
  public async Task DataDrivenArguments(int a, int b, int c)
  {
    Console.WriteLine("This one can accept arguments from an attribute");

    var result = a + b;

    await Assert.That(result).IsEqualTo(c);
  }

  [Test]
  [MethodDataSource(nameof(DataSource))]
  public async Task MethodDataSource(int a, int b, int c)
  {
    Console.WriteLine("This one can accept arguments from a method");

    var result = a + b;

    await Assert.That(result).IsEqualTo(c);
  }

  public static IEnumerable<(int a, int b, int c)> DataSource()
  {
    yield return (1, 1, 2);
    yield return (2, 1, 3);
    yield return (3, 1, 4);
  }
}
