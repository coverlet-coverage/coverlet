// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System;
using System.IO;

namespace Coverlet.Tests.Utils
{

  [Flags]
  public enum BuildConfiguration
  {
    Debug = 1,
    Release = 2
  }

  static class TestUtils
  {
    private static readonly string s_relParent = ".." + Path.DirectorySeparatorChar;
    private static readonly string s_rel2Parents = s_relParent + s_relParent;
    private static readonly string s_rel3Parents = s_rel2Parents + s_relParent;
    private static readonly string s_rel4Parents = s_rel3Parents + s_relParent;
    public static BuildConfiguration GetAssemblyBuildConfiguration()
    {
#if DEBUG
      return BuildConfiguration.Debug;
#endif
#if RELEASE
      return BuildConfiguration.Release;
#endif
      throw new NotSupportedException($"Build configuration not supported");
    }

    public static string GetAssemblyTargetFramework()
    {
#if NET8_0
      return "net8.0";
#endif
#if NET9_0
      return "net9.0";
#endif
#if NET10_0
      return "net10.0";
#endif
      throw new NotSupportedException($"Build configuration not supported");
    }

    public static string GetBuildConfigurationString()
    {
      // Returns lowercase configuration string to match MSBuild output paths on case-sensitive filesystems
      return GetAssemblyBuildConfiguration().ToString().ToLower();
    }

    public static string GetTestProjectPath(string directoryName)
    {
#if NETSTANDARD2_0
      return Path.Combine(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, s_rel4Parents)), "test", directoryName);
#else
      return Path.Join(Path.GetFullPath(Path.Join(AppContext.BaseDirectory, s_rel4Parents)), "test", directoryName);
#endif
    }

    public static string GetTestBinaryPath(string directoryName)
    {
#if NETSTANDARD2_0
      return Path.Combine(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, s_rel2Parents)), directoryName);
#else
      return Path.Join(Path.GetFullPath(Path.Join(AppContext.BaseDirectory, s_rel2Parents)), directoryName);
#endif
    }

    public static string GetPackagePath(string buildConfiguration)
    {
#if NETSTANDARD2_0
      return Path.Combine(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, s_rel3Parents)), "package", buildConfiguration);
#else
      return Path.Join(Path.GetFullPath(Path.Join(AppContext.BaseDirectory, s_rel3Parents)), "package", buildConfiguration);
#endif
    }

    public static string GetTestResultsPath()
    {
#if NETSTANDARD2_0
      return Path.Combine(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, s_rel3Parents)), "reports");
#else
      return Path.Join(Path.GetFullPath(Path.Join(AppContext.BaseDirectory, s_rel3Parents)), "reports");
#endif
    }

    public static string GetRepositoryRootPath()
    {
#if NETSTANDARD2_0
      return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, s_rel4Parents));
#else
      return Path.GetFullPath(Path.Join(AppContext.BaseDirectory, s_rel4Parents));
#endif
    }

    /// <summary>
    /// Reads the SDK version from global.json in the repository root.
    /// </summary>
    /// <returns>The SDK version string (e.g., "10.0.201") or null if not found.</returns>
    public static string? GetSdkVersionFromGlobalJson()
    {
      string globalJsonPath = Path.Combine(GetRepositoryRootPath(), "global.json");
      if (!File.Exists(globalJsonPath))
      {
        return null;
      }

      string content = File.ReadAllText(globalJsonPath);
      // Simple parsing - look for "version": "x.y.z"
      const string versionKey = "\"version\":";
      int versionIndex = content.IndexOf(versionKey, StringComparison.OrdinalIgnoreCase);
      if (versionIndex < 0)
      {
        return null;
      }

      int startQuote = content.IndexOf('"', versionIndex + versionKey.Length);
      if (startQuote < 0)
      {
        return null;
      }

      int endQuote = content.IndexOf('"', startQuote + 1);
      if (endQuote < 0)
      {
        return null;
      }

      return content.Substring(startQuote + 1, endQuote - startQuote - 1);
    }

    /// <summary>
    /// Checks if the SDK version from global.json indicates .NET 10 or later.
    /// </summary>
    public static bool IsNet10OrLater()
    {
      string? sdkVersion = GetSdkVersionFromGlobalJson();
      if (string.IsNullOrEmpty(sdkVersion))
      {
        return false;
      }

      // Parse major version (first number before the dot)
      int dotIndex = sdkVersion!.IndexOf('.');
#if NETSTANDARD2_0
      string majorVersionStr = dotIndex > 0 ? sdkVersion.Substring(0, dotIndex) : sdkVersion;
#else
      string majorVersionStr = dotIndex > 0 ? sdkVersion[..dotIndex] : sdkVersion;
#endif
      return int.TryParse(majorVersionStr, out int majorVersion) && majorVersion >= 10;
    }

  }
}
