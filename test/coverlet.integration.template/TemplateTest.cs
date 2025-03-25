using System;
using Xunit;

namespace Coverlet.Integration.Template
{
  public class TemplateTest
  {

    //public TemplateTest()
    //{
    //  Console.WriteLine("Hello World!");
    //}

    [Fact]
    public void Answer()
    {
      DeepThought dt = new DeepThought();
      Assert.Equal(42, dt.AnswerToTheUltimateQuestionOfLifeTheUniverseAndEverything());
    }
  }
}
