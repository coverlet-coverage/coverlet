using System;
using System.IO;
using System.Linq;

using Xunit;

namespace Coverlet.Integration.Tests
{
    public class TestSDK_16_2_0 : Collectors
    {
        public TestSDK_16_2_0()
        {
            TestSDKVersion = "16.2.0";
        }

        private protected override void AssertCollectorsInjection(ClonedTemplateProject clonedTemplateProject)
        {
            // Check out/in process collectors injection
            Assert.Contains("[coverlet]", File.ReadAllText(clonedTemplateProject.GetFiles("log.datacollector.*.txt").Single()));

            // There is a bug in this SDK version https://github.com/microsoft/vstest/pull/2221
            // in-proc coverlet.collector.dll collector with version != 1.0.0.0 won't be loaded
            // Assert.Contains("[coverlet]", File.ReadAllText(clonedTemplateProject.GetFiles("log.host.*.txt").Single()));
        }
    }

    public class TestSDK_16_5_0 : Collectors
    {
        public TestSDK_16_5_0()
        {
            TestSDKVersion = "16.5.0";
        }
    }

    public class TestSDK_Preview : Collectors
    {
        public TestSDK_Preview()
        {
            TestSDKVersion = "16.5.0-preview-20200203-01";
        }
    }

    public abstract class Collectors : BaseTest
    {
        private string _buildConfiguration;

        public Collectors()
        {
            _buildConfiguration = GetAssemblyBuildConfiguration().ToString();
        }

        protected string? TestSDKVersion { get; set; }

        private ClonedTemplateProject PrepareTemplateProject()
        {
            if (TestSDKVersion is null)
            {
                throw new ArgumentNullException("Invalid TestSDKVersion");
            }

            ClonedTemplateProject clonedTemplateProject = CloneTemplateProject(testSDKVersion: TestSDKVersion);
            UpdateNugeConfigtWithLocalPackageFolder(clonedTemplateProject.ProjectRootPath!);
            AddCoverletCollectosRef(clonedTemplateProject.ProjectRootPath!);
            return clonedTemplateProject;
        }

        private protected virtual void AssertCollectorsInjection(ClonedTemplateProject clonedTemplateProject)
        {
            // Check out/in process collectors injection
            Assert.Contains("[coverlet]Initializing CoverletCoverageDataCollector with configuration:", File.ReadAllText(clonedTemplateProject.GetFiles("log.datacollector.*.txt").Single()));
            Assert.Contains("[coverlet]Initialize CoverletInProcDataCollector", File.ReadAllText(clonedTemplateProject.GetFiles("log.host.*.txt").Single()));
        }

        [Fact]
        public void TestVsTest_Test()
        {
            using ClonedTemplateProject clonedTemplateProject = PrepareTemplateProject();
            Assert.True(DotnetCli($"test -c {_buildConfiguration} \"{clonedTemplateProject.ProjectRootPath}\" --collect:\"XPlat Code Coverage\" --diag:{Path.Combine(clonedTemplateProject.ProjectRootPath, "log.txt")}", out string standardOutput, out string standardError, clonedTemplateProject.ProjectRootPath!), standardOutput);
            // We don't have any result to check because tests and code to instrument are in same assembly so we need to pass
            // IncludeTestAssembly=true we do it in other test
            Assert.Contains("Passed!", standardOutput);
            AssertCollectorsInjection(clonedTemplateProject);
        }

        [Fact]
        public void TestVsTest_Test_Settings()
        {
            using ClonedTemplateProject clonedTemplateProject = PrepareTemplateProject();
            string runSettingsPath = AddCollectorRunsettingsFile(clonedTemplateProject.ProjectRootPath!);
            Assert.True(DotnetCli($"test -c {_buildConfiguration} \"{clonedTemplateProject.ProjectRootPath}\" --collect:\"XPlat Code Coverage\" --settings \"{runSettingsPath}\" --diag:{Path.Combine(clonedTemplateProject.ProjectRootPath, "log.txt")}", out string standardOutput, out string standardError), standardOutput);
            Assert.Contains("Passed!", standardOutput);
            AssertCoverage(clonedTemplateProject);
            AssertCollectorsInjection(clonedTemplateProject);
        }

        [Fact]
        public void TestVsTest_VsTest()
        {
            using ClonedTemplateProject clonedTemplateProject = PrepareTemplateProject();
            string runSettingsPath = AddCollectorRunsettingsFile(clonedTemplateProject.ProjectRootPath!);
            Assert.True(DotnetCli($"publish -c {_buildConfiguration} {clonedTemplateProject.ProjectRootPath}", out string standardOutput, out string standardError), standardOutput);
            string publishedTestFile = clonedTemplateProject.GetFiles("*" + ClonedTemplateProject.AssemblyName + ".dll").Single(f => f.Contains("publish"));
            Assert.NotNull(publishedTestFile);
            Assert.True(DotnetCli($"vstest \"{publishedTestFile}\" --collect:\"XPlat Code Coverage\" --diag:{Path.Combine(clonedTemplateProject.ProjectRootPath, "log.txt")}", out standardOutput, out standardError), standardOutput);
            // We don't have any result to check because tests and code to instrument are in same assembly so we need to pass
            // IncludeTestAssembly=true we do it in other test
            Assert.Contains("Passed!", standardOutput);
            AssertCollectorsInjection(clonedTemplateProject);
        }

        [Fact]
        public void TestVsTest_VsTest_Settings()
        {
            using ClonedTemplateProject clonedTemplateProject = PrepareTemplateProject();
            string runSettingsPath = AddCollectorRunsettingsFile(clonedTemplateProject.ProjectRootPath!);
            Assert.True(DotnetCli($"publish -c {_buildConfiguration} \"{clonedTemplateProject.ProjectRootPath}\"", out string standardOutput, out string standardError), standardOutput);
            string publishedTestFile = clonedTemplateProject.GetFiles("*" + ClonedTemplateProject.AssemblyName + ".dll").Single(f => f.Contains("publish"));
            Assert.NotNull(publishedTestFile);
            Assert.True(DotnetCli($"vstest \"{publishedTestFile}\" --collect:\"XPlat Code Coverage\" --ResultsDirectory:\"{clonedTemplateProject.ProjectRootPath}\" /settings:\"{runSettingsPath}\" --diag:{Path.Combine(clonedTemplateProject.ProjectRootPath, "log.txt")}", out standardOutput, out standardError), standardOutput);
            Assert.Contains("Passed!", standardOutput);
            AssertCoverage(clonedTemplateProject);
            AssertCollectorsInjection(clonedTemplateProject);
        }
    }
}
