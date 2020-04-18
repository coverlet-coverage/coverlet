using Xunit;

namespace Coverlet.Integration.DeterministicBuild
{
    public class TemplateTest
    {
        [Fact]
        public void Answer()
        {
            DeepThought dt = new DeepThought();
            Assert.Equal(42, dt.AnswerToTheUltimateQuestionOfLifeTheUniverseAndEverything());
        }
    }
}
