// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Text.Json;
using System.Xml.Linq;
using Xunit;

namespace Coverlet.MTP.validation.tests;

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
  private const string CoverageLcovFileName = "coverage.info";
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
  public async Task TestCodeWithoutCodeCoverage()
  {
    // Arrange
    using var testProject = CreateTestProject(includeSimpleTest: true,
      includeMethodTests: true,
      includeMultipleClasses: true,
      includeCalculatorTest: true,
      includeBranchTest: true,
      includeMultipleTests: true);
    await BuildProject(testProject.ProjectPath);

    // Act
    var result = await RunTestsWithCoverage(testProject.ProjectPath, "--coverage-output-format json", testName: TestContext.Current.TestCase!.TestMethodName!);

    TestContext.Current?.AddAttachment("Test Output", result.CombinedOutput);

    // Assert
    Assert.True(result.ExitCode == 0, $"Expected successful test run (exit code 0) but got {result.ExitCode} -> '{result.ErrorText}'.\n\n{result.CombinedOutput}");
    Assert.Contains("Passed!", result.StandardOutput);

  }

  [Fact]
  public async Task BasicCoverage_CollectsDataForCoveredLines()
  {
    // Arrange
    using var testProject = CreateTestProject(includeSimpleTest: true);
    await BuildProject(testProject.ProjectPath);

    // Act
    var result = await RunTestsWithCoverage(testProject.ProjectPath, "--coverage --coverage-output-format json", testName: TestContext.Current.TestCase!.TestMethodName!);

    TestContext.Current?.AddAttachment("Test Output", result.CombinedOutput);

    // Assert
    Assert.True(result.ExitCode == 0, $"Expected successful test run (exit code 0) but got {result.ExitCode} -> '{result.ErrorText}'.\n\n{result.CombinedOutput}");
    Assert.Contains("Passed!", result.StandardOutput);

    CheckCoverageResult(testProject, result, CoverageJsonFileName);
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
      "--coverage --coverage-output-format cobertura", testName: TestContext.Current.TestCase!.TestMethodName!);

    TestContext.Current?.AddAttachment("Test Output", result.CombinedOutput);

    // Assert
    Assert.True(result.ExitCode == 0, $"Expected successful test run (exit code 0) but got {result.ExitCode} -> '{result.ErrorText}'.\n\n{result.CombinedOutput}");

    CheckCoverageResult(testProject, result, CoverageCoberturaFileName);
  }

  [Fact]
  public async Task CoverageInstrumentation_TracksMethodHits()
  {
    // Arrange
    using var testProject = CreateTestProject(includeMethodTests: true);
    await BuildProject(testProject.ProjectPath);

    // Act
    var result = await RunTestsWithCoverage(testProject.ProjectPath, "--coverage", testName: TestContext.Current.TestCase!.TestMethodName!);

    TestContext.Current?.AddAttachment("Test Output", result.CombinedOutput);

    // Assert
    Assert.True(result.ExitCode == 0, $"Expected successful test run (exit code 0) but got {result.ExitCode} -> '{result.ErrorText}'.\n\n{result.CombinedOutput}");

    CheckCoverageResult(testProject, result, CoverageJsonFileName);

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
    var result = await RunTestsWithCoverage(testProject.ProjectPath, "--coverage", testName: TestContext.Current.TestCase!.TestMethodName!);

    TestContext.Current?.AddAttachment("Test Output", result.CombinedOutput);

    // Assert
    Assert.True(result.ExitCode == 0, $"Expected successful test run (exit code 0) but got {result.ExitCode} -> '{result.ErrorText}'.\n\n{result.CombinedOutput}");

    CheckCoverageResult(testProject, result, CoverageJsonFileName);

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
      "--coverage --coverage-output-format json --coverage-output-format cobertura --coverage-output-format lcov",
      testName: TestContext.Current.TestCase!.TestMethodName!);

    TestContext.Current?.AddAttachment("Test Output", result.CombinedOutput);

    // Assert
    Assert.True(result.ExitCode == 0, $"Expected successful test run (exit code 0) but got {result.ExitCode} -> '{result.ErrorText}'.\n\n{result.CombinedOutput}");

    //CheckCoverageResult(testProject, result, CoverageJsonFileName);

    // Verify all formats are generated
    Assert.NotEmpty(Directory.GetFiles(testProject.OutputDirectory, CoverageJsonFileName, SearchOption.AllDirectories));
    Assert.NotEmpty(Directory.GetFiles(testProject.OutputDirectory, CoverageCoberturaFileName, SearchOption.AllDirectories));
    Assert.NotEmpty(Directory.GetFiles(testProject.OutputDirectory, CoverageLcovFileName, SearchOption.AllDirectories));
  }

  private void CheckCoverageResult(TestProject testProject, TestResult result, string filename)
  {
    // Check if output directory exists before searching for coverage files
    Assert.True(
      Directory.Exists(testProject.OutputDirectory),
      $"Coverage output directory does not exist: {testProject.OutputDirectory}\n" +
      $"This may indicate that coverage collection failed or the test executable was not built correctly.\n\n" +
      $"Test Output:\n{result.CombinedOutput}");

    string[] coverageFiles = Directory.GetFiles(testProject.OutputDirectory, filename, SearchOption.AllDirectories);
    Assert.True(
      coverageFiles.Length > 0,
      $"No coverage file '{CoverageJsonFileName}' found in '{testProject.OutputDirectory}'.\n" +
      $"Coverage collection may have failed. Check if --coverlet-coverage flag is being processed correctly.\n\n" +
      $"Test Output:\n{result.CombinedOutput}");

    if (filename == CoverageJsonFileName)
    {
      // empty JSON file example:
      // {}
      var coverageData = ParseCoverageJson(coverageFiles[0]);
      Assert.NotNull(coverageData);
      Assert.True(coverageData.RootElement.TryGetProperty("Modules", out _), $"{CoverageJsonFileName} file has no 'Modules'");
    }
    if (filename == CoverageCoberturaFileName)
    {
      // parse XML file and ensure it has valid elements
      // invalid file XML example:
      // <?xml version="1.0" encoding="utf-8"?>
      // <coverage line-rate="0" branch-rate="0" version="1.9" timestamp="1766234003" lines-covered="0" lines-valid="0" branches-covered="0" branches-valid="0">
      //   <sources />
      //   <packages />
      // </coverage>
      XDocument coberturaDoc = XDocument.Load(coverageFiles[0]);
      Assert.NotNull(coberturaDoc.Root);
      Assert.True(coberturaDoc.Root.Name.LocalName == "coverage", $"{CoverageCoberturaFileName} XML root element is not 'coverage'");

      // Check that sources element exists and has at least one source entry
      XElement? sourcesElement = coberturaDoc.Root.Element("sources");
      Assert.True(
        sourcesElement != null,
        $"{CoverageCoberturaFileName} XML file is missing 'sources' element.\n" +
        $"Coverage file: {coverageFiles[0]}\n\n" +
        $"Test Output:\n{result.CombinedOutput}");

      bool hasSourceEntries = sourcesElement.Elements("source").Any();
      Assert.True(
        hasSourceEntries,
        $"{CoverageCoberturaFileName} XML 'sources' element is empty - no source directories were recorded.\n" +
        $"This indicates coverage instrumentation may have failed.\n" +
        $"Coverage file: {coverageFiles[0]}\n\n" +
        $"Test Output:\n{result.CombinedOutput}");

      // Also check that packages element has content
      XElement? packagesElement = coberturaDoc.Root.Element("packages");
      Assert.True(
        packagesElement != null && packagesElement.Elements("package").Any(),
        $"{CoverageCoberturaFileName} XML 'packages' element is empty - no coverage data was collected.\n" +
        $"Coverage file: {coverageFiles[0]}\n\n" +
        $"Test Output:\n{result.CombinedOutput}");
    }
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
    <!-- restore from local folder and nuget.org -->
    <RestoreSources>
			https://api.nuget.org/v3/index.json;
			$(RepoRoot)artifacts/package/$(Configuration.ToLowerInvariant())
		</RestoreSources>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include=""Tests.cs"" />
  </ItemGroup>
  <ItemGroup>
    <!-- Use xunit.v3.mtp-v2 which is designed for MTP v2.x -->
    <PackageReference Include=""xunit.v3.mtp-v2"" Version=""3.2.1"" />
    <PackageReference Include=""Microsoft.Testing.Platform"" Version=""2.0.2"" />
    <PackageReference Include=""coverlet.MTP"" Version=""{coverletMtpVersion}"" />
    <PackageReference Include=""Microsoft.Testing.Extensions.TrxReport"" Version=""2.0.2"" />
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

    // sample 'artifacts\tmp\debug\CoverletMTP_Test_42ddc59580ad4d7696ccebaadcc8e4f6\bin\TestProject\debug'
    string outputPath = Path.Combine(tempPath, "bin", "TestProject", _buildConfiguration.ToLower());
    return new TestProject(projectFile, outputPath);
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
    string result = CheckValue(10);
    Assert.Equal(""Positive"", result);
  }

  [Fact]
  public void Branch_NegativePath_IsCovered()
  {
    string result = CheckValue(-5);
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

  private async Task<TestResult> RunTestsWithCoverage(string projectPath, string arguments, string testName)
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

    string diagFolder = new Uri(Path.Combine(projectDir, "..")).LocalPath;

    var processStartInfo = new ProcessStartInfo
    {
      FileName = "dotnet",
      Arguments = $"exec \"{testExecutable}\" {arguments} --diagnostic --diagnostic-verbosity trace --diagnostic-output-directory {diagFolder} --diagnostic-file-prefix {testName}",
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
      0 => "success, no errors",
      1 => "unknown errors",
      2 => "test failure",
      3 => "test session was aborted",
      4 => "setup of used extensions is invalid",
      5 => "command line arguments passed to the test app are invalid",
      6 => "test session is using a non-implemented feature",
      7 => "unable to complete successfully (likely crashed)",
      8 => "test session ran zero tests",
      9 => "minimum execution policy for the executed tests was violated",
      10 => "test adapter, Testing.Platform Test Framework, MSTest, NUnit, or xUnit, failed to run tests for an infrastructure reason",
      11 => "test process will exit if dependent process exits",
      12 => "test session was unable to run because the client does not support any of the supported protocol versions",
      13 => "excited number of maximum failed tests ",
      _  => "unrecognized exit code"
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
