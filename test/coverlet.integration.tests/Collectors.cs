// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using Coverlet.Tests.Utils;
using Xunit;

namespace Coverlet.Integration.Tests
{
  public class TestSDK_17_8_0 : Collectors
  {
    public TestSDK_17_8_0()
    {
      TestSDKVersion = "17.8.0";
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

  public class TestSDK_17_6_0 : Collectors
  {
    public TestSDK_17_6_0()
    {
      TestSDKVersion = "17.6.0";
    }
  }

  public class TestSDK_Preview : Collectors
  {
    public TestSDK_Preview()
    {
      TestSDKVersion = "17.9.0-preview-23531-01";
    }
  }

  public abstract class Collectors : BaseTest
  {
    private readonly string _buildConfiguration;
    private readonly string _buildTargetFramework;

    public Collectors()
    {
      _buildConfiguration = TestUtils.GetAssemblyBuildConfiguration().ToString();
      _buildTargetFramework = TestUtils.GetAssemblyTargetFramework();
    }

    protected string? TestSDKVersion { get; set; }

    private ClonedTemplateProject PrepareTemplateProject()
    {
      if (TestSDKVersion is null)
      {
        throw new ArgumentNullException("Invalid TestSDKVersion");
      }

      ClonedTemplateProject clonedTemplateProject = CloneTemplateProject(testSDKVersion: TestSDKVersion);
      UpdateNugetConfigWithLocalPackageFolder(clonedTemplateProject.ProjectRootPath!);
      AddCoverletCollectorsRef(clonedTemplateProject.ProjectRootPath!);
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
      int cmdExitCode = DotnetCli($"test -c {_buildConfiguration} -f {_buildTargetFramework} \"{clonedTemplateProject.ProjectRootPath}\" --collect:\"XPlat Code Coverage\" --diag:{Path.Combine(clonedTemplateProject.ProjectRootPath, "log.txt")}", out string standardOutput, out string standardError, clonedTemplateProject.ProjectRootPath!);
      // We don't have any result to check because tests and code to instrument are in same assembly so we need to pass
      // IncludeTestAssembly=true we do it in other test
      if (cmdExitCode != 0)
      {
        //_output.WriteLine(standardError);
      }
      Assert.Contains("Passed!", standardOutput);
      AssertCollectorsInjection(clonedTemplateProject);

    }

    [Fact]
    public void TestVsTest_Test_Settings()
    {
      using ClonedTemplateProject clonedTemplateProject = PrepareTemplateProject();
      string runSettingsPath = AddCollectorRunsettingsFile(clonedTemplateProject.ProjectRootPath!);
      int cmdExitCode = DotnetCli($"test -c {_buildConfiguration} -f {_buildTargetFramework} \"{clonedTemplateProject.ProjectRootPath}\" --collect:\"XPlat Code Coverage\" --settings \"{runSettingsPath}\" --diag:{Path.Combine(clonedTemplateProject.ProjectRootPath, "log.txt")}", out string standardOutput, out string standardError);
      if (cmdExitCode != 0)
      {
        //_output.WriteLine(standardError);
      }
      Assert.Contains("Passed!", standardOutput);
      AssertCoverage(clonedTemplateProject);
      AssertCollectorsInjection(clonedTemplateProject);
    }

    [Fact]
    public void TestVsTest_VsTest()
    {
      using ClonedTemplateProject clonedTemplateProject = PrepareTemplateProject();
      string runSettingsPath = AddCollectorRunsettingsFile(clonedTemplateProject.ProjectRootPath!);
      int cmdExitCode = DotnetCli($"publish -c {_buildConfiguration} -f {_buildTargetFramework} {clonedTemplateProject.ProjectRootPath}", out string standardOutput, out string standardError);
      if (cmdExitCode != 0)
      {
        //_output.WriteLine(standardError);
      }
      Assert.Equal(0, cmdExitCode);
      string publishedTestFile = clonedTemplateProject.GetFiles("*" + ClonedTemplateProject.AssemblyName + ".dll").Single(f => f.Contains("publish"));
      Assert.NotNull(publishedTestFile);
      cmdExitCode = DotnetCli($"vstest \"{publishedTestFile}\" --collect:\"XPlat Code Coverage\" --diag:{Path.Combine(clonedTemplateProject.ProjectRootPath, "log.txt")}", out standardOutput, out standardError);
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
      int cmdExitCode = DotnetCli($"publish -c {_buildConfiguration} -f {_buildTargetFramework} \"{clonedTemplateProject.ProjectRootPath}\"", out string standardOutput, out string standardError);
      if (cmdExitCode != 0)
      {
        //_output.WriteLine(standardError);
      }
      Assert.Equal(0, cmdExitCode);
      string publishedTestFile = clonedTemplateProject.GetFiles("*" + ClonedTemplateProject.AssemblyName + ".dll").Single(f => f.Contains("publish"));
      Assert.NotNull(publishedTestFile);
      cmdExitCode = DotnetCli($"vstest \"{publishedTestFile}\" --collect:\"XPlat Code Coverage\" --ResultsDirectory:\"{clonedTemplateProject.ProjectRootPath}\" /settings:\"{runSettingsPath}\" --diag:{Path.Combine(clonedTemplateProject.ProjectRootPath, "log.txt")}", out standardOutput, out standardError);
      Assert.Contains("Passed!", standardOutput);
      AssertCoverage(clonedTemplateProject);
      AssertCollectorsInjection(clonedTemplateProject);
    }
  }
}
