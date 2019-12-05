using System.IO;
using System.Linq;

using Coverlet.Core;
using Newtonsoft.Json;
using Xunit;


namespace Coverlet.Integration.Tests
{
    public class Msbuild : BaseTest
    {
        [Fact]
        public void TestMsbuild()
        {
            using ClonedTemplateProject clonedTemplateProject = CloneTemplateProject();
            UpdateNuget(clonedTemplateProject.Path);
            AddMsbuildRef(clonedTemplateProject.Path);
            Assert.True(DotnetCli($"test \"{clonedTemplateProject.Path}\" /p:CollectCoverage=true /p:Include=\"[{ClonedTemplateProject.AssemblyName}]*DeepThought\" /p:IncludeTestAssembly=true /p:CoverletOutput=\"{clonedTemplateProject.Path}\"\\", out string standardOutput, out string standardError), standardOutput);
            Assert.Contains("Test Run Successful.", standardOutput);
            Assert.Contains("| coverletsamplelib.integration.template | 100% | 100%   | 100%   |", standardOutput);

            Modules modules = JsonConvert.DeserializeObject<Modules>(File.ReadAllText(clonedTemplateProject.GetFiles("coverage.json").Single()));
            modules
            .Document("DeepThought.cs")
            .Class("Coverlet.Integration.Template.DeepThought")
            .Method("System.Int32 Coverlet.Integration.Template.DeepThought::AnswerToTheUltimateQuestionOfLifeTheUniverseAndEverything()")
            .AssertLinesCovered((6, 1), (7, 1), (8, 1));
        }
    }
}
