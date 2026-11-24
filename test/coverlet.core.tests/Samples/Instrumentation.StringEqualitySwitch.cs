// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Coverlet.Core.Samples.Tests
{
  /// <summary>
  /// String switches whose arms each have their own target. In Debug the last comparison is a
  /// `brtrue` followed by an unconditional jump to the default arm, the same shape as the last
  /// alternative of a pattern-matching `or`, but it is a real decision.
  /// </summary>
  public static class StringEqualitySwitch
  {
    public static int SwitchExpression(string text) => text switch
    {
      "a" => 2,
      _ => 3
    };

    public static int SwitchStatement(string text)
    {
      switch (text)
      {
        case "a":
          return 2;
        default:
          return 3;
      }
    }

    public static int SwitchExpressionWithTwoConstants(string text) => text switch
    {
      "a" => 1,
      "b" => 2,
      _ => 3
    };

    public static bool NullOrConstant(string text) => text is null or "a";

    public static int SwitchArmWithOr(string text) => text switch
    {
      "a" or "b" => 1,
      _ => 3
    };
  }
}
