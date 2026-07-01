// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Coverlet.Core.Samples.Tests
{
  /// <summary>
  /// Sample class for testing pattern matching with `or` to reproduce issue #1969.
  /// When using pattern matching `is ... or ...` the compiler generates an intermediate
  /// short-circuit branch that cannot be exercised during normal execution, causing
  /// false-positive uncovered branch reports.
  /// </summary>
  public static class PatternMatchingOr
  {
    /// <summary>
    /// Demonstrates the traditional `||` operator approach.
    /// Expected: 100% branch coverage (2/2 conditions covered).
    /// </summary>
    public static bool OrOperator(string text)
    {
      return text == "hello" || text == "world";
    }

    /// <summary>
    /// Demonstrates the pattern matching `or` approach.
    /// Issue: Compiler generates a synthetic short-circuit branch.
    /// Expected: 100% branch coverage (should match OrOperator).
    /// Before fix: 75% branch coverage (3/4 conditions covered - one is unreachable).
    /// </summary>
    public static bool PatternMatchingOrMethod(string text)
    {
      return text is "hello" or "world";
    }

    /// <summary>
    /// Pattern matching with `or` for multiple values.
    /// </summary>
    public static bool PatternMatchingOrMultiple(string text)
    {
      return text is "apple" or "banana" or "cherry";
    }

    /// <summary>
    /// Pattern matching with `or` and property patterns.
    /// </summary>
    public static bool PatternMatchingOrProperty(Person person)
    {
      return person is { Name: "Alice" } or { Name: "Bob" };
    }

    /// <summary>
    /// Pattern matching with `or` and type patterns.
    /// </summary>
    public static bool PatternMatchingOrTypes(object value)
    {
      return value is string or int or double;
    }

    /// <summary>
    /// Pattern matching with `or` combined with `and` (relational pattern).
    /// </summary>
    public static bool PatternMatchingOrWithAnd(int value)
    {
      return value is (>= 1 and <= 5) or (>= 10 and <= 15);
    }

    /// <summary>
    /// Nested pattern matching with `or`.
    /// </summary>
    public static bool PatternMatchingOrNested(object value)
    {
      return value switch
      {
        string s when s is "test" or "demo" => true,
        int i when i is 1 or 2 or 3 => true,
        _ => false
      };
    }

    public class Person
    {
      public string Name { get; set; }
      public int Age { get; set; }
    }
  }
}
