// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Coverlet.Tests.Xunit.Extensions;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Xunit;

namespace Coverlet.Integration.Tests
{
    public class DotnetGlobalTools : BaseTest
    {
        private string InstallTool(string projectPath)
        {
            _ = DotnetCli($"tool install coverlet.console --version {GetPackageVersion("*console*.nupkg")} --tool-path \"{Path.Combine(projectPath, "coverletTool")}\"", out string standardOutput, out _, projectPath);
            Assert.Contains("was successfully installed.", standardOutput, StringComparison.InvariantCulture);
            Debug.WriteLine($"dotnet tool installed - toolpath:  {projectPath}\\coverletTool");
            return Path.Combine(projectPath, "coverletTool", "coverlet ");
        }

        [ConditionalFact]
        [SkipOnOS(OS.Linux)]
        [SkipOnOS(OS.MacOS)]
        public void DotnetTool()
        {
            using ClonedTemplateProject clonedTemplateProject = CloneTemplateProject();
            UpdateNugeConfigtWithLocalPackageFolder(clonedTemplateProject.ProjectRootPath!);
            string coverletToolCommandPath = InstallTool(clonedTemplateProject.ProjectRootPath!);
            DotnetCli($"build {clonedTemplateProject.ProjectRootPath}", out string standardOutput, out string standardError);
            string publishedTestFile = clonedTemplateProject.GetFiles("*" + ClonedTemplateProject.AssemblyName + ".dll").Single(f => !f.Contains("obj", StringComparison.InvariantCulture) && !f.Contains("ref", StringComparison.InvariantCulture));
            RunCommand(coverletToolCommandPath, $"\"{publishedTestFile}\" --target \"dotnet\" --targetargs \"test {Path.Combine(clonedTemplateProject.ProjectRootPath, ClonedTemplateProject.ProjectFileName)} --no-build\"  --include-test-assembly --output \"{clonedTemplateProject.ProjectRootPath}\"\\", out standardOutput, out standardError);
            Assert.Contains("Passed!", standardOutput, StringComparison.InvariantCulture);
            AssertCoverage(clonedTemplateProject, standardOutput: standardOutput);
        }

        [ConditionalFact]
        [SkipOnOS(OS.Linux)]
        [SkipOnOS(OS.MacOS)]
        public void StandAlone()
        {
            using ClonedTemplateProject clonedTemplateProject = CloneTemplateProject();
            UpdateNugeConfigtWithLocalPackageFolder(clonedTemplateProject.ProjectRootPath!);
            string coverletToolCommandPath = InstallTool(clonedTemplateProject.ProjectRootPath!);
            DotnetCli($"build {clonedTemplateProject.ProjectRootPath}", out string standardOutput, out string standardError);
            string publishedTestFile = clonedTemplateProject.GetFiles("*" + ClonedTemplateProject.AssemblyName + ".dll").Single(f => !f.Contains("obj",StringComparison.InvariantCulture) && !f.Contains("ref", StringComparison.InvariantCulture));
            RunCommand(coverletToolCommandPath, $"\"{Path.GetDirectoryName(publishedTestFile)}\" --target \"dotnet\" --targetargs \"{publishedTestFile}\"  --output \"{clonedTemplateProject.ProjectRootPath}\"\\", out standardOutput, out standardError);
            Assert.Contains("Hello World!", standardOutput, StringComparison.InvariantCulture);
            AssertCoverage(clonedTemplateProject, standardOutput: standardOutput);
        }
    }
}
