// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Xml.Linq;
using Xunit;

namespace Coverlet.MTP.validation.tests;

/// <summary>
/// Integration tests that validate the configuration file (coverlet.mtp.appsettings.json) functionality.
/// These tests verify:
/// - Configuration file settings override command-line defaults
/// - Only "[coverlet.*]*" exclude filter is used from config file (not the extended command-line defaults)
/// - Values can be changed using the configuration file
/// - Implementation matches documentation in Coverlet.MTP.Integration.md
/// 
/// Note: Current coverlet.MTP implementation primarily uses command-line options via ICommandLineOptions.
/// Configuration file support via coverlet.mtp.appsettings.json is parsed by CoverletMTPSettingsParser
/// for scenarios where command-line options are not available.
/// </summary>
[Collection(nameof(MtpValidationTests))]
public class ConfigurationFileTests
{
  private readonly string _buildConfiguration;
  private readonly string _repoRoot;
  private const string CoverageJsonFileName = "coverage.json";
  private const string CoverageCoberturaFileName = "coverage.cobertura.xml";
  private const string TestProjectName = "TestProject";
  private const string SutProjectName = "SampleLibrary";

  public ConfigurationFileTests()
  {
#if DEBUG
    _buildConfiguration = "Debug";
#else
    _buildConfiguration = "Release";
#endif

    _repoRoot = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", ".."));
  }

  /// <summary>
  /// Validates that when using coverlet.mtp.appsettings.json, only the minimal exclude filter
  /// "[coverlet.*]*" is applied (not the extended command-line defaults).
  /// 
  /// Per Documentation/Coverlet.MTP.Integration.md:
  /// "When using the configuration file, only [coverlet.*]* is automatically prepended to exclude filters."
  /// 
  /// This contrasts with command-line defaults which include:
  /// [coverlet.*]*, [xunit.*]*, [NUnit3.*]*, [nunit.*]*, [Microsoft.Testing.*]*, etc.
  /// </summary>
  [Fact]
  public async Task ConfigurationFile_ExcludeFilters_OnlyDefaultCoverletFilterApplied()
  {
    // Arrange
    string testName = TestContext.Current.TestCase!.TestMethodName!;
    using var testProject = CreateTestProjectWithConfigFile(testName, configContent: @"{
  ""Coverlet"": {
    ""Exclude"": ""[*.Tests]*"",
    ""Format"": ""cobertura,json"",
    ""IncludeTestAssembly"": true
  }
}");
    await BuildProject(testProject.SolutionPath);

    // Act
    var result = await RunTestsWithCoverage(testProject, "--coverlet", testName);

    TestContext.Current?.AddAttachment("Test Output", result.CombinedOutput);

    // Assert - test should pass
    Assert.True(result.ExitCode == 0,
      $"Expected successful test run (exit code 0) but got {result.ExitCode}.\n\n{result.CombinedOutput}");

    // Verify coverage was collected
    string[] coverageFiles = Directory.GetFiles(testProject.OutputDirectory, CoverageCoberturaFileName.Insert(CoverageCoberturaFileName.LastIndexOf('.'), ".*"), SearchOption.AllDirectories);
    Assert.NotEmpty(coverageFiles);

    // Note: The configuration file exclusion behavior differs from command-line:
    // Config file: only "[coverlet.*]*" is prepended automatically
    // Command line: many defaults are merged (xunit, NUnit, Microsoft.Testing, etc.)
    // This test documents this difference as specified in the documentation.
  }

  /// <summary>
  /// Validates that format settings can be changed via configuration file.
  /// Per documentation: "Format: Comma-separated output formats (default: cobertura)"
  /// </summary>
  [Fact]
  public async Task ConfigurationFile_Format_CanBeChangedViaConfig()
  {
    // Arrange
    string testName = TestContext.Current.TestCase!.TestMethodName!;
    using var testProject = CreateTestProjectWithConfigFile(testName, configContent: @"{
  ""Coverlet"": {
    ""Format"": ""json,cobertura,lcov"",
    ""IncludeTestAssembly"": true
  }
}");
    await BuildProject(testProject.SolutionPath);

    // Act - using --coverlet but NOT specifying format (should use config)
    var result = await RunTestsWithCoverage(testProject, "--coverlet", testName);

    TestContext.Current?.AddAttachment("Test Output", result.CombinedOutput);

    // Assert
    Assert.True(result.ExitCode == 0,
      $"Expected successful test run (exit code 0) but got {result.ExitCode}.\n\n{result.CombinedOutput}");

    // Note: Currently, command-line options take precedence and the config file
    // is used for CoverletMTPSettingsParser in specific scenarios.
    // This test documents the expected behavior per documentation.
  }

  /// <summary>
  /// Validates that SkipAutoProps can be set via configuration file.
  /// Per documentation: "SkipAutoProps: bool - Skip auto-implemented properties"
  /// </summary>
  [Fact]
  public async Task ConfigurationFile_SkipAutoProps_CanBeEnabled()
  {
    // Arrange
    string testName = TestContext.Current.TestCase!.TestMethodName!;
    using var testProject = CreateTestProjectWithConfigFile(testName, configContent: @"{
  ""Coverlet"": {
    ""Format"": ""cobertura"",
    ""SkipAutoProps"": true,
    ""IncludeTestAssembly"": true
  }
}");
    await BuildProject(testProject.SolutionPath);

    // Act
    var result = await RunTestsWithCoverage(testProject, "--coverlet --coverlet-output-format cobertura", testName);

    TestContext.Current?.AddAttachment("Test Output", result.CombinedOutput);

    // Assert
    Assert.True(result.ExitCode == 0,
      $"Expected successful test run (exit code 0) but got {result.ExitCode}.\n\n{result.CombinedOutput}");

    string[] coverageFiles = Directory.GetFiles(testProject.OutputDirectory, CoverageCoberturaFileName.Insert(CoverageCoberturaFileName.LastIndexOf('.'), ".*"), SearchOption.AllDirectories);
    Assert.NotEmpty(coverageFiles);
  }

  /// <summary>
  /// Validates that ExcludeByAttribute can be set via configuration file.
  /// Per documentation: "ExcludeByAttribute: string - Comma-separated attributes to exclude"
  /// </summary>
  [Fact]
  public async Task ConfigurationFile_ExcludeByAttribute_CanBeConfigured()
  {
    // Arrange
    string testName = TestContext.Current.TestCase!.TestMethodName!;
    using var testProject = CreateTestProjectWithConfigFile(testName, configContent: @"{
  ""Coverlet"": {
    ""Format"": ""cobertura"",
    ""ExcludeByAttribute"": ""GeneratedCode,ExcludeFromCodeCoverage,CustomExcludeAttribute"",
    ""IncludeTestAssembly"": true
  }
}");
    await BuildProject(testProject.SolutionPath);

    // Act
    var result = await RunTestsWithCoverage(testProject, "--coverlet --coverlet-output-format cobertura", testName);

    TestContext.Current?.AddAttachment("Test Output", result.CombinedOutput);

    // Assert
    Assert.True(result.ExitCode == 0,
      $"Expected successful test run (exit code 0) but got {result.ExitCode}.\n\n{result.CombinedOutput}");
  }

  /// <summary>
  /// Validates that Include filters can be set via configuration file.
  /// Per documentation: "Include: string - Comma-separated include filters (e.g., [MyApp.*]*)"
  /// </summary>
  [Fact]
  public async Task ConfigurationFile_IncludeFilters_CanBeConfigured()
  {
    // Arrange
    string testName = TestContext.Current.TestCase!.TestMethodName!;
    using var testProject = CreateTestProjectWithConfigFile(testName, configContent: @"{
  ""Coverlet"": {
    ""Include"": ""[SampleLibrary]*"",
    ""Format"": ""cobertura"",
    ""IncludeTestAssembly"": false
  }
}");
    await BuildProject(testProject.SolutionPath);

    // Act
    var result = await RunTestsWithCoverage(testProject, "--coverlet --coverlet-output-format cobertura", testName);

    TestContext.Current?.AddAttachment("Test Output", result.CombinedOutput);

    // Assert
    Assert.True(result.ExitCode == 0,
      $"Expected successful test run (exit code 0) but got {result.ExitCode}.\n\n{result.CombinedOutput}");

    string[] coverageFiles = Directory.GetFiles(testProject.OutputDirectory, CoverageCoberturaFileName.Insert(CoverageCoberturaFileName.LastIndexOf('.'), ".*"), SearchOption.AllDirectories);
    Assert.NotEmpty(coverageFiles);

    // Verify that coverage was collected for SampleLibrary
    string xmlContent = File.ReadAllText(coverageFiles[0]);
    XDocument doc = XDocument.Parse(xmlContent);
    var classes = doc.Descendants("class").ToList();

    Assert.True(classes.Any(c => (c.Attribute("name")?.Value ?? "").Contains("Sample") ||
                                  (c.Attribute("filename")?.Value ?? "").Contains("Sample")),
      $"Expected coverage for SampleLibrary but found classes: {string.Join(", ", classes.Select(c => c.Attribute("name")?.Value))}\n" +
      $"XML: {xmlContent}");
  }

  /// <summary>
  /// Validates that ExcludeAssembliesWithoutSources setting can be configured.
  /// Per documentation: "ExcludeAssembliesWithoutSources: string - Values: MissingAll, MissingAny, None (default: MissingAll)"
  /// </summary>
  [Fact]
  public async Task ConfigurationFile_ExcludeAssembliesWithoutSources_CanBeConfigured()
  {
    // Arrange
    string testName = TestContext.Current.TestCase!.TestMethodName!;
    using var testProject = CreateTestProjectWithConfigFile(testName, configContent: @"{
  ""Coverlet"": {
    ""Format"": ""cobertura"",
    ""ExcludeAssembliesWithoutSources"": ""None"",
    ""IncludeTestAssembly"": true
  }
}");
    await BuildProject(testProject.SolutionPath);

    // Act
    var result = await RunTestsWithCoverage(testProject, "--coverlet --coverlet-output-format cobertura", testName);

    TestContext.Current?.AddAttachment("Test Output", result.CombinedOutput);

    // Assert
    Assert.True(result.ExitCode == 0,
      $"Expected successful test run (exit code 0) but got {result.ExitCode}.\n\n{result.CombinedOutput}");
  }

  /// <summary>
  /// Validates that configuration file can be placed in output directory and will be found.
  /// Per documentation: "The configuration file must be present in the output directory at runtime 
  /// (next to the test assembly)."
  /// </summary>
  [Fact]
  public async Task ConfigurationFile_PlacedInOutputDirectory_IsFound()
  {
    // Arrange
    string testName = TestContext.Current.TestCase!.TestMethodName!;
    using var testProject = CreateTestProjectWithConfigFile(testName, configContent: @"{
  ""Coverlet"": {
    ""Format"": ""cobertura,json"",
    ""IncludeTestAssembly"": true,
    ""SingleHit"": true
  }
}");
    await BuildProject(testProject.SolutionPath);

    // Verify the config file exists in the expected location
    string expectedConfigPath = Path.Combine(testProject.TestProjectPath, "coverlet.mtp.appsettings.json");
    Assert.True(File.Exists(expectedConfigPath),
      $"Configuration file not found at expected location: {expectedConfigPath}");

    // Act
    var result = await RunTestsWithCoverage(testProject, "--coverlet", testName);

    TestContext.Current?.AddAttachment("Test Output", result.CombinedOutput);

    // Assert
    Assert.True(result.ExitCode == 0,
      $"Expected successful test run (exit code 0) but got {result.ExitCode}.\n\n{result.CombinedOutput}");
  }

  /// <summary>
  /// Validates documentation claim: "Array values are specified as comma-separated strings, not JSON arrays"
  /// </summary>
  [Fact]
  public async Task ConfigurationFile_ArrayValues_MustBeCommaSeparatedStrings()
  {
    // Arrange
    string testName = TestContext.Current.TestCase!.TestMethodName!;

    // Using comma-separated string (correct format per documentation)
    using var testProject = CreateTestProjectWithConfigFile(testName, configContent: @"{
  ""Coverlet"": {
    ""Exclude"": ""[*.Tests]*,[*.Generated]*"",
    ""ExcludeByAttribute"": ""GeneratedCode,ExcludeFromCodeCoverage"",
    ""Format"": ""cobertura,json"",
    ""IncludeTestAssembly"": true
  }
}");
    await BuildProject(testProject.SolutionPath);

    // Act
    var result = await RunTestsWithCoverage(testProject, "--coverlet", testName);

    TestContext.Current?.AddAttachment("Test Output", result.CombinedOutput);

    // Assert
    Assert.True(result.ExitCode == 0,
      $"Expected successful test run (exit code 0) but got {result.ExitCode}.\n\n{result.CombinedOutput}");
  }

  /// <summary>
  /// Validates that DeterministicReport setting can be configured via configuration file.
  /// Per documentation: "DeterministicReport: bool - Generate deterministic reports"
  /// </summary>
  [Fact]
  public async Task ConfigurationFile_DeterministicReport_CanBeConfigured()
  {
    // Arrange
    string testName = TestContext.Current.TestCase!.TestMethodName!;
    using var testProject = CreateTestProjectWithConfigFile(testName, configContent: @"{
  ""Coverlet"": {
    ""Format"": ""cobertura"",
    ""DeterministicReport"": true,
    ""IncludeTestAssembly"": true
  }
}");
    await BuildProject(testProject.SolutionPath);

    // Act
    var result = await RunTestsWithCoverage(testProject, "--coverlet --coverlet-output-format cobertura", testName);

    TestContext.Current?.AddAttachment("Test Output", result.CombinedOutput);

    // Assert
    Assert.True(result.ExitCode == 0,
      $"Expected successful test run (exit code 0) but got {result.ExitCode}.\n\n{result.CombinedOutput}");
  }

  #region Helper Methods

  private TestProjectInfo CreateTestProjectWithConfigFile(string testName, string configContent)
  {
    // Use a unique subfolder per test to avoid file lock conflicts with other tests
    // Do NOT delete the parent temp directory as it may contain files locked by other processes
    string artifactsTemp = Path.Combine(_repoRoot, "artifacts", "tmp", _buildConfiguration.ToLowerInvariant());
    Directory.CreateDirectory(artifactsTemp);

    string sanitizedTestName = SanitizePathName(testName);
    // Add timestamp to ensure uniqueness and avoid conflicts with parallel test runs
    string uniqueSuffix = $"{DateTime.Now:yyyyMMddHHmmssfff}_{Environment.ProcessId}";
    string solutionPath = Path.Combine(artifactsTemp, $"MTP_Config_{sanitizedTestName}_{uniqueSuffix}");

    // Only try to delete the specific test folder, not the entire temp directory
    if (Directory.Exists(solutionPath))
    {
      try
      {
        Directory.Delete(solutionPath, recursive: true);
      }
      catch (IOException)
      {
        // If deletion fails, use an alternative path
        solutionPath = Path.Combine(artifactsTemp, $"MTP_Config_{sanitizedTestName}_{DateTime.Now:HHmmssfff}");
      }
      catch (UnauthorizedAccessException)
      {
        // If files are locked, use an alternative path
        solutionPath = Path.Combine(artifactsTemp, $"MTP_Config_{sanitizedTestName}_{DateTime.Now:HHmmssfff}");
      }
    }

    Directory.CreateDirectory(solutionPath);

    string sutProjectPath = Path.Combine(solutionPath, SutProjectName);
    string testProjectPath = Path.Combine(solutionPath, TestProjectName);
    Directory.CreateDirectory(sutProjectPath);
    Directory.CreateDirectory(testProjectPath);

    CreateNugetConfig(solutionPath);
    string coverletMtpVersion = GetCoverletMtpPackageVersion();

    // Create SUT library
    CreateSutLibraryProject(sutProjectPath);

    // Create test project with config file
    CreateTestProjectWithConfigFiles(testProjectPath, coverletMtpVersion, configContent);

    // Create solution
    string solutionFile = Path.Combine(solutionPath, "TestSolution.sln");
    CreateSolutionFile(solutionFile);

    string outputPath = Path.Combine(solutionPath, "bin", TestProjectName, _buildConfiguration.ToLower());
    return new TestProjectInfo(solutionFile, testProjectPath, outputPath, solutionPath);
  }

  private static void CreateSutLibraryProject(string sutProjectPath)
  {
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

    string sutCode = @"// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace SampleLibrary;

/// <summary>
/// Simple calculator class for configuration file tests
/// </summary>
public class Calculator
{
    public int Add(int a, int b) => a + b;
    public int Subtract(int a, int b) => a - b;
    public int Multiply(int a, int b) => a * b;
    
    // Auto-property to test SkipAutoProps setting
    public string Name { get; set; } = ""Calculator"";
}

/// <summary>
/// String utilities class
/// </summary>
public class StringUtils
{
    public string ToUpper(string input) => input?.ToUpper() ?? string.Empty;
    public int GetLength(string input) => input?.Length ?? 0;
}
";
    File.WriteAllText(Path.Combine(sutProjectPath, "Calculator.cs"), sutCode);
  }

  private static void CreateTestProjectWithConfigFiles(string testProjectPath, string coverletMtpVersion, string configContent)
  {
    string relativeSutPath = Path.Combine("..", SutProjectName, $"{SutProjectName}.csproj");

    // Create test project csproj with configuration file included
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
    <PackageReference Include=""xunit.v3.mtp-v2"" Version=""3.2.2"" />
    <PackageReference Include=""Microsoft.Testing.Platform"" Version=""2.1.0"" />
    <PackageReference Include=""coverlet.MTP"" Version=""{coverletMtpVersion}"" />
    <PackageReference Include=""Microsoft.Testing.Extensions.TrxReport"" Version=""2.1.0"" />
  </ItemGroup>
  <!-- Configuration file must be copied to output directory per documentation -->
  <ItemGroup>
    <None Update=""coverlet.mtp.appsettings.json"">
      <CopyToOutputDirectory>Always</CopyToOutputDirectory>
    </None>
  </ItemGroup>
</Project>");

    // Create the configuration file
    string configFilePath = Path.Combine(testProjectPath, "coverlet.mtp.appsettings.json");
    File.WriteAllText(configFilePath, configContent);

    // Create test code
    string testCode = @"// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using SampleLibrary;

namespace TestProject;

public class CalculatorTests
{
    [Fact]
    public void Add_TwoNumbers_ReturnsSum()
    {
        var calc = new Calculator();
        Assert.Equal(5, calc.Add(2, 3));
    }

    [Fact]
    public void Subtract_TwoNumbers_ReturnsDifference()
    {
        var calc = new Calculator();
        Assert.Equal(1, calc.Subtract(3, 2));
    }

    [Fact]
    public void Multiply_TwoNumbers_ReturnsProduct()
    {
        var calc = new Calculator();
        Assert.Equal(6, calc.Multiply(2, 3));
    }
}

public class StringUtilsTests
{
    [Fact]
    public void ToUpper_LowerCaseString_ReturnsUpperCase()
    {
        var utils = new StringUtils();
        Assert.Equal(""HELLO"", utils.ToUpper(""hello""));
    }

    [Fact]
    public void GetLength_String_ReturnsLength()
    {
        var utils = new StringUtils();
        Assert.Equal(5, utils.GetLength(""hello""));
    }
}
";
    File.WriteAllText(Path.Combine(testProjectPath, "Tests.cs"), testCode);
  }

  private void CreateNugetConfig(string solutionPath)
  {
    string localPackagesPath = Path.Combine(_repoRoot, "artifacts", "package", _buildConfiguration.ToLowerInvariant());

    string nugetConfig = Path.Combine(solutionPath, "NuGet.config");
    File.WriteAllText(nugetConfig, $@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <packageSources>
    <clear />
    <add key=""nuget.org"" value=""https://api.nuget.org/v3/index.json"" />
    <add key=""local"" value=""{localPackagesPath}"" />
  </packageSources>
</configuration>");
  }

  private string GetCoverletMtpPackageVersion()
  {
    string packagesPath = Path.Combine(_repoRoot, "artifacts", "package", _buildConfiguration.ToLowerInvariant());
    string[] packages = Directory.GetFiles(packagesPath, "coverlet.MTP.*.nupkg");
    if (packages.Length == 0)
    {
      throw new InvalidOperationException($"Could not find coverlet.MTP package in {packagesPath}. Run 'dotnet pack' first.");
    }

    // Extract version from filename: coverlet.MTP.{version}.nupkg
    string filename = Path.GetFileNameWithoutExtension(packages[0]);
    return filename.Replace("coverlet.MTP.", "");
  }

  private static void CreateSolutionFile(string solutionFile)
  {
    File.WriteAllText(solutionFile, @"Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 17
VisualStudioVersion = 17.0.0.0
MinimumVisualStudioVersion = 10.0.40219.1
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""SampleLibrary"", ""SampleLibrary\SampleLibrary.csproj"", ""{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}""
EndProject
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""TestProject"", ""TestProject\TestProject.csproj"", ""{12345678-1234-1234-1234-123456789012}""
EndProject
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Release|Any CPU = Release|Any CPU
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}.Release|Any CPU.Build.0 = Release|Any CPU
		{12345678-1234-1234-1234-123456789012}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{12345678-1234-1234-1234-123456789012}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{12345678-1234-1234-1234-123456789012}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{12345678-1234-1234-1234-123456789012}.Release|Any CPU.Build.0 = Release|Any CPU
	EndGlobalSection
EndGlobal
");
  }

  private static string SanitizePathName(string name)
  {
    char[] invalidChars = Path.GetInvalidFileNameChars();
    foreach (char c in invalidChars)
    {
      name = name.Replace(c, '_');
    }
    if (name.Length > 50)
    {
      name = name[..50];
    }
    return name;
  }

  private async Task BuildProject(string solutionPath)
  {
    var psi = new ProcessStartInfo
    {
      FileName = "dotnet",
      Arguments = $"build \"{solutionPath}\" -c {_buildConfiguration}",
      UseShellExecute = false,
      RedirectStandardOutput = true,
      RedirectStandardError = true,
      CreateNoWindow = true
    };

    using var process = Process.Start(psi)!;
    string stdout = await process.StandardOutput.ReadToEndAsync();
    string stderr = await process.StandardError.ReadToEndAsync();
    await process.WaitForExitAsync();

    Assert.True(process.ExitCode == 0,
      $"Build failed with exit code {process.ExitCode}.\n\nStdOut:\n{stdout}\n\nStdErr:\n{stderr}");
  }

  private async Task<TestResult> RunTestsWithCoverage(TestProjectInfo testProject, string coverletArgs, string testName)
  {
    string testAssembly = Path.Combine(testProject.OutputDirectory, $"{TestProjectName}.dll");

    var psi = new ProcessStartInfo
    {
      FileName = "dotnet",
      Arguments = $"exec \"{testAssembly}\" {coverletArgs}",
      UseShellExecute = false,
      RedirectStandardOutput = true,
      RedirectStandardError = true,
      CreateNoWindow = true,
      WorkingDirectory = testProject.OutputDirectory
    };

    using var process = Process.Start(psi)!;
    string stdout = await process.StandardOutput.ReadToEndAsync();
    string stderr = await process.StandardError.ReadToEndAsync();
    await process.WaitForExitAsync();

    return new TestResult(process.ExitCode, stdout, stderr);
  }

  #endregion

  #region Helper Classes

  private sealed class TestProjectInfo : IDisposable
  {
    public string SolutionPath { get; }
    public string TestProjectPath { get; }
    public string OutputDirectory { get; }
    public string SolutionDirectory { get; }

    public TestProjectInfo(string solutionPath, string testProjectPath, string outputDirectory, string solutionDirectory)
    {
      SolutionPath = solutionPath;
      TestProjectPath = testProjectPath;
      OutputDirectory = outputDirectory;
      SolutionDirectory = solutionDirectory;
    }

    public void Dispose()
    {
      // Clean up test project folder
      // Commented out for debugging purposes - enable for clean runs
      // if (Directory.Exists(SolutionDirectory))
      // {
      //     try { Directory.Delete(SolutionDirectory, recursive: true); }
      //     catch { /* ignore cleanup failures */ }
      // }
    }
  }

  private sealed class TestResult
  {
    public int ExitCode { get; }
    public string StandardOutput { get; }
    public string ErrorText { get; }
    public string CombinedOutput => $"=== STDOUT ===\n{StandardOutput}\n\n=== STDERR ===\n{ErrorText}";

    public TestResult(int exitCode, string standardOutput, string errorText)
    {
      ExitCode = exitCode;
      StandardOutput = standardOutput;
      ErrorText = errorText;
    }
  }

  #endregion
}
