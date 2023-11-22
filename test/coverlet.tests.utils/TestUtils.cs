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

    public static string GetTestProjectPath(string directoryName)
    {
      return Path.Join(Path.GetFullPath(Path.Join(AppContext.BaseDirectory, s_rel4Parents)), "test", directoryName);
    }

    public static string GetTestBinaryPath(string directoryName)
    {
      return Path.Join(Path.GetFullPath(Path.Join(AppContext.BaseDirectory, s_rel2Parents)), directoryName);
    }

    public static string GetPackagePath(string buildConfiguration)
    {
      return Path.Join(Path.GetFullPath(Path.Join(AppContext.BaseDirectory, s_rel3Parents)), "package", buildConfiguration);
    }

    public static string GetTestResultsPath(string directoryName)
    {
      return Path.Join(Path.GetFullPath(Path.Join(AppContext.BaseDirectory, s_rel3Parents)), "tests", directoryName);
    }

  }
}
