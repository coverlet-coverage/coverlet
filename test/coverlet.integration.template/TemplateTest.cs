// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace Coverlet.Integration.Template
{
    public class TemplateTest
    {
        [Fact]
        public void Answer()
        {
            var dt = new DeepThought();
            Assert.Equal(42, dt.AnswerToTheUltimateQuestionOfLifeTheUniverseAndEverything());
        }
    }
}
