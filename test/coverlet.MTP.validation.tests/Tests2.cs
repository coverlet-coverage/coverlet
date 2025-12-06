// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace coverlet.MTP.validation.tests;

[Arguments("Hello")]
[Arguments("World")]
public class MoreTests(string title)
{
  [Test]
  public void ClassLevelDataRow()
  {
    Console.WriteLine(title);
    Console.WriteLine(@"Did I forget that data injection works on classes too?");
  }

  [Test]
  [MatrixDataSource]
  public void Matrices(
      [Matrix(1, 2, 3)] int a,
      [Matrix(true, false)] bool b,
      [Matrix("A", "B", "C")] string c)
  {
    Console.WriteLine(@"A new test will be created for each data row, whether it's on the class or method level!");

    Console.WriteLine(@"Oh and this is a matrix test. That means all combinations of inputs are attempted.");
  }
}
