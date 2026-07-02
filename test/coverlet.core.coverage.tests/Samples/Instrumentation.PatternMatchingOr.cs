// Remember to use full name because adding new using directives change line numbers

namespace Coverlet.Core.CoverageSamples.Tests
{
  public class PatternMatchingOr
  {
    public bool PatternMatching(string text)
    {
      return text is "hello" or "world";
    }
  }
}
