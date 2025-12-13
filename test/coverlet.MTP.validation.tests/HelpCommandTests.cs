// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Xml.Linq;
using Coverlet.Tests.Utils;
using NuGet.Packaging;
using Xunit;

namespace Coverlet.MTP.validation.tests;

/// <summary>
/// Tests to verify coverlet.MTP extension is properly loaded and command-line options are available.
/// These tests check the --help output to ensure the extension is registered with Microsoft Testing Platform.
/// Uses a dedicated test project in the TestProjects subdirectory for easier troubleshooting.
/// </summary>
public class HelpCommandTests
{
  private readonly string _buildConfiguration;
  private readonly string _buildTargetFramework;
  private readonly string _localPackagesPath;
  private const string PropsFileName = "MTPTest.props";
  private string[] _testProjectTfms = [];
  private static readonly string s_projectName = "coverlet.MTP.validation.tests";
  private const string SutName = "BasicTestProject";
  private readonly string _projectOutputPath = TestUtils.GetTestBinaryPath(s_projectName);
  private readonly string _testProjectPath;
  private readonly string _repoRoot ;

  public HelpCommandTests()
  {
    _buildConfiguration = "Debug";
    _buildTargetFramework = "net8.0";

    // Get repository root
    _repoRoot = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", ".."));
    _localPackagesPath = Path.Combine(_repoRoot, "artifacts", "packages", _buildConfiguration.ToLowerInvariant(), "Shipping");

    _projectOutputPath = Path.Combine(_repoRoot, "artifacts", "bin", s_projectName, _buildConfiguration.ToLowerInvariant()); 

    // Use dedicated test project in TestProjects subdirectory
    _testProjectPath = Path.Combine(
      Path.GetDirectoryName(typeof(HelpCommandTests).Assembly.Location)!,
      "TestProjects",
      "BasicTestProject");
  }

  private protected string GetPackageVersion(string filter)
  {
    string packagesPath = TestUtils.GetPackagePath(TestUtils.GetAssemblyBuildConfiguration().ToString().ToLowerInvariant());

    if (!Directory.Exists(packagesPath))
    {
      throw new DirectoryNotFoundException($"Package directory '{packagesPath}' not found, run 'dotnet pack' on repository root");
    }

    List<string> files = Directory.GetFiles(packagesPath, filter).ToList();
    if (files.Count == 0)
    {
      throw new InvalidOperationException($"Could not find any package using filter '{filter}' in folder '{Path.GetFullPath(packagesPath)}'. Make sure 'dotnet pack' was called.");
    }
    else if (files.Count > 1)
    {
      throw new InvalidOperationException($"Found more than one package using filter '{filter}' in folder '{Path.GetFullPath(packagesPath)}'. Make sure 'dotnet pack' was only called once.");
    }
    else
    {
      using Stream pkg = File.OpenRead(files[0]);
      using var reader = new PackageArchiveReader(pkg);
      using Stream nuspecStream = reader.GetNuspec();
      var manifest = Manifest.ReadFrom(nuspecStream, false);
      return manifest.Metadata.Version?.OriginalVersion ?? throw new InvalidOperationException("Version is null");
    }
  }

  private void CreateDeterministicTestPropsFile()
  {
    string propsFile = Path.Combine(_testProjectPath, PropsFileName);
    File.Delete(propsFile);

    XDocument deterministicTestProps = new();
    deterministicTestProps.Add(
            new XElement("Project",
                new XElement("PropertyGroup",
                    new XElement("coverletMTPVersion", GetPackageVersion("*MTP*.nupkg")))));

    string csprojPath = Path.Combine(_testProjectPath, SutName + ".csproj");
    XElement csproj = XElement.Load(csprojPath)!;

    // Use only the first top-level PropertyGroup in the project file
    XElement? firstPropertyGroup = csproj.Elements("PropertyGroup").FirstOrDefault();
    if (firstPropertyGroup is null)
      throw new InvalidOperationException("No top-level <PropertyGroup> found in project file.");

    // Prefer TargetFrameworks, fall back to single TargetFramework
    XElement? tfmsElement = firstPropertyGroup.Element("TargetFrameworks") ?? firstPropertyGroup.Element("TargetFramework");
    if (tfmsElement is null)
      throw new InvalidOperationException("No <TargetFrameworks> or <TargetFramework> element found in the first PropertyGroup.");

    _testProjectTfms = tfmsElement.Value.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

    Assert.Contains(_buildTargetFramework, _testProjectTfms);

    deterministicTestProps.Save(Path.Combine(propsFile));
  }

  [Fact]
  public async Task Help_ShowsCoverletMtpExtension()
  {
    CreateDeterministicTestPropsFile();
    // Arrange
    await EnsureTestProjectBuilt();

    // Act
    TestResult result = await RunTestsWithHelp();

    TestContext.Current.AddAttachment(
     "Test Output",
     result.CombinedOutput);

    // Assert
    Assert.Equal(0, result.ExitCode);
    Assert.Contains("Extension options:", result.StandardOutput);

    // Verify coverlet.MTP is loaded and shows its options
    Assert.Contains("--formats", result.StandardOutput);
  }

  [Fact]
  public async Task Help_ShowsFormatsOption()
  {
    // Arrange
    await EnsureTestProjectBuilt();

    // Act
    TestResult result = await RunTestsWithHelp();

    // Assert - Check for formats option from CoverletExtensionCommandLineProvider
    Assert.Contains("--formats", result.StandardOutput);
    Assert.Contains("Specifies the output formats for the coverage report", result.StandardOutput);
  }

  [Fact]
  public async Task Help_ShowsExcludeOption()
  {
    // Arrange
    await EnsureTestProjectBuilt();

    // Act
    TestResult result = await RunTestsWithHelp();

    TestContext.Current.AddAttachment(
     "Test Output",
     result.CombinedOutput);

    // Assert
    Assert.Contains("--exclude", result.StandardOutput);
    Assert.Contains("Filter expressions to exclude specific modules and types", result.StandardOutput);
  }

  [Fact]
  public async Task Help_ShowsIncludeOption()
  {
    // Arrange
    await EnsureTestProjectBuilt();

    // Act
    TestResult result = await RunTestsWithHelp();

    TestContext.Current.AddAttachment(
     "Test Output",
     result.CombinedOutput);

    // Assert
    Assert.Contains("--include", result.StandardOutput);
    Assert.Contains("Filter expressions to include only specific modules and type", result.StandardOutput);
  }

  [Fact]
  public async Task Help_ShowsExcludeByFileOption()
  {
    // Arrange
    await EnsureTestProjectBuilt();

    // Act
    TestResult result = await RunTestsWithHelp();

    TestContext.Current.AddAttachment(
     "Test Output",
     result.CombinedOutput);

    // Assert
    Assert.Contains("--exclude-by-file", result.StandardOutput);
    Assert.Contains("Glob patterns specifying source files to exclude", result.StandardOutput);
  }

  [Fact]
  public async Task Help_ShowsIncludeDirectoryOption()
  {
    // Arrange
    await EnsureTestProjectBuilt();

    // Act
    TestResult result = await RunTestsWithHelp();

    TestContext.Current.AddAttachment(
     "Test Output",
     result.CombinedOutput);

    // Assert
    Assert.Contains("--include-directory", result.StandardOutput);
    Assert.Contains("Include directories containing additional assemblies", result.StandardOutput);
  }

  [Fact]
  public async Task Help_ShowsExcludeByAttributeOption()
  {
    // Arrange
    await EnsureTestProjectBuilt();

    // Act
    TestResult result = await RunTestsWithHelp();

    TestContext.Current.AddAttachment(
     "Test Output",
     result.CombinedOutput);

    // Assert
    Assert.Contains("--exclude-by-attribute", result.StandardOutput);
    Assert.Contains("Attributes to exclude from code coverage", result.StandardOutput);
  }

  [Fact]
  public async Task Help_ShowsIncludeTestAssemblyOption()
  {
    // Arrange
    await EnsureTestProjectBuilt();

    // Act
    TestResult result = await RunTestsWithHelp();

    TestContext.Current.AddAttachment(
     "Test Output",
     result.CombinedOutput);

    // Assert
    Assert.Contains("--include-test-assembly", result.StandardOutput);
    Assert.Contains("Specifies whether to report code coverage of the test assembly", result.StandardOutput);
  }

  [Fact]
  public async Task Help_ShowsSingleHitOption()
  {
    // Arrange
    await EnsureTestProjectBuilt();

    // Act
    TestResult result = await RunTestsWithHelp();

    TestContext.Current.AddAttachment(
     "Test Output",
     result.CombinedOutput);

    // Assert
    Assert.Contains("--single-hit", result.StandardOutput);
    Assert.Contains("limit code coverage hit reporting to a single hit", result.StandardOutput);
  }

  [Fact]
  public async Task Help_ShowsSkipAutoPropsOption()
  {
    // Arrange
    await EnsureTestProjectBuilt();

    // Act
    TestResult result = await RunTestsWithHelp();

    TestContext.Current.AddAttachment(
     "Test Output",
     result.CombinedOutput);

    // Assert
    Assert.Contains("--skipautoprops", result.StandardOutput);
    Assert.Contains("Neither track nor record auto-implemented properties", result.StandardOutput);
  }

  [Fact]
  public async Task Help_ShowsDoesNotReturnAttributeOption()
  {
    // Arrange
    await EnsureTestProjectBuilt();

    // Act
    TestResult result = await RunTestsWithHelp();

    TestContext.Current.AddAttachment(
     "Test Output",
     result.CombinedOutput);

    // Assert
    Assert.Contains("--does-not-return-attribute", result.StandardOutput);
    Assert.Contains("Attributes that mark methods that do not return", result.StandardOutput);
  }

  [Fact]
  public async Task Help_ShowsExcludeAssembliesWithoutSourcesOption()
  {
    // Arrange
    await EnsureTestProjectBuilt();

    // Act
    TestResult result = await RunTestsWithHelp();

    TestContext.Current.AddAttachment(
     "Test Output",
     result.CombinedOutput);

    // Assert
    Assert.Contains("--exclude-assemblies-without-sources", result.StandardOutput);
    Assert.Contains("Specifies behavior of heuristic to ignore assemblies with missing source documents", result.StandardOutput);
  }

  [Fact]
  public async Task Help_ShowsSourceMappingFileOption()
  {
    // Arrange
    await EnsureTestProjectBuilt();

    // Act
    TestResult result = await RunTestsWithHelp();

    TestContext.Current.AddAttachment(
     "Test Output",
     result.CombinedOutput);

    // Assert
    Assert.Contains("--source-mapping-file", result.StandardOutput);
    Assert.Contains("Specifies the path to a SourceRootsMappings file", result.StandardOutput);
  }

  [Fact]
  public async Task Info_ShowsCoverletMtpExtension()
  {
    // Arrange
    await EnsureTestProjectBuilt();

    // Act
    TestResult result = await RunTestsWithInfo();

    TestContext.Current.AddAttachment(
     "Test Output",
     result.CombinedOutput);

    // Assert
    Assert.Equal(0, result.ExitCode);
    
    // Verify coverlet.MTP extension is listed in --info output
    Assert.Contains("coverlet", result.StandardOutput.ToLowerInvariant());
  }

  #region Helper Methods

  private async Task EnsureTestProjectBuilt()
  {
    // Verify test project exists
    string projectFile = Path.Combine(_testProjectPath, "BasicTestProject.csproj");
    if (!File.Exists(projectFile))
    {
      throw new InvalidOperationException(
        $"Test project not found at: {projectFile}\n" +
        $"Please ensure the TestProjects/BasicTestProject directory exists.");
    }

    // CRITICAL: Ensure packages are built BEFORE running tests
    EnsurePackagesBuilt();

    // Create version props file
    CreateDeterministicTestPropsFile();

    // Update NuGet.config to point to local packages
    UpdateNuGetConfig();

    // Clean any previous builds to avoid stale references
    await CleanProject(projectFile);

    // Restore packages
    await RestoreProject(projectFile);

    // Build the test project
    await BuildProject(projectFile);

    // Verify coverlet.MTP.dll was deployed
    VerifyCoverletMtpDeployed();
  }

  private void EnsurePackagesBuilt()
  {
    string packagesPath = TestUtils.GetPackagePath(
      TestUtils.GetAssemblyBuildConfiguration().ToString().ToLowerInvariant());

    // Check for coverlet.MTP
    string[] mtpPackages = Directory.GetFiles(packagesPath, "coverlet.MTP.*.nupkg");
    if (mtpPackages.Length == 0)
    {
      throw new InvalidOperationException(
        $"coverlet.MTP package not found in '{packagesPath}'.\n" +
        $"Run: dotnet pack src/coverlet.MTP -c {_buildConfiguration}");
    }
  }

  private async Task CleanProject(string projectPath)
  {
    var processStartInfo = new ProcessStartInfo
    {
      FileName = "dotnet",
      Arguments = $"clean \"{projectPath}\" -c {_buildConfiguration}",
      RedirectStandardOutput = true,
      RedirectStandardError = true,
      UseShellExecute = false,
      CreateNoWindow = true
    };

    using var process = Process.Start(processStartInfo);
    await process!.WaitForExitAsync();
  }

  private async Task RestoreProject(string projectPath)
  {
    var processStartInfo = new ProcessStartInfo
    {
      FileName = "dotnet",
      Arguments = $"restore \"{projectPath}\" --force --verbosity detailed",
      RedirectStandardOutput = true,
      RedirectStandardError = true,
      UseShellExecute = false,
      CreateNoWindow = true
    };

    using var process = Process.Start(processStartInfo);

    string output = await process!.StandardOutput.ReadToEndAsync();
    string error = await process.StandardError.ReadToEndAsync();

    await process.WaitForExitAsync();

    if (process.ExitCode != 0)
    {
      TestContext.Current?.AddAttachment(
        "Restore Output",
        $"STDOUT:\n{output}\n\nSTDERR:\n{error}");

      throw new InvalidOperationException(
        $"Restore failed with exit code {process.ExitCode}\n" +
        $"Output: {output}\n" +
        $"Error: {error}");
    }
  }

  private void VerifyCoverletMtpDeployed()
  {
    string binPath = GetSUTBinaryPath();

    string coverletMtpDll = Path.Combine(binPath, "coverlet.MTP.dll");
    string coverletCoreDll = Path.Combine(binPath, "coverlet.core.dll");

    if (!File.Exists(coverletMtpDll))
    {
      string[] deployedFiles = Directory.GetFiles(binPath, "*.dll");
      throw new InvalidOperationException(
        $"coverlet.MTP.dll not found in '{binPath}'.\n" +
        $"Deployed files:\n{string.Join("\n", deployedFiles.Select(f => $"  - {Path.GetFileName(f)}"))}");
    }

    if (!File.Exists(coverletCoreDll))
    {
      throw new InvalidOperationException(
        $"coverlet.core.dll not found in '{binPath}'. This is a dependency of coverlet.MTP.");
    }
  }

  private string GetSUTBinaryPath()
  {
    string binTestProjectPath = Path.Combine(_repoRoot, "artifacts", "bin", SutName);
    string binPath = Path.Combine(binTestProjectPath, _buildConfiguration);
    return binPath;
  }

  private void UpdateNuGetConfig()
  {
    string nugetConfigPath = Path.Combine(_testProjectPath, "NuGet.config");
    
    string nugetConfig = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <packageSources>
    <clear />
    <add key=""local"" value=""{_localPackagesPath}"" />
    <add key=""nuget.org"" value=""https://api.nuget.org/v3/index.json"" />
  </packageSources>
</configuration>";

    File.WriteAllText(nugetConfigPath, nugetConfig);
  }

private async Task<int> BuildProject(string projectPath)
  {
    var processStartInfo = new ProcessStartInfo
    {
      FileName = "dotnet",
      Arguments = $"build \"{projectPath}\" -c {_buildConfiguration} -f {_buildTargetFramework} --no-restore",
      RedirectStandardOutput = true,
      RedirectStandardError = true,
      UseShellExecute = false,
      CreateNoWindow = true
    };

    using var process = Process.Start(processStartInfo);

    string output = await process!.StandardOutput.ReadToEndAsync();
    string error = await process.StandardError.ReadToEndAsync();

    await process.WaitForExitAsync();

    if (process.ExitCode != 0)
    {
      // Attach build output to test results for debugging
      TestContext.Current?.AddAttachment(
        "Build Output",
        $"Exit Code: {process.ExitCode}\n\nSTDOUT:\n{output}\n\nSTDERR:\n{error}");

      throw new InvalidOperationException(
        $"Build failed with exit code {process.ExitCode}\n" +
        $"Output: {output}\n" +
        $"Error: {error}");
    }

    return process.ExitCode;
  }

  private async Task<TestResult> RunTestsWithHelp()
  {
    string testExecutable = Path.Combine(GetSUTBinaryPath(), SutName + ".dll");

    var processStartInfo = new ProcessStartInfo
    {
      FileName = "dotnet",
      Arguments = $"exec \"{testExecutable}\" --help",
      RedirectStandardOutput = true,
      RedirectStandardError = true,
      UseShellExecute = false,
      CreateNoWindow = true,
      WorkingDirectory = _testProjectPath
    };

    using var process = Process.Start(processStartInfo);

    string output = await process!.StandardOutput.ReadToEndAsync();
    string error = await process.StandardError.ReadToEndAsync();

    await process.WaitForExitAsync();

    return new TestResult
    {
      ExitCode = process.ExitCode,
      StandardOutput = output,
      StandardError = error,
      CombinedOutput = $"STDOUT:\n{output}\n\nSTDERR:\n{error}"
    };
  }

  private async Task<TestResult> RunTestsWithInfo()
  {
    string testExecutable = Path.Combine(GetSUTBinaryPath(), SutName + ".dll");

    var processStartInfo = new ProcessStartInfo
    {
      FileName = "dotnet",
      Arguments = $"exec \"{testExecutable}\" --info",
      RedirectStandardOutput = true,
      RedirectStandardError = true,
      UseShellExecute = false,
      CreateNoWindow = true,
      WorkingDirectory = _testProjectPath
    };

    using var process = Process.Start(processStartInfo);

    string output = await process!.StandardOutput.ReadToEndAsync();
    string error = await process.StandardError.ReadToEndAsync();

    await process.WaitForExitAsync();

    return new TestResult
    {
      ExitCode = process.ExitCode,
      StandardOutput = output,
      StandardError = error,
      CombinedOutput = $"STDOUT:\n{output}\n\nSTDERR:\n{error}"
    };
  }

  #endregion

  private class TestResult
  {
    public int ExitCode { get; set; }
    public string StandardOutput { get; set; } = string.Empty;
    public string StandardError { get; set; } = string.Empty;
    public string CombinedOutput { get; set; } = string.Empty;
  }
}
