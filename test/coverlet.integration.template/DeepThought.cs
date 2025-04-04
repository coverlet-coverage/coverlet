namespace Coverlet.Integration.Template
{
  public class DeepThought
  {
    public int AnswerToTheUltimateQuestionOfLifeTheUniverseAndEverything()
    {
      return 42;
    }

    // This method is not covered by any test
    // It is here to demonstrate how Coverlet will report on untested code
    // required for Coverlet.Integration.Tests.DotnetGlobalTools.StandAloneThreshold
    // required for Coverlet.Integration.Tests.DotnetGlobalTools.DotnetToolThreshold
    public void TheUntestedMethod()
    {
#pragma warning disable CS0219 // Variable is assigned but its value is never used
      string s = "this will never be covered by any test";
#pragma warning restore CS0219 // Variable is assigned but its value is never used
    }
  }
}
