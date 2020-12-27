using System.IO;
using System.Linq;
using Xunit;

namespace Coverlet.Integration.Tests
{
    public class Msbuild : BaseTest
    {
        private string _buildConfiguration;

        public Msbuild()
        {
            _buildConfiguration = GetAssemblyBuildConfiguration().ToString();
        }

        private ClonedTemplateProject PrepareTemplateProject()
        {
            ClonedTemplateProject clonedTemplateProject = CloneTemplateProject();
            UpdateNugeConfigtWithLocalPackageFolder(clonedTemplateProject.ProjectRootPath!);
            AddCoverletMsbuildRef(clonedTemplateProject.ProjectRootPath!);
            return clonedTemplateProject;
        }

        [Fact]
        public void TestMsbuild()
        {
            using ClonedTemplateProject clonedTemplateProject = PrepareTemplateProject();
            Assert.True(DotnetCli($"test -c {_buildConfiguration} \"{clonedTemplateProject.ProjectRootPath}\" /p:CollectCoverage=true /p:Include=\"[{ClonedTemplateProject.AssemblyName}]*DeepThought\" /p:IncludeTestAssembly=true /p:CoverletOutput=\"{clonedTemplateProject.ProjectRootPath}\"\\", out string standardOutput, out string standardError), standardOutput);
            Assert.Contains("Passed!", standardOutput);
            Assert.Contains("| coverletsamplelib.integration.template | 100% | 100%   | 100%   |", standardOutput);
            Assert.True(File.Exists(Path.Combine(clonedTemplateProject.ProjectRootPath, "coverage.json")));
            AssertCoverage(clonedTemplateProject);
        }

        [Fact]
        public void TestMsbuild_NoCoverletOutput()
        {
            using ClonedTemplateProject clonedTemplateProject = PrepareTemplateProject();
            Assert.True(DotnetCli($"test -c {_buildConfiguration} \"{clonedTemplateProject.ProjectRootPath}\" /p:CollectCoverage=true /p:Include=\"[{ClonedTemplateProject.AssemblyName}]*DeepThought\" /p:IncludeTestAssembly=true", out string standardOutput, out string standardError), standardOutput);
            Assert.Contains("Passed!", standardOutput);
            Assert.Contains("| coverletsamplelib.integration.template | 100% | 100%   | 100%   |", standardOutput);
            Assert.True(File.Exists(Path.Combine(clonedTemplateProject.ProjectRootPath, "coverage.json")));
            AssertCoverage(clonedTemplateProject);
        }

        [Fact]
        public void TestMsbuild_CoverletOutput_Folder_FileNameWithoutExtension()
        {
            using ClonedTemplateProject clonedTemplateProject = PrepareTemplateProject();
            Assert.True(DotnetCli($"test -c {_buildConfiguration} \"{clonedTemplateProject.ProjectRootPath}\" /p:CollectCoverage=true /p:Include=\"[{ClonedTemplateProject.AssemblyName}]*DeepThought\" /p:IncludeTestAssembly=true /p:CoverletOutput=\"{clonedTemplateProject.ProjectRootPath}\"\\file", out string standardOutput, out string standardError), standardOutput);
            Assert.Contains("Passed!", standardOutput);
            Assert.Contains("| coverletsamplelib.integration.template | 100% | 100%   | 100%   |", standardOutput);
            Assert.True(File.Exists(Path.Combine(clonedTemplateProject.ProjectRootPath, "file.json")));
            AssertCoverage(clonedTemplateProject, "file.json");
        }

        [Fact]
        public void TestMsbuild_CoverletOutput_Folder_FileNameExtension()
        {
            using ClonedTemplateProject clonedTemplateProject = PrepareTemplateProject();
            Assert.True(DotnetCli($"test -c {_buildConfiguration} \"{clonedTemplateProject.ProjectRootPath}\" /p:CollectCoverage=true /p:Include=\"[{ClonedTemplateProject.AssemblyName}]*DeepThought\" /p:IncludeTestAssembly=true /p:CoverletOutput=\"{clonedTemplateProject.ProjectRootPath}\"\\file.ext", out string standardOutput, out string standardError), standardOutput);
            Assert.Contains("Passed!", standardOutput);
            Assert.Contains("| coverletsamplelib.integration.template | 100% | 100%   | 100%   |", standardOutput);
            Assert.True(File.Exists(Path.Combine(clonedTemplateProject.ProjectRootPath, "file.ext")));
            AssertCoverage(clonedTemplateProject, "file.ext");
        }

        [Fact]
        public void TestMsbuild_CoverletOutput_Folder_FileNameExtension_SpecifyFramework()
        {
            using ClonedTemplateProject clonedTemplateProject = PrepareTemplateProject();
            Assert.False(clonedTemplateProject.IsMultipleTargetFramework());
            string framework = clonedTemplateProject.GetTargetFrameworks().Single();
            Assert.True(DotnetCli($"test -c {_buildConfiguration} -f {framework} \"{clonedTemplateProject.ProjectRootPath}\" /p:CollectCoverage=true /p:Include=\"[{ClonedTemplateProject.AssemblyName}]*DeepThought\" /p:IncludeTestAssembly=true /p:CoverletOutput=\"{clonedTemplateProject.ProjectRootPath}\"\\file.ext", out string standardOutput, out string standardError), standardOutput);
            Assert.Contains("Passed!", standardOutput);
            Assert.Contains("| coverletsamplelib.integration.template | 100% | 100%   | 100%   |", standardOutput);
            Assert.True(File.Exists(Path.Combine(clonedTemplateProject.ProjectRootPath, "file.ext")));
            AssertCoverage(clonedTemplateProject, "file.ext");
        }

        [Fact]
        public void TestMsbuild_CoverletOutput_Folder_FileNameWithDoubleExtension()
        {
            using ClonedTemplateProject clonedTemplateProject = PrepareTemplateProject();
            Assert.True(DotnetCli($"test -c {_buildConfiguration} \"{clonedTemplateProject.ProjectRootPath}\" /p:CollectCoverage=true /p:Include=\"[{ClonedTemplateProject.AssemblyName}]*DeepThought\" /p:IncludeTestAssembly=true /p:CoverletOutput=\"{clonedTemplateProject.ProjectRootPath}\"\\file.ext1.ext2", out string standardOutput, out string standardError), standardOutput);
            Assert.Contains("Passed!", standardOutput);
            Assert.Contains("| coverletsamplelib.integration.template | 100% | 100%   | 100%   |", standardOutput);
            Assert.True(File.Exists(Path.Combine(clonedTemplateProject.ProjectRootPath, "file.ext1.ext2")));
            AssertCoverage(clonedTemplateProject, "file.ext1.ext2");
        }

        [Fact]
        public void Test_MultipleTargetFrameworkReport_NoCoverletOutput()
        {
            using ClonedTemplateProject clonedTemplateProject = PrepareTemplateProject();
            string[] targetFrameworks = new string[] { "net5.0", "netcoreapp3.1" };
            UpdateProjectTargetFramework(clonedTemplateProject, targetFrameworks);
            Assert.True(DotnetCli($"test -c {_buildConfiguration} \"{clonedTemplateProject.ProjectRootPath}\" /p:CollectCoverage=true /p:Include=\"[{ClonedTemplateProject.AssemblyName}]*DeepThought\" /p:IncludeTestAssembly=true", out string standardOutput, out string standardError, clonedTemplateProject.ProjectRootPath!), standardOutput);
            Assert.Contains("Passed!", standardOutput);
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
            string[] targetFrameworks = new string[] { "netcoreapp3.1", "net5.0" };
            UpdateProjectTargetFramework(clonedTemplateProject, targetFrameworks);
            Assert.True(DotnetCli($"test -c {_buildConfiguration} \"{clonedTemplateProject.ProjectRootPath}\" /p:CollectCoverage=true /p:Include=\"[{ClonedTemplateProject.AssemblyName}]*DeepThought\" /p:IncludeTestAssembly=true /p:CoverletOutput=\"{clonedTemplateProject.ProjectRootPath}\"\\", out string standardOutput, out string standardError, clonedTemplateProject.ProjectRootPath!), standardOutput);
            Assert.Contains("Passed!", standardOutput);
            Assert.Contains("| coverletsamplelib.integration.template | 100% | 100%   | 100%   |", standardOutput);

            foreach (string targetFramework in targetFrameworks)
            {
                string fileToCheck = Path.Combine(clonedTemplateProject.ProjectRootPath, $"coverage.{targetFramework}.json");
                Assert.True(File.Exists(fileToCheck), $"Expected file '{fileToCheck}'\nOutput:\n{standardOutput}");
            }

            AssertCoverage(clonedTemplateProject, "coverage.*.json");
        }

        [Fact]
        public void Test_MultipleTargetFrameworkReport_CoverletOutput_Folder_FileNameWithoutExtension()
        {
            using ClonedTemplateProject clonedTemplateProject = PrepareTemplateProject();
            string[] targetFrameworks = new string[] { "net5.0", "netcoreapp3.1" };
            UpdateProjectTargetFramework(clonedTemplateProject, targetFrameworks);
            Assert.True(DotnetCli($"test -c {_buildConfiguration} \"{clonedTemplateProject.ProjectRootPath}\" /p:CollectCoverage=true /p:Include=\"[{ClonedTemplateProject.AssemblyName}]*DeepThought\" /p:IncludeTestAssembly=true /p:CoverletOutput=\"{clonedTemplateProject.ProjectRootPath}\"\\file", out string standardOutput, out string standardError, clonedTemplateProject.ProjectRootPath!), standardOutput);
            Assert.Contains("Passed!", standardOutput);
            Assert.Contains("| coverletsamplelib.integration.template | 100% | 100%   | 100%   |", standardOutput);

            foreach (string targetFramework in targetFrameworks)
            {
                Assert.True(File.Exists(Path.Combine(clonedTemplateProject.ProjectRootPath, $"file.{targetFramework}.json")));
            }

            AssertCoverage(clonedTemplateProject, "file.*.json");
        }

        [Fact]
        public void Test_MultipleTargetFrameworkReport_CoverletOutput_Folder_FileNameWithExtension_SpecifyFramework()
        {
            using ClonedTemplateProject clonedTemplateProject = PrepareTemplateProject();
            string[] targetFrameworks = new string[] { "net5.0", "netcoreapp3.1" };
            UpdateProjectTargetFramework(clonedTemplateProject, targetFrameworks);
            Assert.True(clonedTemplateProject.IsMultipleTargetFramework());
            string[] frameworks = clonedTemplateProject.GetTargetFrameworks();
            Assert.Equal(2, frameworks.Length);
            string framework = frameworks.FirstOrDefault()!;
            Assert.True(DotnetCli($"test -c {_buildConfiguration} -f {framework} \"{clonedTemplateProject.ProjectRootPath}\" /p:CollectCoverage=true /p:Include=\"[{ClonedTemplateProject.AssemblyName}]*DeepThought\" /p:IncludeTestAssembly=true /p:CoverletOutput=\"{clonedTemplateProject.ProjectRootPath}\"\\file.ext", out string standardOutput, out string standardError, clonedTemplateProject.ProjectRootPath!), standardOutput);
            Assert.Contains("Passed!", standardOutput);
            Assert.Contains("| coverletsamplelib.integration.template | 100% | 100%   | 100%   |", standardOutput);

            foreach (string targetFramework in targetFrameworks)
            {
                if (framework == targetFramework)
                {
                    Assert.True(File.Exists(Path.Combine(clonedTemplateProject.ProjectRootPath, $"file.{targetFramework}.ext")));
                }
                else
                {
                    Assert.False(File.Exists(Path.Combine(clonedTemplateProject.ProjectRootPath, $"file.{targetFramework}.ext")));
                }
            }

            AssertCoverage(clonedTemplateProject, "file.*.ext");
        }

        [Fact]
        public void Test_MultipleTargetFrameworkReport_CoverletOutput_Folder_FileNameWithExtension()
        {
            using ClonedTemplateProject clonedTemplateProject = PrepareTemplateProject();
            string[] targetFrameworks = new string[] {"net5.0", "netcoreapp3.1" };
            UpdateProjectTargetFramework(clonedTemplateProject, targetFrameworks);
            Assert.True(DotnetCli($"test -c {_buildConfiguration} \"{clonedTemplateProject.ProjectRootPath}\" /p:CollectCoverage=true /p:Include=\"[{ClonedTemplateProject.AssemblyName}]*DeepThought\" /p:IncludeTestAssembly=true /p:CoverletOutput=\"{clonedTemplateProject.ProjectRootPath}\"\\file.ext", out string standardOutput, out string standardError, clonedTemplateProject.ProjectRootPath!), standardOutput);
            Assert.Contains("Passed!", standardOutput);
            Assert.Contains("| coverletsamplelib.integration.template | 100% | 100%   | 100%   |", standardOutput);

            foreach (string targetFramework in targetFrameworks)
            {
                Assert.True(File.Exists(Path.Combine(clonedTemplateProject.ProjectRootPath, $"file.{targetFramework}.ext")));
            }

            AssertCoverage(clonedTemplateProject, "file.*.ext");
        }

        [Fact]
        public void Test_MultipleTargetFrameworkReport_CoverletOutput_Folder_FileNameWithDoubleExtension()
        {
            using ClonedTemplateProject clonedTemplateProject = PrepareTemplateProject();
            string[] targetFrameworks = new string[] { "net5.0", "netcoreapp3.1" };
            UpdateProjectTargetFramework(clonedTemplateProject, targetFrameworks);
            Assert.True(DotnetCli($"test -c {_buildConfiguration} \"{clonedTemplateProject.ProjectRootPath}\" /p:CollectCoverage=true /p:Include=\"[{ClonedTemplateProject.AssemblyName}]*DeepThought\" /p:IncludeTestAssembly=true /p:CoverletOutput=\"{clonedTemplateProject.ProjectRootPath}\"\\file.ext1.ext2", out string standardOutput, out string standardError, clonedTemplateProject.ProjectRootPath!), standardOutput);
            Assert.Contains("Passed!", standardOutput);
            Assert.Contains("| coverletsamplelib.integration.template | 100% | 100%   | 100%   |", standardOutput);

            foreach (string targetFramework in targetFrameworks)
            {
                Assert.True(File.Exists(Path.Combine(clonedTemplateProject.ProjectRootPath, $"file.ext1.{targetFramework}.ext2")));
            }

            AssertCoverage(clonedTemplateProject, "file.ext1.*.ext2");
        }
    }
}
