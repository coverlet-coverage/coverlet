// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ConsoleTables;
using Xunit;

namespace coverlet.msbuild.tasks.tests
{
  public class ConsoleTableTests
  {
    [Fact]
    public void Write_DefaultFormat_WritesToConsole()
    {
      // Arrange
      var columns = new[] { "", "Line", "Branch", "Method" };
      var table = new ConsoleTable(columns);
      table.AddRow("Value1", "Value2", "Value3", "Value4");

      var consoleOutput = new StringWriter();
      Console.SetOut(consoleOutput);

      // Act
      table.Write(Format.Default);

      // Assert
      var output = consoleOutput.ToString();
      Assert.Contains("-------------------------------------", output);
      Assert.Contains("|        | Line   | Branch | Method |", output);
      Assert.Contains("| Value1 | Value2 | Value3 | Value4 |", output);
    }

    [Fact]
    public void Write_MarkDownFormat_WritesToConsole()
    {
      // Arrange
      var columns = new[] { "", "Line", "Branch", "Method" };
      var table = new ConsoleTable(columns);
      table.AddRow("Value1", "Value2", "Value3", "Value4");

      var consoleOutput = new StringWriter();
      Console.SetOut(consoleOutput);

      // Act
      table.Write(Format.MarkDown);

      // Assert
      var output = consoleOutput.ToString();
      Assert.Contains("|        | Line   | Branch | Method |", output);
      Assert.Contains("|--------|--------|--------|--------|", output);
      Assert.Contains("| Value1 | Value2 | Value3 | Value4 |", output);
    }

    [Fact]
    public void Write_AlternativeFormat_WritesToConsole()
    {
      // Arrange
      var columns = new[] { "", "Line", "Branch", "Method" };
      var table = new ConsoleTable(columns);
      table.AddRow("Value1", "Value2", "Value3", "Value4");

      var consoleOutput = new StringWriter();
      Console.SetOut(consoleOutput);

      // Act
      table.Write(Format.Alternative);

      // Assert
      var output = consoleOutput.ToString();
      Assert.Contains("+--------+--------+--------+--------+", output);
      Assert.Contains("|        | Line   | Branch | Method |", output);
      Assert.Contains("| Value1 | Value2 | Value3 | Value4 |", output);
    }

    [Fact]
    public void Write_MinimalFormat_WritesToConsole()
    {
      // Arrange
      var columns = new[] { "", "Line", "Branch", "Method" };
      var table = new ConsoleTable(columns);
      table.AddRow("Value1", "Value2", "Value3", "Value4");

      var consoleOutput = new StringWriter();
      Console.SetOut(consoleOutput);

      // Act
      table.Write(Format.Minimal);

      // Assert
      var output = consoleOutput.ToString();
      Assert.Contains("        Line    Branch  Method", output);
      Assert.Contains("------------------------------", output);
      Assert.Contains("Value1  Value2  Value3  Value4", output);
    }

    [Fact]
    public void Write_InvalidFormat_ThrowsArgumentOutOfRangeException()
    {
      // Arrange
      var columns = new[] { "", "Line", "Branch", "Method" };
      var table = new ConsoleTable(columns);

      // Act & Assert
      Assert.Throws<ArgumentOutOfRangeException>(() => table.Write((Format)999));
    }
  }
}
