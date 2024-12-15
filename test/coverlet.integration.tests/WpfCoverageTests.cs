// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Xml.Linq;
using Coverlet.Tests.Utils;
using Coverlet.Tests.Xunit.Extensions;
using Xunit;
using Xunit.Abstractions;

namespace Coverlet.Integration.Tests
{
  public class WpfCoverageTests : BaseTest
  {
    private readonly string _buildConfiguration;
    private readonly string _buildTargetFramework;
    private readonly ITestOutputHelper _output;

    public WpfCoverageTests(ITestOutputHelper output)
    {
      _buildConfiguration = TestUtils.GetAssemblyBuildConfiguration().ToString();
      _buildTargetFramework = TestUtils.GetAssemblyTargetFramework();
      _output = output;
    }
    private ClonedTemplateProject PrepareTemplateProject(bool UseCollectorPackage)
    {
      ClonedTemplateProject clonedTemplateProject = CloneTemplateProjectByName("coverlet.tests.projectsample.wpf.tests");
      UpdateNugetConfigWithLocalPackageFolder(clonedTemplateProject.ProjectRootPath!);
      if (UseCollectorPackage)
      {
        AddCoverletCollectorRef(clonedTemplateProject.ProjectFileName, clonedTemplateProject.ProjectRootPath!);
      }
      else
      {
        AddCoverletMsbuildRef(clonedTemplateProject.ProjectFileName, clonedTemplateProject.ProjectRootPath!);
      }
      return clonedTemplateProject;
    }

    [ConditionalFact]
    [SkipOnOS(OS.Linux, "WPF only runs on Windows")]
    [SkipOnOS(OS.MacOS, "WPF only runs on Windows")]
    public void TestCoverage_WPF_collector_net8()
    {
      using ClonedTemplateProject clonedTemplateProject = PrepareTemplateProject(true);
      string[] targetFrameworks = new string[] { "net8.0-windows" };
      UpdateProjectTargetFramework(clonedTemplateProject, targetFrameworks);

      DotnetCli($"publish -c {_buildConfiguration} -f {targetFrameworks[0]} \"{clonedTemplateProject.ProjectRootPath}\"", out string buildOutput, out string buildError);
      _output.WriteLine(buildOutput);

      bool result = DotnetCli($"test -c {_buildConfiguration} -f {targetFrameworks[0]} \"{clonedTemplateProject.ProjectRootPath}\" --no-build --collect:\"XPlat Code Coverage\" /p:Include=\"[{ClonedTemplateProject.AssemblyName}]*TestClass\" /p:IncludeTestAssembly=true", out string testOutput, out string testError);
      if (!string.IsNullOrEmpty(testError))
      {
        _output.WriteLine(testError);
      }
      else
      {
        _output.WriteLine(testOutput);
      }
      Assert.True(result);

      string coverageFileName = "coverage.cobertura.xml";
      Assert.Contains(coverageFileName, testOutput);

      var files = Directory.GetFiles(clonedTemplateProject.ProjectRootPath, coverageFileName, SearchOption.AllDirectories);
      Assert.Single(files);

      using FileStream? coverageFileStream = File.OpenRead(files[0]);
      XDocument xml = XDocument.Load(coverageFileStream);

      // Check if the classes exist
      Assert.NotEmpty(xml.Descendants("class"));
      Assert.Contains(xml.Descendants("class"), cls => cls.Attribute("name")?.Value == "TestProject.Class");
    }

    [ConditionalFact]
    [SkipOnOS(OS.Linux, "WPF only runs on Windows")]
    [SkipOnOS(OS.MacOS, "WPF only runs on Windows")]
    public void TestCoverage_WPF_collector_net48()
    {
      using ClonedTemplateProject clonedTemplateProject = PrepareTemplateProject(true);
      string[] targetFrameworks = new string[] { "net48" };
      UpdateProjectTargetFramework(clonedTemplateProject, targetFrameworks);

      DotnetCli($"publish -c {_buildConfiguration} -f {targetFrameworks[0]} \"{clonedTemplateProject.ProjectRootPath}\"", out string buildOutput, out string buildError);
      _output.WriteLine(buildOutput);

      bool result = DotnetCli($"test -c {_buildConfiguration} -f {targetFrameworks[0]} \"{clonedTemplateProject.ProjectRootPath}\" --no-build --collect:\"XPlat Code Coverage\" /p:Include=\"[{ClonedTemplateProject.AssemblyName}]*TestClass\" /p:IncludeTestAssembly=true", out string testOutput, out string testError);
      if (!string.IsNullOrEmpty(testError))
      {
        _output.WriteLine(testError);
      }
      else
      {
        _output.WriteLine(testOutput);
      }
      Assert.True(result);

      string coverageFileName = "coverage.cobertura.xml";
      Assert.Contains(coverageFileName, testOutput);

      var files = Directory.GetFiles(clonedTemplateProject.ProjectRootPath, coverageFileName, SearchOption.AllDirectories);
      Assert.Single(files);

      using FileStream? coverageFileStream = File.OpenRead(files[0]);
      XDocument xml = XDocument.Load(coverageFileStream);

      // Check if the classes exist
      Assert.NotEmpty(xml.Descendants("class"));
      Assert.Contains(xml.Descendants("class"), cls => cls.Attribute("name")?.Value == "TestProject.Class");
    }

    [ConditionalFact]
    [SkipOnOS(OS.Linux, "WPF only runs on Windows")]
    [SkipOnOS(OS.MacOS, "WPF only runs on Windows")]
    public void TestCoverage_WPF_msbuild_net8()
    {
      using ClonedTemplateProject clonedTemplateProject = PrepareTemplateProject(false);
      string[] targetFrameworks = new string[] { "net8.0-windows" };
      UpdateProjectTargetFramework(clonedTemplateProject, targetFrameworks);

      DotnetCli($"build -c {_buildConfiguration} -f {targetFrameworks[0]} \"{clonedTemplateProject.ProjectRootPath}\"", out string buildOutput, out string buildError);
      _output.WriteLine(buildOutput);

      bool result = DotnetCli($"test -c {_buildConfiguration} -f {targetFrameworks[0]} \"{clonedTemplateProject.ProjectRootPath}\" /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura /p:Include=\"[{ClonedTemplateProject.AssemblyName}]*TestClass\" /p:IncludeTestAssembly=true", out string testOutput, out string testError);
      if (!string.IsNullOrEmpty(testError))
      {
        _output.WriteLine(testError);
      }
      else
      {
        _output.WriteLine(testOutput);
      }
      Assert.True(result);

      string coverageFileName = "coverage.cobertura.xml";
      Assert.Contains(coverageFileName, testOutput);

      var files = Directory.GetFiles(clonedTemplateProject.ProjectRootPath, coverageFileName, SearchOption.AllDirectories);
      Assert.Single(files);

      using FileStream? coverageFileStream = File.OpenRead(files[0]);
      XDocument xml = XDocument.Load(coverageFileStream);

      // Check if the classes exist
      Assert.NotEmpty(xml.Descendants("class"));
      Assert.Contains(xml.Descendants("class"), cls => cls.Attribute("name")?.Value == "TestProject.Class");
    }

    [ConditionalFact]
    [SkipOnOS(OS.Linux, "WPF only runs on Windows")]
    [SkipOnOS(OS.MacOS, "WPF only runs on Windows")]
    public void TestCoverage_WPF_msbuild_net48()
    {
      using ClonedTemplateProject clonedTemplateProject = PrepareTemplateProject(false);
      string[] targetFrameworks = new string[] { "net48" };
      UpdateProjectTargetFramework(clonedTemplateProject, targetFrameworks);

      DotnetCli($"build -c {_buildConfiguration} -f {targetFrameworks[0]} \"{clonedTemplateProject.ProjectRootPath}\"", out string buildOutput, out string buildError);
      _output.WriteLine(buildOutput);

      bool result = DotnetCli($"test -c {_buildConfiguration} -f {targetFrameworks[0]} \"{clonedTemplateProject.ProjectRootPath}\" /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura /p:Include=\"[{ClonedTemplateProject.AssemblyName}]*TestClass\" /p:IncludeTestAssembly=true", out string testOutput, out string testError);
      if (!string.IsNullOrEmpty(testError))
      {
        _output.WriteLine(testError);
      }
      else
      {
        _output.WriteLine(testOutput);
      }
      Assert.True(result);

      string coverageFileName = "coverage.cobertura.xml";
      Assert.Contains(coverageFileName, testOutput);

      var files = Directory.GetFiles(clonedTemplateProject.ProjectRootPath, coverageFileName, SearchOption.AllDirectories);
      Assert.Single(files);

      using FileStream? coverageFileStream = File.OpenRead(files[0]);
      XDocument xml = XDocument.Load(coverageFileStream);

      // Check if the classes exist
      Assert.NotEmpty(xml.Descendants("class"));
      Assert.Contains(xml.Descendants("class"), cls => cls.Attribute("name")?.Value == "TestProject.Class");
    }
  }
}
