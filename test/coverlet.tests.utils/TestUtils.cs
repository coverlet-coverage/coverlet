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

    public static string GetTestProjectPath( string directoryName )
    {
      return Path.GetFullPath(Path.Join(AppContext.BaseDirectory, "../../../../test", directoryName));
    }

    public static string GetTestBinaryPath( string directoryName )
    {
      return Path.GetFullPath(Path.Join(AppContext.BaseDirectory, "../..", directoryName));
    }

    public static string GetPackagePath(string buildConfiguration)
    {
      return Path.GetFullPath(Path.Join(AppContext.BaseDirectory, "../../../package", buildConfiguration));
    }

    public static string GetTestResultsPath(string directoryName)
    {
      return Path.GetFullPath(Path.Join(AppContext.BaseDirectory, "../../../tests", directoryName));
    }

  }
}
