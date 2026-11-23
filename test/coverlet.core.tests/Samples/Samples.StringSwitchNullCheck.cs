namespace Coverlet.Core.Samples.Tests
{
  // Sample for https://github.com/coverlet-coverage/coverlet/issues/2080
  // A string switch with a null arm, an empty-string arm and another constant checks the
  // value for null twice in a row: the second null outcome can't happen.
  public class StringSwitchNullCheck
  {
    public static int Classify(string text) => text switch
    {
      null => 0,
      "" => 1,
      "a" => 2,
      _ => 3
    };

    public static int ClassifyLocal(string text)
    {
      string value = text?.Trim();
      return value switch
      {
        null => 0,
        "" => 1,
        "a" => 2,
        _ => 3
      };
    }

    // No empty-string arm: the value is checked for null once.
    public static int ClassifyWithoutEmpty(string text) => text switch
    {
      null => 0,
      "b" => 1,
      "a" => 2,
      _ => 3
    };
  }
}
