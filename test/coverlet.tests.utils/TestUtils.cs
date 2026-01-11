// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

  }
}
