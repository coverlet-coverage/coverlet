// Debug.Assert is [Conditional("DEBUG")]: define the symbol so that the calls below are
// compiled in every build configuration of the test project.
#define DEBUG

using System.Diagnostics;

namespace Coverlet.Core.Samples.Tests
{
  // Sample for https://github.com/coverlet-coverage/coverlet/issues/2087
  // An interpolated message of Debug.Assert is lowered to an AssertInterpolatedStringHandler,
  // whose constructor sets shouldAppend only when the condition is false, followed by a
  // compiler-generated branch on shouldAppend that formats the message.
  public class DebugAssertInterpolation
  {
    public static int Assert(int value)
    {
      Debug.Assert(value >= 0, $"value={value}");
      return value;
    }

    public static int AssertTwice(int value, string name)
    {
      Debug.Assert(value >= 0, $"value={value}");
      Debug.Assert(name != null, $"name of {value} is null");
      return value;
    }

    // The same message as an ordinary string has no branch at all.
    public static int AssertWithFormattedMessage(int value)
    {
      Debug.Assert(value >= 0, string.Format("value={0}", value));
      return value;
    }

    // A user-written branch next to the assertion must still be reported.
    public static int AssertAndBranch(int value)
    {
      Debug.Assert(value >= 0, $"value={value}");
      return value > 10 ? 10 : value;
    }
  }
}
