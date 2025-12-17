// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Text.Json;
using System.Xml.Linq;
using Xunit;

namespace coverlet.MTP.validation.tests;

/// <summary>
/// Integration tests for Coverlet Microsoft Testing Platform extension.
/// These tests verify code instrumentation and coverage data collection using MTP.
/// Similar to coverlet.integration.tests.Collectors but for Microsoft Testing Platform instead of VSTest.
/// </summary>
public class CollectCoverageTests
{
  private readonly string _buildConfiguration;
  private readonly string _buildTargetFramework;
  private readonly string _localPackagesPath;
  private const string CoverageJsonFileName = "coverage.json";
  private const string CoverageCoberturaFileName = "coverage.cobertura.xml";
  private readonly string _repoRoot;

  public CollectCoverageTests()
  {
    _buildConfiguration = "Debug";
    _buildTargetFramework = "net8.0";
    
    // Get local packages path (adjust based on your build output)
    _repoRoot = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", ".."));
    _localPackagesPath = Path.Combine(_repoRoot, "artifacts", "package", _buildConfiguration.ToLowerInvariant());
  }

  [Fact]
  public async Task BasicCoverage_CollectsDataForCoveredLines()
  {
    // Arrange
    using var testProject = CreateTestProject(includeSimpleTest: true);
    await BuildProject(testProject.ProjectPath);

    // Act
    var result = await RunTestsWithCoverage(testProject.ProjectPath, "--coverage");

    TestContext.Current?.AddAttachment("Test Output", result.CombinedOutput);

    // Assert
    Assert.True( result.ExitCode == 0, $"Expected successful test run (exit code 0) but got {result.ExitCode}.\n\n{result.CombinedOutput}");
    Assert.Contains("Passed!", result.StandardOutput);

    string[] coverageFiles = Directory.GetFiles(testProject.OutputDirectory, CoverageJsonFileName, SearchOption.AllDirectories);
    Assert.NotEmpty(coverageFiles);

    var coverageData = ParseCoverageJson(coverageFiles[0]);
    Assert.NotNull(coverageData);
    Assert.True(coverageData.RootElement.TryGetProperty("Modules", out _));
  }

  [Fact]
  public async Task CoverageWithFormat_GeneratesCorrectOutputFormat()
  {
    // Arrange
    using var testProject = CreateTestProject(includeSimpleTest: true);
    await BuildProject(testProject.ProjectPath);

    // Act
    var result = await RunTestsWithCoverage(
      testProject.ProjectPath,
      "--coverage --coverage-output-format cobertura");

    TestContext.Current?.AddAttachment("Test Output", result.CombinedOutput);

    // Assert
    Assert.True(result.ExitCode == 0, $"Expected successful test run (exit code 0) but got {result.ExitCode}.\n\n{result.CombinedOutput}");

    string[] coverageFiles = Directory.GetFiles(testProject.OutputDirectory, CoverageCoberturaFileName, SearchOption.AllDirectories);
    Assert.NotEmpty(coverageFiles);

    var xmlDoc = XDocument.Load(coverageFiles[0]);
    Assert.NotNull(xmlDoc.Root);
    Assert.Equal("coverage", xmlDoc.Root.Name.LocalName);
  }

  [Fact]
  public async Task CoverageInstrumentation_TracksMethodHits()
  {
    // Arrange
    using var testProject = CreateTestProject(includeMethodTests: true);
    await BuildProject(testProject.ProjectPath);

    // Act
    var result = await RunTestsWithCoverage(testProject.ProjectPath, "--coverage");

    TestContext.Current?.AddAttachment("Test Output", result.CombinedOutput);

    // Assert
    Assert.True(result.ExitCode == 0, $"Expected successful test run (exit code 0) but got {result.ExitCode}.\n\n{result.CombinedOutput}");

    string[] coverageFiles = Directory.GetFiles(testProject.OutputDirectory, CoverageJsonFileName, SearchOption.AllDirectories);
    var coverageData = ParseCoverageJson(coverageFiles[0]);

    // Verify method-level coverage tracking
    bool foundCoveredMethod = false;
    if (coverageData.RootElement.TryGetProperty("Modules", out var modules))
    {
      foreach (var module in modules.EnumerateArray())
      {
        if (module.TryGetProperty("Documents", out var documents))
        {
          foreach (var document in documents.EnumerateArray())
          {
            if (document.TryGetProperty("Classes", out var classes))
            {
              foreach (var classInfo in classes.EnumerateArray())
              {
                if (classInfo.TryGetProperty("Methods", out var methods))
                {
                  foreach (var method in methods.EnumerateArray())
                  {
                    if (method.TryGetProperty("Lines", out var lines) && lines.GetArrayLength() > 0)
                    {
                      foundCoveredMethod = true;
                      break;
                    }
                  }
                }
              }
            }
          }
        }
      }
    }

    Assert.True(foundCoveredMethod);
  }

  [Fact]
  public async Task BranchCoverage_TracksConditionalPaths()
  {
    // Arrange
    using var testProject = CreateTestProject(includeBranchTest: true);
    await BuildProject(testProject.ProjectPath);

    // Act
    var result = await RunTestsWithCoverage(testProject.ProjectPath, "--coverage");

    TestContext.Current?.AddAttachment("Test Output", result.CombinedOutput);

    // Assert
    Assert.True(result.ExitCode == 0, $"Expected successful test run (exit code 0) but got {result.ExitCode}.\n\n{result.CombinedOutput}");

    string[] coverageFiles = Directory.GetFiles(testProject.OutputDirectory, CoverageJsonFileName, SearchOption.AllDirectories);
    var coverageData = ParseCoverageJson(coverageFiles[0]);

    // Verify branch coverage is tracked
    bool foundBranches = false;
    if (coverageData.RootElement.TryGetProperty("Modules", out var modules))
    {
      foreach (var module in modules.EnumerateArray())
      {
        if (module.TryGetProperty("Documents", out var documents))
        {
          foreach (var document in documents.EnumerateArray())
          {
            if (document.TryGetProperty("Classes", out var classes))
            {
              foreach (var classInfo in classes.EnumerateArray())
              {
                if (classInfo.TryGetProperty("Methods", out var methods))
                {
                  foreach (var method in methods.EnumerateArray())
                  {
                    if (method.TryGetProperty("Branches", out var branches) &&
                        branches.GetArrayLength() > 0)
                    {
                      foundBranches = true;
                      break;
                    }
                  }
                }
              }
            }
          }
        }
      }
    }

    Assert.True(foundBranches);
  }

  [Fact]
  public async Task MultipleCoverageFormats_GeneratesAllReports()
  {
    // Arrange
    using var testProject = CreateTestProject(includeSimpleTest: true);
    await BuildProject(testProject.ProjectPath);

    // Act
    var result = await RunTestsWithCoverage(
      testProject.ProjectPath,
      "--coverage --coverage-output-format json,cobertura,lcov");

    TestContext.Current?.AddAttachment("Test Output", result.CombinedOutput);

    // Assert
    Assert.True(result.ExitCode == 0, $"Expected successful test run (exit code 0) but got {result.ExitCode}.\n\n{result.CombinedOutput}");

    // Verify all formats are generated
    Assert.NotEmpty(Directory.GetFiles(testProject.OutputDirectory, "coverage.json", SearchOption.AllDirectories));
    Assert.NotEmpty(Directory.GetFiles(testProject.OutputDirectory, "coverage.cobertura.xml", SearchOption.AllDirectories));
    Assert.NotEmpty(Directory.GetFiles(testProject.OutputDirectory, "coverage.info", SearchOption.AllDirectories));
  }

  #region Helper Methods

  private TestProject CreateTestProject(

    bool includeSimpleTest = false,
    bool includeMethodTests = false,
    bool includeMultipleClasses = false,
    bool includeCalculatorTest = false,
    bool includeBranchTest = false,
    bool includeMultipleTests = false)
  {
    // Use repository artifacts folder instead of user temp
    string artifactsTemp = Path.Combine(_repoRoot, "artifacts", "tmp", _buildConfiguration.ToLowerInvariant());
    Directory.CreateDirectory(artifactsTemp);

    string tempPath = Path.Combine(artifactsTemp, $"CoverletMTP_Test_{Guid.NewGuid():N}");
    Directory.CreateDirectory(tempPath);

    // Create NuGet.config to use local packages
    CreateNuGetConfig(tempPath);

    // Get coverlet.MTP package version
    string coverletMtpVersion = GetCoverletMtpPackageVersion();

    // Create project file with MTP enabled and coverlet.MTP reference
    string projectFile = Path.Combine(tempPath, "TestProject.csproj");
    File.WriteAllText(projectFile, $@"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <LangVersion>12.0</LangVersion>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
    <UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner>
    <TestingPlatformDotnetTestSupport>true</TestingPlatformDotnetTestSupport>    
    <OutputType>Exe</OutputType>
    <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>
    <UseArtifactsOutput>true</UseArtifactsOutput>
    <ArtifactsPath>$(MSBuildThisFileDirectory)</ArtifactsPath>
  </PropertyGroup>
  <ItemGroup>
    <!-- Use xunit.v3.mtp-v2 which is designed for MTP v2.x -->
    <PackageReference Include=""xunit.v3.mtp-v2"" Version=""3.2.1"" />
    <PackageReference Include=""Microsoft.Testing.Platform"" Version=""2.0.2"" />
    <PackageReference Include=""coverlet.MTP"" Version=""{coverletMtpVersion}"" />
  </ItemGroup>
</Project>");

    // Create test file based on parameters
    string testCode = GenerateTestCode(
      includeSimpleTest,
      includeMethodTests,
      includeMultipleClasses,
      includeCalculatorTest,
      includeBranchTest,
      includeMultipleTests);

    File.WriteAllText(Path.Combine(tempPath, "Tests.cs"), testCode);

    return new TestProject(projectFile, Path.Combine(tempPath, "bin", _buildConfiguration, _buildTargetFramework));
  }

  private void CreateNuGetConfig(string projectPath)
  {
    string nugetConfig = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <packageSources>
    <clear />
    <add key=""local"" value=""{_localPackagesPath}"" />
    <add key=""nuget.org"" value=""https://api.nuget.org/v3/index.json"" />
  </packageSources>
</configuration>";

    File.WriteAllText(Path.Combine(projectPath, "NuGet.config"), nugetConfig);
  }

  private string GetCoverletMtpPackageVersion()
  {
    // Look for coverlet.MTP package in local packages folder
    if (Directory.Exists(_localPackagesPath))
    {
      var mtpPackages = Directory.GetFiles(_localPackagesPath, "coverlet.MTP.*.nupkg");
      if (mtpPackages.Length > 0)
      {
        string packageName = Path.GetFileNameWithoutExtension(mtpPackages[0]);
        // Extract version from filename (e.g., coverlet.MTP.8.0.0-preview.28.g4608ccb7ad.nupkg)
        string version = packageName["coverlet.MTP.".Length..];
        return version;
      }
    }

    // Fallback to a default version
    return "8.0.0-preview.*";
  }

  private string GenerateTestCode(
    bool includeSimpleTest,
    bool includeMethodTests,
    bool includeMultipleClasses,
    bool includeCalculatorTest,
    bool includeBranchTest,
    bool includeMultipleTests)
  {
    var codeBuilder = new System.Text.StringBuilder();
    codeBuilder.AppendLine("// Copyright (c) Toni Solarin-Sodara");
    codeBuilder.AppendLine("// Licensed under the MIT license. See LICENSE file in the project root for full license information.");
    codeBuilder.AppendLine();
    codeBuilder.AppendLine("using Xunit;");
    codeBuilder.AppendLine();
    codeBuilder.AppendLine("namespace TestProject;");
    codeBuilder.AppendLine();

    if (includeSimpleTest)
    {
      codeBuilder.AppendLine(@"
public class SimpleTests
{
  [Fact]
  public void SimpleTest_Passes()
  {
    int result = Add(2, 3);
    Assert.Equal(5, result);
  }

  private int Add(int a, int b)
  {
    return a + b;
  }
}");
    }

    if (includeMethodTests)
    {
      codeBuilder.AppendLine(@"
public class MethodTests
{
  [Fact]
  public void Method_ExecutesAndIsCovered()
  {
    var sut = new SystemUnderTest();
    int result = sut.Calculate(10, 5);
    Assert.Equal(15, result);
  }
}

public class SystemUnderTest
{
  public int Calculate(int x, int y)
  {
    int temp = x + y;
    return temp;
  }
}");
    }

    if (includeCalculatorTest)
    {
      codeBuilder.AppendLine(@"
public class CalculatorTests
{
  [Fact]
  public void Calculator_Add_ReturnsSum()
  {
    var calc = new Calculator();
    Assert.Equal(10, calc.Add(4, 6));
  }

  [Fact]
  public void Calculator_Multiply_ReturnsProduct()
  {
    var calc = new Calculator();
    Assert.Equal(20, calc.Multiply(4, 5));
  }
}

public class Calculator
{
  public int Add(int a, int b) => a + b;
  public int Multiply(int a, int b) => a * b;
}");
    }

    if (includeBranchTest)
    {
      codeBuilder.AppendLine(@"
public class BranchTests
{
  [Fact]
  public void Branch_PositivePath_IsCovered()
  {
    var result = CheckValue(10);
    Assert.Equal(""Positive"", result);
  }

  [Fact]
  public void Branch_NegativePath_IsCovered()
  {
    var result = CheckValue(-5);
    Assert.Equal(""Negative"", result);
  }

  private string CheckValue(int value)
  {
    if (value > 0)
    {
      return ""Positive"";
    }
    else if (value < 0)
    {
      return ""Negative"";
    }
    return ""Zero"";
  }
}");
    }

    if (includeMultipleTests)
    {
      codeBuilder.AppendLine(@"
public class ConcurrentTests
{
  [Fact]
  public void Test1() => Assert.True(true);

  [Fact]
  public void Test2() => Assert.True(true);

  [Fact]
  public void Test3() => Assert.True(true);
}");
    }

    if (includeMultipleClasses)
    {
      codeBuilder.AppendLine(@"
public class IncludedTests
{
  [Fact]
  public void IncludedTest() => Assert.True(true);
}

// This would be in ExcludedClass.cs in real scenario
public class ExcludedClass
{
  public void ExcludedMethod() { }
}");
    }

    return codeBuilder.ToString();
  }

  private async Task<int> BuildProject(string projectPath)
  {
    var processStartInfo = new ProcessStartInfo
    {
      FileName = "dotnet",
      Arguments = $"build \"{projectPath}\" -c {_buildConfiguration} -f {_buildTargetFramework}",
      RedirectStandardOutput = true,
      RedirectStandardError = true,
      UseShellExecute = false,
      CreateNoWindow = true,
      WorkingDirectory = Path.GetDirectoryName(projectPath)
    };

    using var process = Process.Start(processStartInfo);
    
    string output = await process!.StandardOutput.ReadToEndAsync();
    string error = await process.StandardError.ReadToEndAsync();
    
    await process.WaitForExitAsync();
    
    // Attach build output for debugging
    if (process.ExitCode != 0)
    {
      throw new InvalidOperationException($"Build failed:\nOutput: {output}\nError: {error}");
    }
    
    return process.ExitCode;
  }

  private async Task<TestResult> RunTestsWithCoverage(string projectPath, string arguments)
  {
    // For MTP, we need to run the test executable directly, not through dotnet test
    string projectDir = Path.GetDirectoryName(projectPath)!;
    string projectName = Path.GetFileNameWithoutExtension(projectPath);
    string testExecutable = Path.Combine(projectDir, "bin", projectName, _buildConfiguration.ToLower(), $"{projectName}.dll");

    if (!File.Exists(testExecutable))
    {
      throw new FileNotFoundException(
        $"Test executable not found: {testExecutable}\n" +
        $"Build may have failed silently.");
    }

    string coverletMtpDll = Path.Combine(
      Path.GetDirectoryName(testExecutable)!,
      "coverlet.MTP.dll");

    if (!File.Exists(coverletMtpDll))
    {
      throw new FileNotFoundException(
        $"Coverlet MTP extension not found: {coverletMtpDll}\n" +
        $"The coverlet.MTP NuGet package may not have restored correctly.");
    }

    var processStartInfo = new ProcessStartInfo
    {
      FileName = "dotnet",
      Arguments = $"exec \"{testExecutable}\" {arguments} --diagnostic --diagnostic-verbosity trace",
      RedirectStandardOutput = true,
      RedirectStandardError = true,
      UseShellExecute = false,
      CreateNoWindow = true,
      WorkingDirectory = projectDir
    };

    using var process = Process.Start(processStartInfo);

    string output = await process!.StandardOutput.ReadToEndAsync();
    string error = await process.StandardError.ReadToEndAsync();

    await process.WaitForExitAsync();

    string errorContext = process.ExitCode switch
    {
      0 => "Success",
      1 => "Test failures occurred",
      2 => "Invalid command-line arguments",
      3 => "Test discovery failed",
      4 => "Test execution failed",
      5 => "Unexpected error (unhandled exception)",
      _ => "Unknown error"
    };

    return new TestResult
    {
      ExitCode = process.ExitCode,
      ErrorText = errorContext,
      StandardOutput = output,
      StandardError = error,
      CombinedOutput = $"=== TEST EXECUTABLE ===\n{testExecutable}\n\n" +
                    $"=== ARGUMENTS ===\n{arguments}\n\n" +
                    $"=== EXIT CODE ===\n{process.ExitCode}\n\n" +
                    $"=== STDOUT ===\n{output}\n\n" +
                    $"=== STDERR ===\n{error}"
    };
  }

  private JsonDocument ParseCoverageJson(string filePath)
  {
    string jsonContent = File.ReadAllText(filePath);
    return JsonDocument.Parse(jsonContent);
  }

  #endregion

  private class TestProject : IDisposable
  {
    public string ProjectPath { get; }
    public string OutputDirectory { get; }

    public TestProject(string projectPath, string outputDirectory)
    {
      ProjectPath = projectPath;
      OutputDirectory = outputDirectory;
    }

    public void Dispose()
    {
      string? projectDir = Path.GetDirectoryName(ProjectPath);
      if (projectDir == null || !Directory.Exists(projectDir))
        return;

      // Retry cleanup to handle file locks (especially on Windows)
      for (int i = 0; i < 3; i++)
      {
        try
        {
          Directory.Delete(projectDir, recursive: true);
          return; // Success
        }
        catch (IOException) when (i < 2)
        {
          // File may be locked by antivirus or other process
          System.Threading.Thread.Sleep(100);
        }
        catch (UnauthorizedAccessException) when (i < 2)
        {
          // Mark files as normal (remove read-only) and retry
          foreach (var file in Directory.GetFiles(projectDir, "*", SearchOption.AllDirectories))
          {
            File.SetAttributes(file, FileAttributes.Normal);
          }
          System.Threading.Thread.Sleep(100);
        }
      }
      
      // Log cleanup failure but don't throw (test already finished)
      Debug.WriteLine($"Warning: Failed to cleanup test directory: {projectDir}");
    }
  }

  private class TestResult
  {
    public int ExitCode { get; set; }
    public string ErrorText { get; set; } = string.Empty;
    public string StandardOutput { get; set; } = string.Empty;
    public string StandardError { get; set; } = string.Empty;
    public string CombinedOutput { get; set; } = string.Empty;
  }
}
