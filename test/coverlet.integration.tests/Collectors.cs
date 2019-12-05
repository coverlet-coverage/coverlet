using System.IO;
using System.Linq;

using Coverlet.Core;
using Newtonsoft.Json;
using Xunit;

namespace Coverlet.Integration.Tests
{
    public class Collectors : BaseTest
    {
        [Fact]
        public void TestVsTest_Test()
        {
            using ClonedTemplateProject clonedTemplateProject = CloneTemplateProject();
            UpdateNuget(clonedTemplateProject.Path);
            AddCollectorsRef(clonedTemplateProject.Path);
            string runSettingsPath = AddRunsettings(clonedTemplateProject.Path);
            Assert.True(DotnetCli($"test \"{clonedTemplateProject.Path}\" --collect:\"XPlat Code Coverage\" --diag:{Path.Combine(clonedTemplateProject.Path, "log.txt")}", out string standardOutput, out string standardError, clonedTemplateProject.Path), standardOutput);
            // We don't have any result to check because tests and code to instrument are in same assembly so we need to pass
            // IncludeTestAssembly=true we do it in other test
            Assert.Contains("Test Run Successful.", standardOutput);

            // Check out/in process collectors injection
            Assert.Contains("[coverlet]", File.ReadAllText(clonedTemplateProject.GetFiles("log.datacollector.*.txt").Single()));
            Assert.Contains("[coverlet]", File.ReadAllText(clonedTemplateProject.GetFiles("log.host.*.txt").Single()));
        }

        [Fact]
        public void TestVsTest_Test_Settings()
        {
            using ClonedTemplateProject clonedTemplateProject = CloneTemplateProject();
            UpdateNuget(clonedTemplateProject.Path);
            AddCollectorsRef(clonedTemplateProject.Path);
            string runSettingsPath = AddRunsettings(clonedTemplateProject.Path);
            Assert.True(DotnetCli($"test \"{clonedTemplateProject.Path}\" --collect:\"XPlat Code Coverage\" --settings \"{runSettingsPath}\" --diag:{Path.Combine(clonedTemplateProject.Path, "log.txt")}", out string standardOutput, out string standardError), standardOutput);
            Assert.Contains("Test Run Successful.", standardOutput);

            Modules modules = JsonConvert.DeserializeObject<Modules>(File.ReadAllText(clonedTemplateProject.GetFiles("coverage.json").Single()));
            modules
            .Document("DeepThought.cs")
            .Class("Coverlet.Integration.Template.DeepThought")
            .Method("System.Int32 Coverlet.Integration.Template.DeepThought::AnswerToTheUltimateQuestionOfLifeTheUniverseAndEverything()")
            .AssertLinesCovered((6, 1), (7, 1), (8, 1));

            // Check out/in process collectors injection
            Assert.Contains("[coverlet]", File.ReadAllText(clonedTemplateProject.GetFiles("log.datacollector.*.txt").Single()));
            Assert.Contains("[coverlet]", File.ReadAllText(clonedTemplateProject.GetFiles("log.host.*.txt").Single()));
        }

        [Fact]
        public void TestVsTest_VsTest()
        {
            using ClonedTemplateProject clonedTemplateProject = CloneTemplateProject();
            UpdateNuget(clonedTemplateProject.Path);
            AddCollectorsRef(clonedTemplateProject.Path);
            string runSettingsPath = AddRunsettings(clonedTemplateProject.Path);
            Assert.True(DotnetCli($"publish {clonedTemplateProject.Path}", out string standardOutput, out string standardError), standardOutput);
            string publishedTestFile = clonedTemplateProject.GetFiles("*" + ClonedTemplateProject.AssemblyName + ".dll").Single(f => f.Contains("publish"));
            Assert.NotNull(publishedTestFile);
            Assert.True(DotnetCli($"vstest \"{publishedTestFile}\" --collect:\"XPlat Code Coverage\" --diag:{Path.Combine(clonedTemplateProject.Path, "log.txt")}", out standardOutput, out standardError), standardOutput);
            // We don't have any result to check because tests and code to instrument are in same assembly so we need to pass
            // IncludeTestAssembly=true we do it in other test
            Assert.Contains("Test Run Successful.", standardOutput);

            // Check out/in process collectors injection
            Assert.Contains("[coverlet]", File.ReadAllText(clonedTemplateProject.GetFiles("log.datacollector.*.txt").Single()));
            Assert.Contains("[coverlet]", File.ReadAllText(clonedTemplateProject.GetFiles("log.host.*.txt").Single()));
        }

        [Fact]
        public void TestVsTest_VsTest_Settings()
        {
            using ClonedTemplateProject clonedTemplateProject = CloneTemplateProject();
            UpdateNuget(clonedTemplateProject.Path);
            AddCollectorsRef(clonedTemplateProject.Path);
            string runSettingsPath = AddRunsettings(clonedTemplateProject.Path);
            Assert.True(DotnetCli($"publish \"{clonedTemplateProject.Path}\"", out string standardOutput, out string standardError), standardOutput);
            string publishedTestFile = clonedTemplateProject.GetFiles("*" + ClonedTemplateProject.AssemblyName + ".dll").Single(f => f.Contains("publish"));
            Assert.NotNull(publishedTestFile);
            Assert.True(DotnetCli($"vstest \"{publishedTestFile}\" --collect:\"XPlat Code Coverage\" --ResultsDirectory:\"{clonedTemplateProject.Path}\" /settings:\"{runSettingsPath}\" --diag:{Path.Combine(clonedTemplateProject.Path, "log.txt")}", out standardOutput, out standardError), standardOutput);
            Assert.Contains("Test Run Successful.", standardOutput);

            Modules modules = JsonConvert.DeserializeObject<Modules>(File.ReadAllText(clonedTemplateProject.GetFiles("coverage.json").Single()));
            modules
            .Document("DeepThought.cs")
            .Class("Coverlet.Integration.Template.DeepThought")
            .Method("System.Int32 Coverlet.Integration.Template.DeepThought::AnswerToTheUltimateQuestionOfLifeTheUniverseAndEverything()")
            .AssertLinesCovered((6, 1), (7, 1), (8, 1));

            // Check out/in process collectors injection
            Assert.Contains("[coverlet]", File.ReadAllText(clonedTemplateProject.GetFiles("log.datacollector.*.txt").Single()));
            Assert.Contains("[coverlet]", File.ReadAllText(clonedTemplateProject.GetFiles("log.host.*.txt").Single()));
        }
    }
}
