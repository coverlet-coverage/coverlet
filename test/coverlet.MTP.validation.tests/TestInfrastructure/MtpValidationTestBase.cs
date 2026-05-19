// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Text;
using Xunit;

namespace Coverlet.MTP.validation.tests;

/// <summary>
/// Shared infrastructure for MTP integration tests that scaffold temporary test solutions.
/// Both <see cref="CollectCoverageTests"/> and <see cref="ConfigurationFileTests"/> inherit this class.
/// </summary>
public abstract class MtpValidationTestBase
{
    protected const string TestProjectName = "TestProject";
    protected const string SutProjectName = "SampleLibrary";

    protected readonly string BuildConfiguration;
    protected readonly string RepoRoot;
    protected readonly string LocalPackagesPath;

    protected MtpValidationTestBase()
    {
#if DEBUG
        BuildConfiguration = "Debug";
#else
        BuildConfiguration = "Release";
#endif
        RepoRoot = Path.GetFullPath(
            Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", ".."));
        LocalPackagesPath = Path.Combine(
            RepoRoot, "artifacts", "package", BuildConfiguration.ToLowerInvariant());
    }

    // ── Package version ──────────────────────────────────────────────────────

    /// <summary>
    /// Resolves the coverlet.MTP package version from locally built artifacts.
    /// Throws if the package directory is missing or contains more than one package.
    /// </summary>
    protected string GetCoverletMtpPackageVersion()
    {
        if (!Directory.Exists(LocalPackagesPath))
        {
            throw new DirectoryNotFoundException(
                $"Local packages path '{LocalPackagesPath}' not found. Run 'dotnet pack' first.");
        }

        string[] packages = Directory.GetFiles(LocalPackagesPath, "coverlet.MTP.*.nupkg");
        if (packages.Length == 0)
        {
            throw new InvalidOperationException(
                $"Could not find coverlet.MTP package in '{LocalPackagesPath}'. Run 'dotnet pack' first.");
        }

        if (packages.Length > 1)
        {
            throw new InvalidOperationException(
                $"Found {packages.Length} coverlet.MTP packages in '{LocalPackagesPath}'. Expected exactly one.");
        }

        string filename = Path.GetFileNameWithoutExtension(packages[0]);
        return filename["coverlet.MTP.".Length..];
    }

    // ── Solution directory helpers ───────────────────────────────────────────

    /// <summary>
    /// Creates a unique solution directory under <paramref name="artifactsTemp"/>.
    /// If the directory already exists from a previous run it is deleted first.
    /// On deletion failure a timestamp-suffixed alternative path is used.
    /// </summary>
    protected static string CreateSolutionDirectory(
        string artifactsTemp,
        string prefix,
        string sanitizedTestName)
    {
        string path = Path.Combine(artifactsTemp, $"{prefix}{sanitizedTestName}");

        if (Directory.Exists(path))
        {
            try
            {
                Directory.Delete(path, recursive: true);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                path = Path.Combine(artifactsTemp,
                    $"{prefix}{sanitizedTestName}_{DateTime.Now:HHmmssfff}");
            }
        }

        Directory.CreateDirectory(path);
        return path;
    }

    /// <summary>Removes invalid path characters and trims to 50 characters.</summary>
    protected static string SanitizePathName(string name)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(c, '_');
        }

        return name.Length > 50 ? name[..50] : name;
    }

    // ── NuGet.config / solution file ─────────────────────────────────────────

    /// <summary>
    /// Writes a <c>NuGet.config</c> that uses the local artifacts folder as the primary feed
    /// followed by nuget.org.
    /// </summary>
    protected void CreateNugetConfig(string solutionPath)
    {
        File.WriteAllText(Path.Combine(solutionPath, "NuGet.config"), $"""
            <?xml version="1.0" encoding="utf-8"?>
            <configuration>
              <packageSources>
                <clear />
                <add key="local" value="{LocalPackagesPath}" />
                <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
              </packageSources>
            </configuration>
            """);
    }

    /// <summary>
    /// Writes a two-project <c>.sln</c> file for <c>SampleLibrary</c> and <c>TestProject</c>
    /// using freshly generated GUIDs so parallel test runs never collide.
    /// </summary>
    protected static void CreateSolutionFile(string solutionFile)
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

    // ── Csproj generator ─────────────────────────────────────────────────────

    /// <summary>
    /// Generates a test project <c>.csproj</c> whose package versions are read from
    /// <c>Directory.Packages.props</c> via <see cref="MtpPackageVersions"/>.
    /// </summary>
    /// <param name="coverletMtpVersion">The version of the locally built <c>coverlet.MTP</c> package.</param>
    /// <param name="relativeSutPath">Relative path from the test project to the SUT <c>.csproj</c>.</param>
    /// <param name="additionalNoneItems">
    /// Optional config files that need to be copied to the output directory,
    /// e.g. <c>("coverlet.mtp.appsettings.json", "Always")</c>.
    /// </param>
    protected static string GenerateTestCsproj(
        string coverletMtpVersion,
        string relativeSutPath,
        IEnumerable<(string FileName, string CopyToOutput)>? additionalNoneItems = null)
    {
        string mtpVersion = MtpPackageVersions.MicrosoftTestingPlatform;
        string xunitVersion = MtpPackageVersions.XunitV3;
        string noneItemGroup = BuildNoneItemGroup(additionalNoneItems);

        return $"""
            <Project Sdk="Microsoft.NET.Sdk">
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
                <ProjectReference Include="{relativeSutPath}" />
              </ItemGroup>
              <ItemGroup>
                <PackageReference Include="xunit.v3.mtp-v2" Version="{xunitVersion}" />
                <PackageReference Include="Microsoft.Testing.Platform" Version="{mtpVersion}" />
                <PackageReference Include="coverlet.MTP" Version="{coverletMtpVersion}" />
                <PackageReference Include="Microsoft.Testing.Extensions.TrxReport" Version="{mtpVersion}" />
              </ItemGroup>
            {noneItemGroup}</Project>
            """;
    }

    private static string BuildNoneItemGroup(
        IEnumerable<(string FileName, string CopyToOutput)>? items)
    {
        if (items is null)
        {
            return string.Empty;
        }

        var sb = new StringBuilder("  <ItemGroup>\n");
        foreach ((string file, string copy) in items)
        {
            sb.AppendLine($"""    <None Update="{file}"><CopyToOutputDirectory>{copy}</CopyToOutputDirectory></None>""");
        }

        sb.AppendLine("  </ItemGroup>");
        return sb.ToString();
    }

    // ── Build / run helpers ──────────────────────────────────────────────────

    /// <summary>Builds the solution at <paramref name="solutionPath"/> and asserts success.</summary>
    protected async Task BuildProjectAsync(string solutionPath)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"build \"{solutionPath}\" -c {BuildConfiguration}",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        using var process = Process.Start(psi)!;

        // Read both streams concurrently to avoid deadlock.
        // Sequential reads can deadlock if one buffer fills while waiting for the other.
        // See: https://learn.microsoft.com/dotnet/api/system.diagnostics.process.standardoutput#remarks
        Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync();
        Task<string> stderrTask = process.StandardError.ReadToEndAsync();

        await Task.WhenAll(stdoutTask, stderrTask, process.WaitForExitAsync());

        Assert.True(
            process.ExitCode == 0,
            $"Build failed with exit code {process.ExitCode}.\n\nStdOut:\n{await stdoutTask}\n\nStdErr:\n{await stderrTask}");
    }
}
