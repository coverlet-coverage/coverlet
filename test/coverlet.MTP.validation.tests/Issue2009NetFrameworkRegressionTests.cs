// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Xunit;

namespace Coverlet.MTP.validation.tests;

/// <summary>
/// Regression test for issue #2009 (.NET Framework tracker TypeLoadException).
/// </summary>
[Collection(nameof(MtpValidationTests))]
public class Issue2009NetFrameworkRegressionTests : MtpValidationTestBase
{
  private const string CoverageJsonFileName = "coverage.json";
  private const string NetFrameworkTfm = "net472";

  [Fact]
  public async Task Issue2009_NetFrameworkCoverage_DoesNotThrowTypeLoadException()
  {
    Assert.SkipUnless(OperatingSystem.IsWindows(), "Test requires Windows");

    // Arrange
    string testName = TestContext.Current.TestCase!.TestMethodName!;
    using var testProject = CreateIssue2009ReproProject(testName);
    await BuildProjectAsync(testProject.SolutionPath);

    // Act
    TestResult result = await RunNetFrameworkTestsWithCoverage(testProject, BuildConfiguration);
    TestContext.Current?.AddAttachment("Test Output", result.CombinedOutput);

    // Assert
    Assert.True(result.ExitCode == 0,
      $"Expected successful test run (exit code 0) but got {result.ExitCode}.\n\n{result.CombinedOutput}");
    Assert.Contains("Passed!", result.StandardOutput);
    Assert.DoesNotContain("TypeLoadException", result.CombinedOutput, StringComparison.Ordinal);
    Assert.DoesNotContain("ConcurrentBag`1", result.CombinedOutput, StringComparison.Ordinal);

    string[] coverageFiles = Directory.GetFiles(
      testProject.SolutionDirectory,
      CoverageJsonFileName.Insert(CoverageJsonFileName.LastIndexOf('.'), ".*"),
      SearchOption.AllDirectories);

    Assert.NotEmpty(coverageFiles);
  }

  private TestProjectInfo CreateIssue2009ReproProject(string testName)
  {
    string artifactsTemp = Path.Combine(RepoRoot, "artifacts", "tmp", BuildConfiguration.ToLowerInvariant());
    Directory.CreateDirectory(artifactsTemp);

    string solutionPath = CreateSolutionDirectory(artifactsTemp, "MTP_Issue2009_", SanitizePathName(testName));

    string sutProjectPath = Path.Combine(solutionPath, SutProjectName);
    string testProjectPath = Path.Combine(solutionPath, TestProjectName);
    Directory.CreateDirectory(sutProjectPath);
    Directory.CreateDirectory(testProjectPath);

    CreateNugetConfig(solutionPath);

    string coverletMtpVersion = GetCoverletMtpPackageVersion();

    CreateIssue2009SutProject(sutProjectPath);
    CreateIssue2009TestProject(testProjectPath, coverletMtpVersion);

    string solutionFile = Path.Combine(solutionPath, "TestSolution.sln");
    CreateSolutionFile(solutionFile);

    string outputPath = Path.Combine(solutionPath, "bin", TestProjectName, BuildConfiguration.ToLowerInvariant());
    return new TestProjectInfo(solutionFile, testProjectPath, outputPath, solutionPath);
  }

  private static void CreateIssue2009SutProject(string sutProjectPath)
  {
    string sutCsproj = Path.Combine(sutProjectPath, $"{SutProjectName}.csproj");
    File.WriteAllText(sutCsproj, """
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net472</TargetFramework>
    <IsPackable>false</IsPackable>
    <UseArtifactsOutput>true</UseArtifactsOutput>
    <ArtifactsPath>$(MSBuildThisFileDirectory)..</ArtifactsPath>
    <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NETFramework.ReferenceAssemblies" Version="1.0.3">
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>
</Project>
""");

    File.WriteAllText(Path.Combine(sutProjectPath, "Class1.cs"), """
// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace SampleLibrary;

public class Class1
{
  public int Method()
  {
    return 1;
  }
}
""");
  }

  private static void CreateIssue2009TestProject(string testProjectPath, string coverletMtpVersion)
  {
    string relativeSutPath = Path.Combine("..", SutProjectName, $"{SutProjectName}.csproj");

    string testCsproj = Path.Combine(testProjectPath, $"{TestProjectName}.csproj");
    File.WriteAllText(testCsproj, $$"""
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>{{NetFrameworkTfm}}</TargetFramework>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
    <UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner>
    <TestingPlatformDotnetTestSupport>true</TestingPlatformDotnetTestSupport>
    <OutputType>Exe</OutputType>
    <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>
    <UseArtifactsOutput>true</UseArtifactsOutput>
    <ArtifactsPath>$(MSBuildThisFileDirectory)..</ArtifactsPath>
    <Deterministic>false</Deterministic>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="{{relativeSutPath}}" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="xunit.v3.mtp-v2" Version="{{MtpPackageVersions.XunitV3}}" />
    <PackageReference Include="Microsoft.Testing.Platform" Version="{{MtpPackageVersions.MicrosoftTestingPlatform}}" />
    <PackageReference Include="coverlet.MTP" Version="{{coverletMtpVersion}}" />
    <PackageReference Include="Microsoft.Testing.Extensions.TrxReport" Version="{{MtpPackageVersions.MicrosoftTestingPlatform}}" />
    <PackageReference Include="Microsoft.NETFramework.ReferenceAssemblies" Version="1.0.3">
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>
</Project>
""");

    File.WriteAllText(Path.Combine(testProjectPath, "ReproTests.cs"), """
// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using SampleLibrary;
using Xunit;

namespace TestProject;

public class ReproTests
{
  [Fact]
  public void DemoTest()
  {
    var sut = new Class1();
    Assert.Equal(1, sut.Method());
  }
}
""");
  }

  private static async Task<TestResult> RunNetFrameworkTestsWithCoverage(TestProjectInfo testProject, string buildConfiguration)
  {
    string testExecutablePath = Path.Combine(
      testProject.SolutionDirectory,
      "bin",
      TestProjectName,
      buildConfiguration.ToLowerInvariant(),
      $"{TestProjectName}.exe");

    if (!File.Exists(testExecutablePath))
    {
      throw new FileNotFoundException($"Test executable not found: {testExecutablePath}");
    }

    string workingDirectory = Path.GetDirectoryName(testExecutablePath)!;

    var processStartInfo = new ProcessStartInfo
    {
      FileName = testExecutablePath,
      Arguments = "--diagnostic --coverlet --results-directory ./results",
      RedirectStandardOutput = true,
      RedirectStandardError = true,
      UseShellExecute = false,
      CreateNoWindow = true,
      WorkingDirectory = workingDirectory,
    };

    using var process = Process.Start(processStartInfo)!;

    Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync();
    Task<string> stderrTask = process.StandardError.ReadToEndAsync();

    await process.WaitForExitAsync();
    await Task.WhenAll(stdoutTask, stderrTask, process.WaitForExitAsync());
    string output = await stdoutTask;
    string error = await stderrTask;

    return new TestResult(
      process.ExitCode,
      output,
      error);
  }
}
