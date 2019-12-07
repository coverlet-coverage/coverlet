using System.IO;

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
            Assert.True(File.Exists(Path.Combine(clonedTemplateProject.ProjectRootPath, "coverage.json")));
            AssertCoverage(clonedTemplateProject);
        }

        [Fact]
        public void TestMsbuild_NoCoverletOutput()
        {
            using ClonedTemplateProject clonedTemplateProject = PrepareTemplateProject();
            Assert.True(DotnetCli($"test \"{clonedTemplateProject.ProjectRootPath}\" /p:CollectCoverage=true /p:Include=\"[{ClonedTemplateProject.AssemblyName}]*DeepThought\" /p:IncludeTestAssembly=true", out string standardOutput, out string standardError), standardOutput);
            Assert.Contains("Test Run Successful.", standardOutput);
            Assert.Contains("| coverletsamplelib.integration.template | 100% | 100%   | 100%   |", standardOutput);
            Assert.True(File.Exists(Path.Combine(clonedTemplateProject.ProjectRootPath, "coverage.json")));
            AssertCoverage(clonedTemplateProject);
        }

        [Fact]
        public void TestMsbuild_CoverletOutput_Folder_FileNameWithoutExtension()
        {
            using ClonedTemplateProject clonedTemplateProject = PrepareTemplateProject();
            Assert.True(DotnetCli($"test \"{clonedTemplateProject.ProjectRootPath}\" /p:CollectCoverage=true /p:Include=\"[{ClonedTemplateProject.AssemblyName}]*DeepThought\" /p:IncludeTestAssembly=true /p:CoverletOutput=\"{clonedTemplateProject.ProjectRootPath}\"\\file", out string standardOutput, out string standardError), standardOutput);
            Assert.Contains("Test Run Successful.", standardOutput);
            Assert.Contains("| coverletsamplelib.integration.template | 100% | 100%   | 100%   |", standardOutput);
            Assert.True(File.Exists(Path.Combine(clonedTemplateProject.ProjectRootPath, "file.json")));
            AssertCoverage(clonedTemplateProject, "file.json");
        }

        [Fact]
        public void TestMsbuild_CoverletOutput_Folder_FileNameExtension()
        {
            using ClonedTemplateProject clonedTemplateProject = PrepareTemplateProject();
            Assert.True(DotnetCli($"test \"{clonedTemplateProject.ProjectRootPath}\" /p:CollectCoverage=true /p:Include=\"[{ClonedTemplateProject.AssemblyName}]*DeepThought\" /p:IncludeTestAssembly=true /p:CoverletOutput=\"{clonedTemplateProject.ProjectRootPath}\"\\file.ext", out string standardOutput, out string standardError), standardOutput);
            Assert.Contains("Test Run Successful.", standardOutput);
            Assert.Contains("| coverletsamplelib.integration.template | 100% | 100%   | 100%   |", standardOutput);
            Assert.True(File.Exists(Path.Combine(clonedTemplateProject.ProjectRootPath, "file.ext.json")));
            AssertCoverage(clonedTemplateProject, "file.ext.json");
        }

        [Fact]
        public void TestMsbuild_CoverletOutput_Folder_FileNameWithDoubleExtension()
        {
            using ClonedTemplateProject clonedTemplateProject = PrepareTemplateProject();
            Assert.True(DotnetCli($"test \"{clonedTemplateProject.ProjectRootPath}\" /p:CollectCoverage=true /p:Include=\"[{ClonedTemplateProject.AssemblyName}]*DeepThought\" /p:IncludeTestAssembly=true /p:CoverletOutput=\"{clonedTemplateProject.ProjectRootPath}\"\\file.ext1.ext2", out string standardOutput, out string standardError), standardOutput);
            Assert.Contains("Test Run Successful.", standardOutput);
            Assert.Contains("| coverletsamplelib.integration.template | 100% | 100%   | 100%   |", standardOutput);
            Assert.True(File.Exists(Path.Combine(clonedTemplateProject.ProjectRootPath, "file.ext1.ext2.json")));
            AssertCoverage(clonedTemplateProject, "file.ext1.ext2.json");
        }

        [Fact]
        public void Test_MultipleTargetFrameworkReport_NoCoverletOutput()
        {
            using ClonedTemplateProject clonedTemplateProject = PrepareTemplateProject();
            string[] targetFrameworks = new string[] { "netcoreapp2.2", "netcoreapp2.1" };
            UpdateProjectTargetFramework(clonedTemplateProject, targetFrameworks);
            Assert.True(DotnetCli($"test \"{clonedTemplateProject.ProjectRootPath}\" /p:CollectCoverage=true /p:Include=\"[{ClonedTemplateProject.AssemblyName}]*DeepThought\" /p:IncludeTestAssembly=true", out string standardOutput, out string standardError, clonedTemplateProject.ProjectRootPath!), standardOutput);
            Assert.Contains("Test Run Successful.", standardOutput);
            Assert.Contains("| coverletsamplelib.integration.template | 100% | 100%   | 100%   |", standardOutput);

            foreach (string targetFramework in targetFrameworks)
            {
                Assert.True(File.Exists(Path.Combine(clonedTemplateProject.ProjectRootPath, $"coverage.{targetFramework}.json")));
            }

            AssertCoverage(clonedTemplateProject, "coverage.*.json");
        }

        [Fact]
        public void Test_MultipleTargetFrameworkReport_CoverletOutput_Folder()
        {
            using ClonedTemplateProject clonedTemplateProject = PrepareTemplateProject();
            string[] targetFrameworks = new string[] { "netcoreapp2.2", "netcoreapp2.1" };
            UpdateProjectTargetFramework(clonedTemplateProject, targetFrameworks);
            Assert.True(DotnetCli($"test \"{clonedTemplateProject.ProjectRootPath}\" /p:CollectCoverage=true /p:Include=\"[{ClonedTemplateProject.AssemblyName}]*DeepThought\" /p:IncludeTestAssembly=true /p:CoverletOutput=\"{clonedTemplateProject.ProjectRootPath}\"\\", out string standardOutput, out string standardError, clonedTemplateProject.ProjectRootPath!), standardOutput);
            Assert.Contains("Test Run Successful.", standardOutput);
            Assert.Contains("| coverletsamplelib.integration.template | 100% | 100%   | 100%   |", standardOutput);

            foreach (string targetFramework in targetFrameworks)
            {
                Assert.True(File.Exists(Path.Combine(clonedTemplateProject.ProjectRootPath, $"coverage.{targetFramework}.json")));
            }

            AssertCoverage(clonedTemplateProject, "coverage.*.json");
        }

        [Fact]
        public void Test_MultipleTargetFrameworkReport_CoverletOutput_Folder_FileNameWithoutExtension()
        {
            using ClonedTemplateProject clonedTemplateProject = PrepareTemplateProject();
            string[] targetFrameworks = new string[] { "netcoreapp2.2", "netcoreapp2.1" };
            UpdateProjectTargetFramework(clonedTemplateProject, targetFrameworks);
            Assert.True(DotnetCli($"test \"{clonedTemplateProject.ProjectRootPath}\" /p:CollectCoverage=true /p:Include=\"[{ClonedTemplateProject.AssemblyName}]*DeepThought\" /p:IncludeTestAssembly=true /p:CoverletOutput=\"{clonedTemplateProject.ProjectRootPath}\"\\file", out string standardOutput, out string standardError, clonedTemplateProject.ProjectRootPath!), standardOutput);
            Assert.Contains("Test Run Successful.", standardOutput);
            Assert.Contains("| coverletsamplelib.integration.template | 100% | 100%   | 100%   |", standardOutput);

            foreach (string targetFramework in targetFrameworks)
            {
                Assert.True(File.Exists(Path.Combine(clonedTemplateProject.ProjectRootPath, $"file.{targetFramework}.json")));
            }

            AssertCoverage(clonedTemplateProject, "file.*.json");
        }

        [Fact]
        public void Test_MultipleTargetFrameworkReport_CoverletOutput_Folder_FileNameWithExtension()
        {
            using ClonedTemplateProject clonedTemplateProject = PrepareTemplateProject();
            string[] targetFrameworks = new string[] { "netcoreapp2.2", "netcoreapp2.1" };
            UpdateProjectTargetFramework(clonedTemplateProject, targetFrameworks);
            Assert.True(DotnetCli($"test \"{clonedTemplateProject.ProjectRootPath}\" /p:CollectCoverage=true /p:Include=\"[{ClonedTemplateProject.AssemblyName}]*DeepThought\" /p:IncludeTestAssembly=true /p:CoverletOutput=\"{clonedTemplateProject.ProjectRootPath}\"\\file.ext", out string standardOutput, out string standardError, clonedTemplateProject.ProjectRootPath!), standardOutput);
            Assert.Contains("Test Run Successful.", standardOutput);
            Assert.Contains("| coverletsamplelib.integration.template | 100% | 100%   | 100%   |", standardOutput);

            foreach (string targetFramework in targetFrameworks)
            {
                Assert.True(File.Exists(Path.Combine(clonedTemplateProject.ProjectRootPath, $"file.{targetFramework}.ext.json")));
            }

            AssertCoverage(clonedTemplateProject, "file.*.ext.json");
        }

        [Fact]
        public void Test_MultipleTargetFrameworkReport_CoverletOutput_Folder_FileNameWithDoubleExtension()
        {
            using ClonedTemplateProject clonedTemplateProject = PrepareTemplateProject();
            string[] targetFrameworks = new string[] { "netcoreapp2.2", "netcoreapp2.1" };
            UpdateProjectTargetFramework(clonedTemplateProject, targetFrameworks);
            Assert.True(DotnetCli($"test \"{clonedTemplateProject.ProjectRootPath}\" /p:CollectCoverage=true /p:Include=\"[{ClonedTemplateProject.AssemblyName}]*DeepThought\" /p:IncludeTestAssembly=true /p:CoverletOutput=\"{clonedTemplateProject.ProjectRootPath}\"\\file.ext1.ext2", out string standardOutput, out string standardError, clonedTemplateProject.ProjectRootPath!), standardOutput);
            Assert.Contains("Test Run Successful.", standardOutput);
            Assert.Contains("| coverletsamplelib.integration.template | 100% | 100%   | 100%   |", standardOutput);

            foreach (string targetFramework in targetFrameworks)
            {
                Assert.True(File.Exists(Path.Combine(clonedTemplateProject.ProjectRootPath, $"file.ext1.{targetFramework}.ext2.json")));
            }

            AssertCoverage(clonedTemplateProject, "file.ext1.*.ext2.json");
        }
    }
}
