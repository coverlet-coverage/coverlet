using System.IO;
using System.Linq;

using Coverlet.Core;
using Newtonsoft.Json;
using Xunit;

namespace Coverlet.Integration.Tests
{
    public class DotnetGlobalTools : BaseTest
    {
        private string InstallTool(string projectPath)
        {
            DotnetCli($"tool install coverlet.console --version {GetPackageVersion("*console*.nupkg")} --tool-path \"{Path.Combine(projectPath, "coverletTool")}\"", out string standardOutput, out string standardError, projectPath);
            Assert.Contains("was successfully installed.", standardOutput);
            return Path.Combine(projectPath, "coverletTool", "coverlet ");
        }

        [Fact]
        public void DotnetTool()
        {
            using ClonedTemplateProject clonedTemplateProject = CloneTemplateProject();
            UpdateNugeConfigtWithLocalPackageFolder(clonedTemplateProject.Path);
            string coverletToolCommandPath = InstallTool(clonedTemplateProject.Path);
            DotnetCli($"build {clonedTemplateProject.Path}", out string standardOutput, out string standardError);
            string publishedTestFile = clonedTemplateProject.GetFiles("*" + ClonedTemplateProject.AssemblyName + ".dll").Single(f => !f.Contains("obj"));
            RunCommand(coverletToolCommandPath, $"\"{publishedTestFile}\" --target \"dotnet\" --targetargs \"test {Path.Combine(clonedTemplateProject.Path, ClonedTemplateProject.ProjectFileName)} --no-build\"  --include-test-assembly --output \"{clonedTemplateProject.Path}\"\\", out standardOutput, out standardError);
            Assert.Contains("Test Run Successful.", standardOutput);
            AssertCoverage(clonedTemplateProject);
        }
    }
}
