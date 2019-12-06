using System.IO;
using System.Linq;

using Coverlet.Core;
using Newtonsoft.Json;
using Xunit;

namespace Coverlet.Integration.Tests
{
    public class Collectors : BaseTest
    {
        private ClonedTemplateProject PrepareTemplateProject()
        {
            ClonedTemplateProject clonedTemplateProject = CloneTemplateProject();
            UpdateNugeConfigtWithLocalPackageFolder(clonedTemplateProject.Path);
            AddCoverletCollectosRef(clonedTemplateProject.Path);
            return clonedTemplateProject;
        }

        private void AssertCollectorsInjection(ClonedTemplateProject clonedTemplateProject)
        {
            // Check out/in process collectors injection
            Assert.Contains("[coverlet]", File.ReadAllText(clonedTemplateProject.GetFiles("log.datacollector.*.txt").Single()));
            Assert.Contains("[coverlet]", File.ReadAllText(clonedTemplateProject.GetFiles("log.host.*.txt").Single()));
        }

        [Fact]
        public void TestVsTest_Test()
        {
            using ClonedTemplateProject clonedTemplateProject = PrepareTemplateProject();
            string runSettingsPath = AddCollectorRunsettingsFile(clonedTemplateProject.Path);
            Assert.True(DotnetCli($"test \"{clonedTemplateProject.Path}\" --collect:\"XPlat Code Coverage\" --diag:{Path.Combine(clonedTemplateProject.Path, "log.txt")}", out string standardOutput, out string standardError, clonedTemplateProject.Path), standardOutput);
            // We don't have any result to check because tests and code to instrument are in same assembly so we need to pass
            // IncludeTestAssembly=true we do it in other test
            Assert.Contains("Test Run Successful.", standardOutput);
            AssertCollectorsInjection(clonedTemplateProject);
        }

        [Fact]
        public void TestVsTest_Test_Settings()
        {
            using ClonedTemplateProject clonedTemplateProject = PrepareTemplateProject();
            string runSettingsPath = AddCollectorRunsettingsFile(clonedTemplateProject.Path);
            Assert.True(DotnetCli($"test \"{clonedTemplateProject.Path}\" --collect:\"XPlat Code Coverage\" --settings \"{runSettingsPath}\" --diag:{Path.Combine(clonedTemplateProject.Path, "log.txt")}", out string standardOutput, out string standardError), standardOutput);
            Assert.Contains("Test Run Successful.", standardOutput);
            AssertCoverage(clonedTemplateProject);
            AssertCollectorsInjection(clonedTemplateProject);
        }

        [Fact]
        public void TestVsTest_VsTest()
        {
            using ClonedTemplateProject clonedTemplateProject = PrepareTemplateProject();
            string runSettingsPath = AddCollectorRunsettingsFile(clonedTemplateProject.Path);
            Assert.True(DotnetCli($"publish {clonedTemplateProject.Path}", out string standardOutput, out string standardError), standardOutput);
            string publishedTestFile = clonedTemplateProject.GetFiles("*" + ClonedTemplateProject.AssemblyName + ".dll").Single(f => f.Contains("publish"));
            Assert.NotNull(publishedTestFile);
            Assert.True(DotnetCli($"vstest \"{publishedTestFile}\" --collect:\"XPlat Code Coverage\" --diag:{Path.Combine(clonedTemplateProject.Path, "log.txt")}", out standardOutput, out standardError), standardOutput);
            // We don't have any result to check because tests and code to instrument are in same assembly so we need to pass
            // IncludeTestAssembly=true we do it in other test
            Assert.Contains("Test Run Successful.", standardOutput);
            AssertCollectorsInjection(clonedTemplateProject);
        }

        [Fact]
        public void TestVsTest_VsTest_Settings()
        {
            using ClonedTemplateProject clonedTemplateProject = PrepareTemplateProject();
            string runSettingsPath = AddCollectorRunsettingsFile(clonedTemplateProject.Path);
            Assert.True(DotnetCli($"publish \"{clonedTemplateProject.Path}\"", out string standardOutput, out string standardError), standardOutput);
            string publishedTestFile = clonedTemplateProject.GetFiles("*" + ClonedTemplateProject.AssemblyName + ".dll").Single(f => f.Contains("publish"));
            Assert.NotNull(publishedTestFile);
            Assert.True(DotnetCli($"vstest \"{publishedTestFile}\" --collect:\"XPlat Code Coverage\" --ResultsDirectory:\"{clonedTemplateProject.Path}\" /settings:\"{runSettingsPath}\" --diag:{Path.Combine(clonedTemplateProject.Path, "log.txt")}", out standardOutput, out standardError), standardOutput);
            Assert.Contains("Test Run Successful.", standardOutput);
            AssertCoverage(clonedTemplateProject);
            AssertCollectorsInjection(clonedTemplateProject);
        }
    }
}
