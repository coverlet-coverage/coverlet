// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Linq;
using Coverlet.Tests.Utils;
using Coverlet.Tests.Xunit.Extensions;
using Xunit;
using Xunit.Abstractions;

namespace Coverlet.Integration.Tests
{
  public class DotnetGlobalTools : BaseTest
  {
    private readonly ITestOutputHelper _output;
    private readonly string _buildTargetFramework;

    public DotnetGlobalTools(ITestOutputHelper output)
    {
      _output = output;
      _buildTargetFramework = TestUtils.GetAssemblyTargetFramework();
    }
    private string InstallTool(string projectPath)
    {
      _ = DotnetCli($"tool install coverlet.console --version {GetPackageVersion("*console*.nupkg")} --tool-path \"{Path.Combine(projectPath, "coverletTool")}\"", out string standardOutput, out _, projectPath);
      string[] expectedMessage = ["was successfully installed" , "Skipping NuGet package signature verification"];
      Assert.Contains(standardOutput.Split('.')[0], expectedMessage);

      return Path.Combine(projectPath, "coverletTool", "coverlet");
    }

    [Fact]
    public void DotnetTool()
    {
      using ClonedTemplateProject clonedTemplateProject = CloneTemplateProject();
      UpdateNugetConfigWithLocalPackageFolder(clonedTemplateProject.ProjectRootPath!);
      string coverletToolCommandPath = InstallTool(clonedTemplateProject.ProjectRootPath!);
      DotnetCli($"build -f {_buildTargetFramework} {clonedTemplateProject.ProjectRootPath}", out string standardOutput, out string standardError);
      string publishedTestFile = clonedTemplateProject.GetFiles("*" + ClonedTemplateProject.AssemblyName + ".dll").Single(f => !f.Contains("obj") && !f.Contains("ref"));
      RunCommand(coverletToolCommandPath, $"\"{publishedTestFile}\" --target \"dotnet\" --targetargs \"test {Path.Combine(clonedTemplateProject.ProjectRootPath, ClonedTemplateProject.ProjectFileName)} --no-build\"  --include-test-assembly --output \"{clonedTemplateProject.ProjectRootPath}\"{Path.DirectorySeparatorChar}", out standardOutput, out standardError);
      if (!string.IsNullOrEmpty(standardError))
      {
        _output.WriteLine(standardError);
      }
      Assert.Contains("Passed!", standardOutput);
      AssertCoverage(clonedTemplateProject, standardOutput: standardOutput);
    }

    [Fact]
    public void StandAlone()
    {
      using ClonedTemplateProject clonedTemplateProject = CloneTemplateProject();
      UpdateNugetConfigWithLocalPackageFolder(clonedTemplateProject.ProjectRootPath!);
      string coverletToolCommandPath = InstallTool(clonedTemplateProject.ProjectRootPath!);
      DotnetCli($"build -f {_buildTargetFramework} {clonedTemplateProject.ProjectRootPath}", out string standardOutput, out string standardError);
      string publishedTestFile = clonedTemplateProject.GetFiles("*" + ClonedTemplateProject.AssemblyName + ".dll").Single(f => !f.Contains("obj") && !f.Contains("ref"));
      RunCommand(coverletToolCommandPath, $"\"{Path.GetDirectoryName(publishedTestFile)}\" --target \"dotnet\" --targetargs \"{publishedTestFile}\"  --output \"{clonedTemplateProject.ProjectRootPath}\"{Path.DirectorySeparatorChar}", out standardOutput, out standardError);
      if (!string.IsNullOrEmpty(standardError))
      {
        _output.WriteLine(standardError);
      }
      Assert.Contains("Hello World!", standardOutput);
      AssertCoverage(clonedTemplateProject, standardOutput: standardOutput);
    }

    [ConditionalFact]
    [SkipOnOS(OS.Linux)]
    [SkipOnOS(OS.MacOS)]
    public void StandAloneThreshold()
    {
      using ClonedTemplateProject clonedTemplateProject = CloneTemplateProject();
      UpdateNugetConfigWithLocalPackageFolder(clonedTemplateProject.ProjectRootPath!);
      string coverletToolCommandPath = InstallTool(clonedTemplateProject.ProjectRootPath!);
      DotnetCli($"build -f  {_buildTargetFramework}  {clonedTemplateProject.ProjectRootPath}", out string standardOutput, out string standardError);
      string publishedTestFile = clonedTemplateProject.GetFiles("*" + ClonedTemplateProject.AssemblyName + ".dll").Single(f => !f.Contains("obj") && !f.Contains("ref"));
      Assert.False(RunCommand(coverletToolCommandPath, $"\"{Path.GetDirectoryName(publishedTestFile)}\" --target \"dotnet\" --targetargs \"{publishedTestFile}\"  --threshold 80 --output \"{clonedTemplateProject.ProjectRootPath}\"\\", out standardOutput, out standardError));
      if (!string.IsNullOrEmpty(standardError))
      {
        _output.WriteLine(standardError);
      }
      Assert.Contains("Hello World!", standardOutput);
      AssertCoverage(clonedTemplateProject, standardOutput: standardOutput);
      Assert.Contains("The minimum line coverage is below the specified 80", standardOutput);
      Assert.Contains("The minimum method coverage is below the specified 80", standardOutput);
    }

    [ConditionalFact]
    [SkipOnOS(OS.Linux)]
    [SkipOnOS(OS.MacOS)]
    public void StandAloneThresholdLine()
    {
      using ClonedTemplateProject clonedTemplateProject = CloneTemplateProject();
      UpdateNugetConfigWithLocalPackageFolder(clonedTemplateProject.ProjectRootPath!);
      string coverletToolCommandPath = InstallTool(clonedTemplateProject.ProjectRootPath!);
      DotnetCli($"build -f {_buildTargetFramework} {clonedTemplateProject.ProjectRootPath}", out string standardOutput, out string standardError);
      string publishedTestFile = clonedTemplateProject.GetFiles("*" + ClonedTemplateProject.AssemblyName + ".dll").Single(f => !f.Contains("obj") && !f.Contains("ref"));
      Assert.False(RunCommand(coverletToolCommandPath, $"\"{Path.GetDirectoryName(publishedTestFile)}\" --target \"dotnet\" --targetargs \"{publishedTestFile}\"  --threshold 80 --threshold-type line --output \"{clonedTemplateProject.ProjectRootPath}\"\\", out standardOutput, out standardError));
      if (!string.IsNullOrEmpty(standardError))
      {
        _output.WriteLine(standardError);
      }
      Assert.Contains("Hello World!", standardOutput);
      AssertCoverage(clonedTemplateProject, standardOutput: standardOutput);
      Assert.Contains("The minimum line coverage is below the specified 80", standardOutput);
      Assert.DoesNotContain("The minimum method coverage is below the specified 80", standardOutput);
    }

    [ConditionalFact]
    [SkipOnOS(OS.Linux)]
    [SkipOnOS(OS.MacOS)]
    public void StandAloneThresholdLineAndMethod()
    {
      using ClonedTemplateProject clonedTemplateProject = CloneTemplateProject();
      UpdateNugetConfigWithLocalPackageFolder(clonedTemplateProject.ProjectRootPath!);
      string coverletToolCommandPath = InstallTool(clonedTemplateProject.ProjectRootPath!);
      DotnetCli($"build -f  {_buildTargetFramework}  {clonedTemplateProject.ProjectRootPath}", out string standardOutput, out string standardError);
      string publishedTestFile = clonedTemplateProject.GetFiles("*" + ClonedTemplateProject.AssemblyName + ".dll").Single(f => !f.Contains("obj") && !f.Contains("ref"));
      Assert.False(RunCommand(coverletToolCommandPath, $"\"{Path.GetDirectoryName(publishedTestFile)}\" --target \"dotnet\" --targetargs \"{publishedTestFile}\"  --threshold 80 --threshold-type line --threshold-type method --output \"{clonedTemplateProject.ProjectRootPath}\"\\", out standardOutput, out standardError));
      if (!string.IsNullOrEmpty(standardError))
      {
        _output.WriteLine(standardError);
      }
      Assert.Contains("Hello World!", standardOutput);
      AssertCoverage(clonedTemplateProject, standardOutput: standardOutput);
      Assert.Contains("The minimum line coverage is below the specified 80", standardOutput);
      Assert.Contains("The minimum method coverage is below the specified 80", standardOutput);
    }
  }
}
