// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NuGet.Packaging;
using Xunit;

namespace Coverlet.MTP.validation.tests;

[Collection(nameof(MtpValidationTests))]
public class PackageLayoutTests : MtpValidationTestBase
{
  private static readonly string[] s_supportedTfms = ["netstandard2.0", "net9.0", "net10.0"];

  [Fact]
  public void PackageContainsOnlySupportedFrameworkAssets()
  {
    using PackageArchiveReader reader = OpenPackage();

    string[] files = reader.GetFiles().Select(NormalizePath).ToArray();

    foreach (string targetFramework in s_supportedTfms)
    {
      Assert.Contains($"build/{targetFramework}/Codebelt.Coverlet.MTP.props", files);
      Assert.Contains($"build/{targetFramework}/Codebelt.Coverlet.MTP.targets", files);
      Assert.Contains($"buildTransitive/{targetFramework}/Codebelt.Coverlet.MTP.props", files);
      Assert.Contains($"buildTransitive/{targetFramework}/Codebelt.Coverlet.MTP.targets", files);
      Assert.Contains($"lib/{targetFramework}/coverlet.MTP.dll", files);
      Assert.Contains($"lib/{targetFramework}/coverlet.core.dll", files);
      Assert.Contains($"lib/{targetFramework}/Microsoft.Extensions.Configuration.dll", files);
      Assert.Contains($"lib/{targetFramework}/Microsoft.Extensions.Configuration.Json.dll", files);
      Assert.Contains($"lib/{targetFramework}/Microsoft.Extensions.DependencyModel.dll", files);
      Assert.Contains($"lib/{targetFramework}/Microsoft.Extensions.FileSystemGlobbing.dll", files);
      Assert.Contains($"lib/{targetFramework}/Mono.Cecil.dll", files);
      Assert.Contains($"lib/{targetFramework}/NuGet.Versioning.dll", files);
      Assert.Contains($"lib/{targetFramework}/System.Security.Cryptography.Pkcs.dll", files);
    }

    Assert.Contains("lib/netstandard2.0/System.Buffers.dll", files);
    Assert.Contains("lib/netstandard2.0/System.Memory.dll", files);
    Assert.Contains("lib/netstandard2.0/System.Text.Json.dll", files);

    Assert.DoesNotContain(files, file => file.Contains("net8.0", StringComparison.OrdinalIgnoreCase));
    Assert.DoesNotContain(files, file => file.Contains("coverlet.console", StringComparison.OrdinalIgnoreCase));
    Assert.DoesNotContain(files, file => file.Contains("coverlet.collector", StringComparison.OrdinalIgnoreCase));
    Assert.DoesNotContain(files, file => file.Contains("coverlet.msbuild", StringComparison.OrdinalIgnoreCase));
  }

  [Fact]
  public void NuspecContainsFrameworkAlignedDependencyGroups()
  {
    using PackageArchiveReader reader = OpenPackage();
    using Stream nuspecStream = reader.GetNuspec();

    Manifest manifest = Manifest.ReadFrom(nuspecStream, false);

    Assert.Equal("Codebelt.Coverlet.MTP", manifest.Metadata.Id);

    Dictionary<string, IReadOnlyDictionary<string, string>> dependencyGroups = manifest.Metadata.DependencyGroups
      .ToDictionary(
        group => group.TargetFramework.GetShortFolderName(),
        GetDependencies,
        StringComparer.OrdinalIgnoreCase);

    Assert.Equal(s_supportedTfms.Order(StringComparer.OrdinalIgnoreCase), dependencyGroups.Keys.Order(StringComparer.OrdinalIgnoreCase));

    AssertDependency(dependencyGroups["netstandard2.0"], "Microsoft.Extensions.Configuration", "8.0.0");
    AssertDependency(dependencyGroups["netstandard2.0"], "Microsoft.Extensions.Configuration.Json", "8.0.1");
    AssertDependency(dependencyGroups["netstandard2.0"], "Microsoft.Extensions.DependencyInjection", "8.0.1");
    AssertDependency(dependencyGroups["netstandard2.0"], "Microsoft.Testing.Platform", MtpPackageVersions.MicrosoftTestingPlatform);
    AssertDependency(dependencyGroups["netstandard2.0"], "System.Buffers", "4.6.1");
    AssertDependency(dependencyGroups["netstandard2.0"], "System.Memory", "4.6.3");

    AssertDependency(dependencyGroups["net9.0"], "Microsoft.Extensions.Configuration", "9.0.20");
    AssertDependency(dependencyGroups["net9.0"], "Microsoft.Extensions.Configuration.Json", "9.0.20");
    AssertDependency(dependencyGroups["net9.0"], "Microsoft.Extensions.DependencyInjection", "9.0.20");
    AssertDependency(dependencyGroups["net9.0"], "Microsoft.Testing.Platform", MtpPackageVersions.MicrosoftTestingPlatform);

    AssertDependency(dependencyGroups["net10.0"], "Microsoft.Extensions.Configuration", "10.0.12");
    AssertDependency(dependencyGroups["net10.0"], "Microsoft.Extensions.Configuration.Json", "10.0.12");
    AssertDependency(dependencyGroups["net10.0"], "Microsoft.Extensions.DependencyInjection", "10.0.12");
    AssertDependency(dependencyGroups["net10.0"], "Microsoft.Testing.Platform", MtpPackageVersions.MicrosoftTestingPlatform);
  }

  private PackageArchiveReader OpenPackage()
  {
    string version = GetCoverletMtpPackageVersion();
    string packagePath = Path.Combine(LocalPackagesPath, $"Codebelt.Coverlet.MTP.{version}.nupkg");
    return new PackageArchiveReader(packagePath);
  }

  private static IReadOnlyDictionary<string, string> GetDependencies(PackageDependencyGroup group)
  {
    return group.Packages.ToDictionary(
      dependency => dependency.Id,
      dependency => dependency.VersionRange.MinVersion?.ToNormalizedString()
        ?? throw new InvalidOperationException($"Dependency '{dependency.Id}' does not have a minimum version."),
      StringComparer.OrdinalIgnoreCase);
  }

  private static void AssertDependency(
    IReadOnlyDictionary<string, string> dependencies,
    string packageId,
    string expectedVersion)
  {
    Assert.True(
      dependencies.TryGetValue(packageId, out string? actualVersion),
      $"Expected dependency '{packageId}' was not found. Dependencies: {string.Join(", ", dependencies.Keys.Order(StringComparer.OrdinalIgnoreCase))}");

    Assert.Equal(expectedVersion, actualVersion);
  }

  private static string NormalizePath(string path)
  {
    return path.Replace('\\', '/');
  }
}
