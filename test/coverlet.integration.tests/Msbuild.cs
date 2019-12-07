using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Xunit;


namespace Coverlet.Integration.Tests
{
    public class Msbuild : BaseTest
    {
        private ClonedTemplateProject PrepareTemplateProject()
        {
            ClonedTemplateProject clonedTemplateProject = CloneTemplateProject(false);
            UpdateNugeConfigtWithLocalPackageFolder(clonedTemplateProject.ProjectRootPath!);
            AddCoverletMsbuildRef(clonedTemplateProject.ProjectRootPath!);
            return clonedTemplateProject;
        }

        [Fact]
        public void TestMsbuild()
        {
            using ClonedTemplateProject clonedTemplateProject = PrepareTemplateProject();
            Assert.True(DotnetCli($"test \"{clonedTemplateProject.ProjectRootPath}\" /p:CollectCoverage=true /p:Include=\"[{ClonedTemplateProject.AssemblyName}]*DeepThought\" /p:IncludeTestAssembly=true /p:CoverletOutput=\"{clonedTemplateProject.ProjectRootPath}\"\\", out string standardOutput, out string standardError), standardOutput);
            Assert.Contains("Test Run Successful.", standardOutput);
            Assert.Contains("| coverletsamplelib.integration.template | 100% | 100%   | 100%   |", standardOutput);
            AssertCoverage(clonedTemplateProject);
        }

        [Fact]
        public void Test_MultipleTargetFrameworkReport()
        {
            using ClonedTemplateProject clonedTemplateProject = PrepareTemplateProject();
            PinSDK(clonedTemplateProject, "3.1.100");
            string[] targetFrameworks = new string[] { "netcoreapp2.2", "netcoreapp3.1" };
            UpdateProjectTargetFramework(clonedTemplateProject, targetFrameworks);

            Assert.True(DotnetCli($"test \"{clonedTemplateProject.ProjectRootPath}\" /p:CollectCoverage=true /p:Include=\"[{ClonedTemplateProject.AssemblyName}]*DeepThought\" /p:IncludeTestAssembly=true /p:CoverletOutput=\"{clonedTemplateProject.ProjectRootPath}\"\\", out string standardOutput, out string standardError), standardOutput);
            Assert.Contains("Test Run Successful.", standardOutput);
            Assert.Contains("| coverletsamplelib.integration.template | 100% | 100%   | 100%   |", standardOutput);

            foreach (string targetFramework in targetFrameworks)
            {
                Assert.True(File.Exists(Path.Combine(clonedTemplateProject.ProjectRootPath, $"coverage.{targetFramework}.json")));
            }

            AssertCoverage(clonedTemplateProject);
        }
    }
}
