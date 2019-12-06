using Xunit;


namespace Coverlet.Integration.Tests
{
    public class Msbuild : BaseTest
    {
        [Fact]
        public void TestMsbuild()
        {
            using ClonedTemplateProject clonedTemplateProject = CloneTemplateProject();
            UpdateNugeConfigtWithLocalPackageFolder(clonedTemplateProject.Path);
            AddCoverletMsbuildRef(clonedTemplateProject.Path);
            Assert.True(DotnetCli($"test \"{clonedTemplateProject.Path}\" /p:CollectCoverage=true /p:Include=\"[{ClonedTemplateProject.AssemblyName}]*DeepThought\" /p:IncludeTestAssembly=true /p:CoverletOutput=\"{clonedTemplateProject.Path}\"\\", out string standardOutput, out string standardError), standardOutput);
            Assert.Contains("Test Run Successful.", standardOutput);
            Assert.Contains("| coverletsamplelib.integration.template | 100% | 100%   | 100%   |", standardOutput);
            AssertCoverage(clonedTemplateProject);
        }
    }
}
