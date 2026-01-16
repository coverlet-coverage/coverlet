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
/// Uses a separate library project (SUT) referenced by the test project - the typical real-world scenario.
/// </summary>
public class CollectCoverageTests
{
  private readonly string _buildConfiguration;
  private readonly string _localPackagesPath;
  private const string CoverageJsonFileName = "coverage.json";
  private const string CoverageCoberturaFileName = "coverage.cobertura.xml";
  private const string CoverageLcovFileName = "coverage.info";
  private const string TestProjectName = "TestProject";
  private const string SutProjectName = "SampleLibrary";
  private readonly string _repoRoot;

  public CollectCoverageTests()
  {
#if DEBUG
    _buildConfiguration = "Debug";
#else
    _buildConfiguration = "Release";
#endif

    // Get local packages path (adjust based on your build output)
    _repoRoot = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", ".."));
    _localPackagesPath = Path.Combine(_repoRoot, "artifacts", "package", _buildConfiguration.ToLowerInvariant());
  }

  [Fact]
  public async Task TestCodeWithoutCodeCoverage()
  {
    // Arrange
    string testName = TestContext.Current.TestCase!.TestMethodName!;
    using var testProject = CreateTestProject(testName,
      includeSimpleTest: true,
      includeMethodTests: true,
      includeMultipleClasses: true,
      includeCalculatorTest: true,
      includeBranchTest: true,
      includeMultipleTests: true);
    await BuildProject(testProject.SolutionPath);

    // Act
    var result = await RunTestsWithCoverage(testProject, "--coverlet-output-format json", testName);

    TestContext.Current?.AddAttachment("Test Output", result.CombinedOutput);

    // Assert
    Assert.True(result.ExitCode == 0, $"Expected successful test run (exit code 0) but got {result.ExitCode} -> '{result.ErrorText}'.\n\n{result.CombinedOutput}");
    Assert.Contains("Passed!", result.StandardOutput);
  }

  [Fact]
  public async Task BasicCoverage_CollectsDataForCoveredLines()
  {
    // Arrange
    string testName = TestContext.Current.TestCase!.TestMethodName!;
    using var testProject = CreateTestProject(testName, includeSimpleTest: true);
    await BuildProject(testProject.SolutionPath);

    // Act
    var result = await RunTestsWithCoverage(testProject, "--coverlet --coverlet-output-format json", testName);

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
    string testName = TestContext.Current.TestCase!.TestMethodName!;
    using var testProject = CreateTestProject(testName, includeSimpleTest: true);
    await BuildProject(testProject.SolutionPath);

    // Act
    var result = await RunTestsWithCoverage(
      testProject,
      "--coverlet --coverlet-output-format cobertura", testName);

    TestContext.Current?.AddAttachment("Test Output", result.CombinedOutput);

    // Assert
    Assert.True(result.ExitCode == 0, $"Expected successful test run (exit code 0) but got {result.ExitCode} -> '{result.ErrorText}'.\n\n{result.CombinedOutput}");

    CheckCoverageResult(testProject, result, CoverageCoberturaFileName);
  }

  [Fact]
  public async Task CoverageInstrumentation_TracksMethodHits()
  {
    // Arrange
    string testName = TestContext.Current.TestCase!.TestMethodName!;
    using var testProject = CreateTestProject(testName, includeMethodTests: true);
    await BuildProject(testProject.SolutionPath);

    // Act
    var result = await RunTestsWithCoverage(testProject, "--coverlet", testName);

    TestContext.Current?.AddAttachment("Test Output", result.CombinedOutput);

    // Assert
    Assert.True(result.ExitCode == 0, $"Expected successful test run (exit code 0) but got {result.ExitCode} -> '{result.ErrorText}'.\n\n{result.CombinedOutput}");

    CheckCoverageResult(testProject, result, CoverageJsonFileName);

    string[] coverageFiles = Directory.GetFiles(testProject.OutputDirectory, CoverageJsonFileName, SearchOption.AllDirectories);
    var coverageData = ParseCoverageJson(coverageFiles[0]);

    // Verify method-level coverage tracking
    // JSON format: { "Module.dll": { "SourceFile.cs": { "Namespace.Class": { "MethodSignature": { "Lines": {...}, "Branches": [...] } } } } }
    bool foundCoveredMethod = false;

    foreach (var module in coverageData.RootElement.EnumerateObject()
      .Where(m => m.Name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)))
    {
      // Enumerate documents (source files)
      foreach (var document in module.Value.EnumerateObject())
      {
        // Enumerate classes
        foreach (var classEntry in document.Value.EnumerateObject())
        {
          // Enumerate methods
          foreach (var method in classEntry.Value.EnumerateObject())
          {
            // Check for Lines property with actual line data
            if (method.Value.TryGetProperty("Lines", out var lines) &&
                lines.ValueKind == JsonValueKind.Object &&
                lines.EnumerateObject().Any())
            {
              foundCoveredMethod = true;
              break;
            }
          }
          if (foundCoveredMethod) break;
        }
        if (foundCoveredMethod) break;
      }
      if (foundCoveredMethod) break;
    }

    Assert.True(foundCoveredMethod,
      $"No covered methods found in coverage data.\n" +
      $"Coverage file: {coverageFiles[0]}\n\n" +
      $"Test Output:\n{result.CombinedOutput}");
  }

  [Fact]
  public async Task BranchCoverage_TracksConditionalPaths()
  {
    // Arrange
    string testName = TestContext.Current.TestCase!.TestMethodName!;
    using var testProject = CreateTestProject(testName, includeBranchTest: true);
    await BuildProject(testProject.SolutionPath);

    // Act
    var result = await RunTestsWithCoverage(testProject, "--coverlet", testName);

    TestContext.Current?.AddAttachment("Test Output", result.CombinedOutput);

    // Assert
    Assert.True(result.ExitCode == 0, $"Expected successful test run (exit code 0) but got {result.ExitCode} -> '{result.ErrorText}'.\n\n{result.CombinedOutput}");

    CheckCoverageResult(testProject, result, CoverageJsonFileName);

    string[] coverageFiles = Directory.GetFiles(testProject.OutputDirectory, CoverageJsonFileName, SearchOption.AllDirectories);
    var coverageData = ParseCoverageJson(coverageFiles[0]);

    // Verify branch coverage is tracked
    // JSON format: { "Module.dll": { "SourceFile.cs": { "Namespace.Class": { "MethodSignature": { "Lines": {...}, "Branches": [{...}] } } } } }
    bool foundBranches = false;

    foreach (var module in coverageData.RootElement.EnumerateObject()
      .Where(m => m.Name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)))
    {
      // Enumerate documents (source files)
      foreach (var document in module.Value.EnumerateObject())
      {
        // Enumerate classes
        foreach (var classEntry in document.Value.EnumerateObject())
        {
          // Enumerate methods
          foreach (var method in classEntry.Value.EnumerateObject())
          {
            // Check for Branches array with actual branch data
            if (method.Value.TryGetProperty("Branches", out var branches) &&
                branches.ValueKind == JsonValueKind.Array &&
                branches.GetArrayLength() > 0)
            {
              foundBranches = true;
              break;
            }
          }
          if (foundBranches) break;
        }
        if (foundBranches) break;
      }
      if (foundBranches) break;
    }

    Assert.True(foundBranches,
      $"No branch coverage data found.\n" +
      $"Coverage file: {coverageFiles[0]}\n\n" +
      $"Test Output:\n{result.CombinedOutput}");
  }

  [Fact]
  public async Task MultipleCoverageFormats_GeneratesAllReports()
  {
    // Arrange
    string testName = TestContext.Current.TestCase!.TestMethodName!;
    using var testProject = CreateTestProject(testName, includeSimpleTest: true);
    await BuildProject(testProject.SolutionPath);

    // Act
    var result = await RunTestsWithCoverage(
      testProject,
      "--coverlet --coverlet-output-format json --coverlet-output-format cobertura --coverlet-output-format lcov",
      testName);

    TestContext.Current?.AddAttachment("Test Output", result.CombinedOutput);

    // Assert
    Assert.True(result.ExitCode == 0, $"Expected successful test run (exit code 0) but got {result.ExitCode} -> '{result.ErrorText}'.\n\n{result.CombinedOutput}");

    // Verify all formats are generated
    Assert.NotEmpty(Directory.GetFiles(testProject.OutputDirectory, CoverageJsonFileName, SearchOption.AllDirectories));
    Assert.NotEmpty(Directory.GetFiles(testProject.OutputDirectory, CoverageCoberturaFileName, SearchOption.AllDirectories));
    Assert.NotEmpty(Directory.GetFiles(testProject.OutputDirectory, CoverageLcovFileName, SearchOption.AllDirectories));
  }

  [Fact]
  public async Task MultipleCoverageFormats_WithResultsDirectory()
  {
    // Arrange
    string testName = TestContext.Current.TestCase!.TestMethodName!;
    using var testProject = CreateTestProject(testName, includeSimpleTest: true);
    await BuildProject(testProject.SolutionPath);

    // Create a specific results directory
    string resultsDirectory = Path.GetFullPath(Path.Combine(testProject.SolutionDirectory, _repoRoot, "artifacts", "tmp", "TestResults"));
    Directory.CreateDirectory(resultsDirectory);

    // Act
    var result = await RunTestsWithCoverage(
      testProject,
      $"--coverlet --coverlet-output-format json --coverlet-output-format cobertura --coverlet-output-format lcov --report-xunit-trx --results-directory \"{resultsDirectory}\"",
      testName);

    TestContext.Current?.AddAttachment("Test Output", result.CombinedOutput);

    // Assert - Test should pass
    Assert.True(result.ExitCode == 0, $"Expected successful test run (exit code 0) but got {result.ExitCode} -> '{result.ErrorText}'.\n\n{result.CombinedOutput}");

    // Verify coverage files are generated in the specified results directory
    string[] jsonFiles = Directory.GetFiles(resultsDirectory, CoverageJsonFileName, SearchOption.AllDirectories);
    string[] coberturaFiles = Directory.GetFiles(resultsDirectory, CoverageCoberturaFileName, SearchOption.AllDirectories);
    string[] lcovFiles = Directory.GetFiles(resultsDirectory, CoverageLcovFileName, SearchOption.AllDirectories);

    Assert.True(jsonFiles.Length > 0,
      $"No {CoverageJsonFileName} found in results directory: {resultsDirectory}\n" +
      $"Files in directory: {string.Join(", ", Directory.GetFiles(resultsDirectory, "*", SearchOption.AllDirectories))}\n\n" +
      $"Test Output:\n{result.CombinedOutput}");

    Assert.True(coberturaFiles.Length > 0,
      $"No {CoverageCoberturaFileName} found in results directory: {resultsDirectory}\n" +
      $"Files in directory: {string.Join(", ", Directory.GetFiles(resultsDirectory, "*", SearchOption.AllDirectories))}\n\n" +
      $"Test Output:\n{result.CombinedOutput}");

    Assert.True(lcovFiles.Length > 0,
      $"No {CoverageLcovFileName} found in results directory: {resultsDirectory}\n" +
      $"Files in directory: {string.Join(", ", Directory.GetFiles(resultsDirectory, "*", SearchOption.AllDirectories))}\n\n" +
      $"Test Output:\n{result.CombinedOutput}");

    // Verify console output contains "Coverage reports generated:" message
    Assert.Contains("Out of process file artifacts produced:", result.StandardOutput);
    Assert.Contains(CoverageJsonFileName, result.StandardOutput);
    Assert.Contains(CoverageCoberturaFileName.Replace(".cobertura.xml", ""), result.StandardOutput);
  }

  private static void CheckCoverageResult(TestProjectInfo testProject, TestResult result, string filename)
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
      $"No coverage file '{filename}' found in '{testProject.OutputDirectory}'.\n" +
      $"Coverage collection may have failed. Check if --coverlet flag is being processed correctly.\n\n" +
      $"Test Output:\n{result.CombinedOutput}");

    if (filename == CoverageJsonFileName)
    {
      // JSON format structure:
      // { "ModuleName.dll": { "SourceFile.cs": { "Namespace.Class": { "MethodSignature": { "Lines": {...}, "Branches": [...] } } } } }
      var coverageData = ParseCoverageJson(coverageFiles[0]);
      Assert.NotNull(coverageData);

      // Check that we have at least one module (top-level property)
      bool hasModules = false;
      bool hasLines = false;

      foreach (var module in coverageData.RootElement.EnumerateObject())
      {
        if (module.Name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
        {
          hasModules = true;

          // Enumerate documents (source files)
          foreach (var document in module.Value.EnumerateObject())
          {
            // Enumerate classes
            foreach (var classEntry in document.Value.EnumerateObject())
            {
              // Enumerate methods
              foreach (var method in classEntry.Value.EnumerateObject())
              {
                // Check for Lines property
                if (method.Value.TryGetProperty("Lines", out var lines) &&
                    lines.ValueKind == JsonValueKind.Object &&
                    lines.EnumerateObject().Any())
                {
                  hasLines = true;
                  break;
                }
              }
              if (hasLines) break;
            }
            if (hasLines) break;
          }
        }
        if (hasLines) break;
      }

      Assert.True(hasModules,
        $"{CoverageJsonFileName} file has no modules (no .dll entries found at root level).\n" +
        $"Coverage file: {coverageFiles[0]}\n\n" +
        $"Test Output:\n{result.CombinedOutput}");

      Assert.True(hasLines,
        $"{CoverageJsonFileName} file has no line coverage data.\n" +
        $"Coverage file: {coverageFiles[0]}\n\n" +
        $"Test Output:\n{result.CombinedOutput}");
    }

    if (filename == CoverageCoberturaFileName)
    {
      // Cobertura XML format structure:
      // <coverage><sources><source>...</source></sources><packages><package><classes><class>...</class></classes></package></packages></coverage>
      XDocument coberturaDoc = XDocument.Load(coverageFiles[0]);
      Assert.NotNull(coberturaDoc.Root);
      Assert.True(coberturaDoc.Root.Name.LocalName == "coverage",
        $"{CoverageCoberturaFileName} XML root element is not 'coverage'");

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

      XElement? packagesElement = coberturaDoc.Root.Element("packages");
      Assert.True(
        packagesElement != null && packagesElement.Elements("package").Any(),
        $"{CoverageCoberturaFileName} XML 'packages' element is empty - no coverage data was collected.\n" +
        $"Coverage file: {coverageFiles[0]}\n\n" +
        $"Test Output:\n{result.CombinedOutput}");

      // Verify we have actual line coverage data
      bool hasLineCoverage = packagesElement
        .Descendants("line")
        .Any(line => line.Attribute("hits") != null);

      Assert.True(
        hasLineCoverage,
        $"{CoverageCoberturaFileName} XML has no line coverage data (no <line> elements with hits).\n" +
        $"Coverage file: {coverageFiles[0]}\n\n" +
        $"Test Output:\n{result.CombinedOutput}");
    }
  }

  #region Helper Methods

  private TestProjectInfo CreateTestProject(
    string testName,
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

    // Use test method name for folder naming (sanitize invalid path characters)
    string sanitizedTestName = SanitizePathName(testName);
    string solutionPath = Path.Combine(artifactsTemp, $"MTP_{sanitizedTestName}");

    // Clean up any existing folder from previous test runs
    if (Directory.Exists(solutionPath))
    {
      try
      {
        Directory.Delete(solutionPath, recursive: true);
      }
      catch (IOException)
      {
        // If deletion fails, append a short unique suffix
        solutionPath = Path.Combine(artifactsTemp, $"MTP_{sanitizedTestName}_{DateTime.Now:HHmmss}");
      }
    }

    Directory.CreateDirectory(solutionPath);

    // Create solution structure with separate SUT library and test project
    string sutProjectPath = Path.Combine(solutionPath, SutProjectName);
    string testProjectPath = Path.Combine(solutionPath, TestProjectName);
    Directory.CreateDirectory(sutProjectPath);
    Directory.CreateDirectory(testProjectPath);

    // Create NuGet.config at solution level
    CreateNugetConfig(solutionPath);

    // Get coverlet.MTP package version
    string coverletMtpVersion = GetCoverletMtpPackageVersion();

    // Create the SUT library project
    CreateSutLibraryProject(sutProjectPath, includeSimpleTest, includeMethodTests, includeCalculatorTest, includeBranchTest, includeMultipleClasses);

    // Create the test project with reference to SUT library
    CreateTestProjectFiles(testProjectPath, coverletMtpVersion, includeSimpleTest, includeMethodTests, includeCalculatorTest, includeBranchTest, includeMultipleTests, includeMultipleClasses);

    // Create solution file
    string solutionFile = Path.Combine(solutionPath, "TestSolution.sln");
    CreateSolutionFile(solutionFile);

    // Output path for test project: artifacts\tmp\debug\MTP_TestName\bin\TestProject\debug
    string outputPath = Path.Combine(solutionPath, "bin", TestProjectName, _buildConfiguration.ToLower());

    return new TestProjectInfo(solutionFile, testProjectPath, outputPath, solutionPath);
  }

  private static string SanitizePathName(string name)
  {
    // Replace invalid path characters with underscore
    char[] invalidChars = Path.GetInvalidFileNameChars();
    foreach (char c in invalidChars)
    {
      name = name.Replace(c, '_');
    }
    // Limit length to avoid path too long issues
    if (name.Length > 50)
    {
      name = name[..50];
    }
    return name;
  }

  private static void CreateSutLibraryProject(string sutProjectPath,
    bool includeSimpleTest,
    bool includeMethodTests,
    bool includeCalculatorTest,
    bool includeBranchTest,
    bool includeMultipleClasses)
  {
    // Create SUT library .csproj
    string sutCsproj = Path.Combine(sutProjectPath, $"{SutProjectName}.csproj");
    File.WriteAllText(sutCsproj, $@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <LangVersion>12.0</LangVersion>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <UseArtifactsOutput>true</UseArtifactsOutput>
    <ArtifactsPath>$(MSBuildThisFileDirectory)..</ArtifactsPath>
    <DebugType>portable</DebugType>
  </PropertyGroup>
</Project>");

    // Generate SUT code
    string sutCode = GenerateSutCode(includeSimpleTest, includeMethodTests, includeCalculatorTest, includeBranchTest, includeMultipleClasses);
    File.WriteAllText(Path.Combine(sutProjectPath, "SampleClasses.cs"), sutCode);
  }

  private static void CreateTestProjectFiles(string testProjectPath, string coverletMtpVersion,
    bool includeSimpleTest,
    bool includeMethodTests,
    bool includeCalculatorTest,
    bool includeBranchTest,
    bool includeMultipleTests,
    bool includeMultipleClasses)
  {
    // Relative path from test project to SUT project
    string relativeSutPath = Path.Combine("..", SutProjectName, $"{SutProjectName}.csproj");

    string testCsproj = Path.Combine(testProjectPath, $"{TestProjectName}.csproj");
    File.WriteAllText(testCsproj, $@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <LangVersion>12.0</LangVersion>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
    <UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner>
    <TestingPlatformDotnetTestSupport>true</TestingPlatformDotnetTestSupport>
    <OutputType>Exe</OutputType>
    <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>
    <UseArtifactsOutput>true</UseArtifactsOutput>
    <ArtifactsPath>$(MSBuildThisFileDirectory)..</ArtifactsPath>
    <DebugType>portable</DebugType>
    <Deterministic>false</Deterministic>
    <RestoreSources>
      https://api.nuget.org/v3/index.json;
      $(RepoRoot)artifacts/package/$(Configuration.ToLowerInvariant())
    </RestoreSources>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include=""{relativeSutPath}"" />
  </ItemGroup>
  <ItemGroup>
    <!-- Use xunit.v3.mtp-v2 which is designed for MTP v2.x -->
    <PackageReference Include=""xunit.v3.mtp-v2"" Version=""3.2.1"" />
    <PackageReference Include=""Microsoft.Testing.Platform"" Version=""2.0.2"" />
    <PackageReference Include=""coverlet.MTP"" Version=""{coverletMtpVersion}"" />
    <PackageReference Include=""Microsoft.Testing.Extensions.TrxReport"" Version=""2.0.2"" />
  </ItemGroup>
</Project>");

    // Generate test code
    string testCode = GenerateTestCode(includeSimpleTest, includeMethodTests, includeCalculatorTest, includeBranchTest, includeMultipleTests, includeMultipleClasses);
    File.WriteAllText(Path.Combine(testProjectPath, "Tests.cs"), testCode);
  }

  private static void CreateSolutionFile(string solutionFile)
  {
    string sutGuid = Guid.NewGuid().ToString("B").ToUpperInvariant();
    string testGuid = Guid.NewGuid().ToString("B").ToUpperInvariant();

    string solutionContent = $@"Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 17
VisualStudioVersion = 17.0.31903.59
MinimumVisualStudioVersion = 10.0.40219.1
Project(""{{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}}"") = ""{SutProjectName}"", ""{SutProjectName}\{SutProjectName}.csproj"", ""{sutGuid}""
EndProject
Project(""{{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}}"") = ""{TestProjectName}"", ""{TestProjectName}\{TestProjectName}.csproj"", ""{testGuid}""
EndProject
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Release|Any CPU = Release|Any CPU
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{sutGuid}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{sutGuid}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{sutGuid}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{sutGuid}.Release|Any CPU.Build.0 = Release|Any CPU
		{testGuid}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{testGuid}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{testGuid}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{testGuid}.Release|Any CPU.Build.0 = Release|Any CPU
	EndGlobalSection
	GlobalSection(SolutionProperties) = preSolution
		HideSolutionNode = FALSE
	EndGlobalSection
EndGlobal
";
    File.WriteAllText(solutionFile, solutionContent);
  }

  private void CreateNugetConfig(string solutionPath)
  {
    string nugetConfig = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <packageSources>
    <clear />
    <add key=""local"" value=""{_localPackagesPath}"" />
    <add key=""nuget.org"" value=""https://api.nuget.org/v3/index.json"" />
  </packageSources>
</configuration>";

    File.WriteAllText(Path.Combine(solutionPath, "NuGet.config"), nugetConfig);
  }

  private string GetCoverletMtpPackageVersion()
  {
    if (Directory.Exists(_localPackagesPath))
    {
      var mtpPackages = Directory.GetFiles(_localPackagesPath, "coverlet.MTP.*.nupkg");
      if (mtpPackages.Length > 0)
      {
        string packageName = Path.GetFileNameWithoutExtension(mtpPackages[0]);
        string version = packageName["coverlet.MTP.".Length..];
        return version;
      }
    }

    return "8.0.0-preview.*";
  }

  private static string GenerateSutCode(
    bool includeSimpleTest,
    bool includeMethodTests,
    bool includeCalculatorTest,
    bool includeBranchTest,
    bool includeMultipleClasses)
  {
    var codeBuilder = new System.Text.StringBuilder();
    codeBuilder.AppendLine("// Copyright (c) Toni Solarin-Sodara");
    codeBuilder.AppendLine("// Licensed under the MIT license. See LICENSE file in the project root for full license information.");
    codeBuilder.AppendLine();
    codeBuilder.AppendLine($"namespace {SutProjectName};");

    if (includeSimpleTest)
    {
      codeBuilder.AppendLine(@"/// <summary>
/// Simple math operations for testing basic coverage scenarios.
/// </summary>
public class SimpleMath
{
  public int Add(int a, int b)
  {
    return a + b;
  }

  public int Subtract(int a, int b)
  {
    return a - b;
  }
}");
    }

    if (includeMethodTests)
    {
      codeBuilder.AppendLine(@"
/// <summary>
/// System under test with calculation methods.
/// </summary>
public class SystemUnderTest
{
  public int Calculate(int x, int y)
  {
    int temp = x + y;
    return temp;
  }

  public int Multiply(int x, int y)
  {
    return x * y;
  }
}");
    }

    if (includeCalculatorTest)
    {
      codeBuilder.AppendLine(@"
/// <summary>
/// Calculator class with basic arithmetic operations.
/// </summary>
public class Calculator
{
  public int Add(int a, int b) => a + b;

  public int Multiply(int a, int b) => a * b;

  public int Divide(int a, int b)
  {
    if (b == 0)
      throw new DivideByZeroException();
    return a / b;
  }
}");
    }

    if (includeBranchTest)
    {
      codeBuilder.AppendLine(@"
/// <summary>
/// Class with branching logic for testing branch coverage.
/// </summary>
public class BranchLogic
{
  public string CheckValue(int value)
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

  public string GetGrade(int score)
  {
    return score switch
    {
      >= 90 => ""A"",
      >= 80 => ""B"",
      >= 70 => ""C"",
      >= 60 => ""D"",
      _ => ""F""
    };
  }
}");
    }

    if (includeMultipleClasses)
    {
      codeBuilder.AppendLine(@"
/// <summary>
/// Additional class for multi-class coverage scenarios.
/// </summary>
public class StringHelper
{
  public string Reverse(string input)
  {
    if (string.IsNullOrEmpty(input))
      return input;

    char[] chars = input.ToCharArray();
    Array.Reverse(chars);
    return new string(chars);
  }
}

/// <summary>
/// Class that should be excluded from coverage in some tests.
/// </summary>
public class ExcludedClass
{
  public void ExcludedMethod()
  {
    // This method might be excluded from coverage
  }
}");
    }

    return codeBuilder.ToString();
  }

  private static string GenerateTestCode(
    bool includeSimpleTest,
    bool includeMethodTests,
    bool includeCalculatorTest,
    bool includeBranchTest,
    bool includeMultipleTests,
    bool includeMultipleClasses)
  {
    var codeBuilder = new System.Text.StringBuilder();
    codeBuilder.AppendLine("// Copyright (c) Toni Solarin-Sodara");
    codeBuilder.AppendLine("// Licensed under the MIT license. See LICENSE file in the project root for full license information.");
    codeBuilder.AppendLine();
    codeBuilder.AppendLine("using Xunit;");
    codeBuilder.AppendLine($"using {SutProjectName};");
    codeBuilder.AppendLine();
    codeBuilder.AppendLine($"namespace {TestProjectName};");

    if (includeSimpleTest)
    {
      codeBuilder.AppendLine(@"
public class SimpleMathTests
{
  [Fact]
  public void Add_TwoPositiveNumbers_ReturnsSum()
  {
    // Arrange
    var math = new SimpleMath();

    // Act
    int result = math.Add(2, 3);

    // Assert
    Assert.Equal(5, result);
  }

  [Fact]
  public void Subtract_TwoNumbers_ReturnsDifference()
  {
    var math = new SimpleMath();
    int result = math.Subtract(10, 4);
    Assert.Equal(6, result);
  }
}");
    }

    if (includeMethodTests)
    {
      codeBuilder.AppendLine(@"
public class SystemUnderTestTests
{
  [Fact]
  public void Calculate_AddsTwoNumbers_ReturnsCorrectResult()
  {
    // Arrange
    var sut = new SystemUnderTest();

    // Act
    int result = sut.Calculate(10, 5);

    // Assert
    Assert.Equal(15, result);
  }

  [Fact]
  public void Multiply_TwoNumbers_ReturnsProduct()
  {
    var sut = new SystemUnderTest();
    int result = sut.Multiply(3, 4);
    Assert.Equal(12, result);
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

  [Fact]
  public void Calculator_Divide_ReturnsQuotient()
  {
    var calc = new Calculator();
    Assert.Equal(5, calc.Divide(20, 4));
  }

  [Fact]
  public void Calculator_DivideByZero_ThrowsException()
  {
    var calc = new Calculator();
    Assert.Throws<DivideByZeroException>(() => calc.Divide(10, 0));
  }
}");
    }

    if (includeBranchTest)
    {
      codeBuilder.AppendLine(@"
public class BranchLogicTests
{
  [Fact]
  public void CheckValue_PositiveNumber_ReturnsPositive()
  {
    var logic = new BranchLogic();
    string result = logic.CheckValue(10);
    Assert.Equal(""Positive"", result);
  }

  [Fact]
  public void CheckValue_NegativeNumber_ReturnsNegative()
  {
    var logic = new BranchLogic();
    string result = logic.CheckValue(-5);
    Assert.Equal(""Negative"", result);
  }

  [Fact]
  public void CheckValue_Zero_ReturnsZero()
  {
    var logic = new BranchLogic();
    string result = logic.CheckValue(0);
    Assert.Equal(""Zero"", result);
  }

  [Theory]
  [InlineData(95, ""A"")]
  [InlineData(85, ""B"")]
  [InlineData(75, ""C"")]
  [InlineData(65, ""D"")]
  [InlineData(50, ""F"")]
  public void GetGrade_VariousScores_ReturnsCorrectGrade(int score, string expectedGrade)
  {
    var logic = new BranchLogic();
    string result = logic.GetGrade(score);
    Assert.Equal(expectedGrade, result);
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
public class StringHelperTests
{
  [Fact]
  public void Reverse_ValidString_ReturnsReversed()
  {
    var helper = new StringHelper();
    string result = helper.Reverse(""hello"");
    Assert.Equal(""olleh"", result);
  }

  [Fact]
  public void Reverse_EmptyString_ReturnsEmpty()
  {
    var helper = new StringHelper();
    string result = helper.Reverse("""");
    Assert.Equal("""", result);
  }

  [Fact]
  public void Reverse_NullString_ReturnsNull()
  {
    var helper = new StringHelper();
    string? result = helper.Reverse(null!);
    Assert.Null(result);
  }
}");
    }

    return codeBuilder.ToString();
  }

  private async Task<int> BuildProject(string solutionPath)
  {
    var processStartInfo = new ProcessStartInfo
    {
      FileName = "dotnet",
      Arguments = $"build \"{solutionPath}\" -c {_buildConfiguration}",
      RedirectStandardOutput = true,
      RedirectStandardError = true,
      UseShellExecute = false,
      CreateNoWindow = true,
      WorkingDirectory = Path.GetDirectoryName(solutionPath)
    };

    using var process = Process.Start(processStartInfo);

    string output = await process!.StandardOutput.ReadToEndAsync();
    string error = await process.StandardError.ReadToEndAsync();

    await process.WaitForExitAsync();

    if (process.ExitCode != 0)
    {
      throw new InvalidOperationException($"Build failed:\nOutput: {output}\nError: {error}");
    }

    return process.ExitCode;
  }

  private static async Task<TestResult> RunTestsWithCoverage(TestProjectInfo testProject, string arguments, string testName)
  {
    string testExecutable = Path.Combine(testProject.OutputDirectory, $"{TestProjectName}.dll");

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

    string solutionDir = Path.GetDirectoryName(testProject.SolutionPath)!;

    // Exclude coverlet assemblies and test framework assemblies from instrumentation
    string excludeFilters = "--coverlet-exclude \"[coverlet.*]*\" --coverlet-exclude \"[xunit.*]*\" --coverlet-exclude \"[Microsoft.Testing.*]*\"";

    var processStartInfo = new ProcessStartInfo
    {
      FileName = "dotnet",
      Arguments = $"exec \"{testExecutable}\" {arguments} {excludeFilters} --diagnostic --diagnostic-verbosity trace --diagnostic-output-directory \"{solutionDir}\" --diagnostic-file-prefix {testName}\"",
      RedirectStandardOutput = true,
      RedirectStandardError = true,
      UseShellExecute = false,
      CreateNoWindow = true,
      WorkingDirectory = testProject.TestProjectPath
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
      13 => "exceeded number of maximum failed tests",
      _ => "unrecognized exit code"
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

  private static JsonDocument ParseCoverageJson(string filePath)
  {
    string jsonContent = File.ReadAllText(filePath);
    return JsonDocument.Parse(jsonContent);
  }

  #endregion

  /// <summary>
  /// Holds information about the generated test project structure.
  /// Implements cleanup to remove binary artifacts while preserving diagnostic and coverage files.
  /// </summary>
  private sealed class TestProjectInfo : IDisposable
  {
    public string SolutionPath { get; }
    public string TestProjectPath { get; }
    public string OutputDirectory { get; }
    public string SolutionDirectory { get; }

    // File extensions to preserve (diagnostic logs and coverage reports)
    private static readonly string[] s_preserveExtensions = [".txt", ".log", ".json", ".xml", ".info", ".cobertura.xml"];

    // Directories to remove (build artifacts)
    private static readonly string[] s_cleanupDirectories = ["bin", "obj"];

    public TestProjectInfo(string solutionPath, string testProjectPath, string outputDirectory, string solutionDirectory)
    {
      SolutionPath = solutionPath;
      TestProjectPath = testProjectPath;
      OutputDirectory = outputDirectory;
      SolutionDirectory = solutionDirectory;
    }

    public void Dispose()
    {
      CleanupBinaryArtifacts();
    }

    /// <summary>
    /// Removes binary artifacts (bin, obj folders) while preserving diagnostic and coverage files.
    /// </summary>
    private void CleanupBinaryArtifacts()
    {
      if (string.IsNullOrEmpty(SolutionDirectory) || !Directory.Exists(SolutionDirectory))
        return;

      try
      {
        // First, copy coverage and diagnostic files from bin to solution root (if needed)
        PreserveCoverageFiles();

        // Remove bin and obj directories
        foreach (string dirName in s_cleanupDirectories)
        {
          string dirPath = Path.Combine(SolutionDirectory, dirName);
          if (Directory.Exists(dirPath))
          {
            DeleteDirectoryWithRetry(dirPath);
          }
        }

        // Remove project directories (SampleLibrary, TestProject) but keep files at solution root
        string sutDir = Path.Combine(SolutionDirectory, SutProjectName);
        if (Directory.Exists(sutDir))
        {
          DeleteDirectoryWithRetry(sutDir);
        }

        string testDir = Path.Combine(SolutionDirectory, TestProjectName);
        if (Directory.Exists(testDir))
        {
          DeleteDirectoryWithRetry(testDir);
        }

        // Remove solution file and NuGet.config (keep only coverage/diagnostic files)
        TryDeleteFile(SolutionPath);
        TryDeleteFile(Path.Combine(SolutionDirectory, "NuGet.config"));
      }
      catch (Exception ex)
      {
        Debug.WriteLine($"Warning: Cleanup failed for {SolutionDirectory}: {ex.Message}");
      }
    }

    /// <summary>
    /// Ensures coverage files are available at solution root level.
    /// </summary>
    private void PreserveCoverageFiles()
    {
      if (!Directory.Exists(OutputDirectory))
        return;

      // Find and copy coverage files to solution root if they're only in bin
      foreach (string file in s_preserveExtensions
        .Select(extension => $"*{extension}")
        .SelectMany(pattern => Directory.GetFiles(OutputDirectory, pattern, SearchOption.AllDirectories)))
      {
        string fileName = Path.GetFileName(file);
        string destPath = Path.Combine(SolutionDirectory, fileName);

        // Only copy if not already at solution root
        if (!File.Exists(destPath))
        {
          try
          {
            File.Copy(file, destPath, overwrite: false);
          }
          catch
          {
            // Ignore copy failures
          }
        }
      }
    }
  

    private static void DeleteDirectoryWithRetry(string path, int maxRetries = 3)
    {
      for (int i = 0; i < maxRetries; i++)
      {
        try
        {
          // Clear read-only attributes
          foreach (string file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
          {
            File.SetAttributes(file, FileAttributes.Normal);
          }

          Directory.Delete(path, recursive: true);
          return;
        }
        catch (IOException) when (i < maxRetries - 1)
        {
          Thread.Sleep(100 * (i + 1));
        }
        catch (UnauthorizedAccessException) when (i < maxRetries - 1)
        {
          Thread.Sleep(100 * (i + 1));
        }
      }
    }

    private static void TryDeleteFile(string path)
    {
      try
      {
        if (File.Exists(path))
        {
          File.SetAttributes(path, FileAttributes.Normal);
          File.Delete(path);
        }
      }
      catch
      {
        // Ignore deletion failures
      }
    }
  }

  private sealed class TestResult
  {
    public int ExitCode { get; set; }
    public string ErrorText { get; set; } = string.Empty;
    public string StandardOutput { get; set; } = string.Empty;
    public string StandardError { get; set; } = string.Empty;
    public string CombinedOutput { get; set; } = string.Empty;
  }
}
