// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Coverlet.Core.Abstractions;
using Coverlet.Core.Instrumentation;
using Microsoft.Extensions.DependencyModel;
using Moq;
using Xunit;

namespace Coverlet.CoreCoverage.Tests
{
  public partial class CoverageTests
  {
    [Fact]
    public void NetCoreSharedFrameworkResolverResolvesCurrentRuntimeAssembly()
    {
      string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
      Directory.CreateDirectory(tempDirectory);

      try
      {
        string modulePath = Path.Combine(tempDirectory, "copied.project.reference.dll");
        WriteSingleFrameworkRuntimeConfig(Path.Combine(tempDirectory, "testhost.runtimeconfig.json"));

        var logger = new Mock<ILogger>();
        var resolver = new NetCoreSharedFrameworkResolver(modulePath, logger.Object);
        AssemblyName textJsonAssembly = typeof(System.Text.Json.JsonSerializer).Assembly.GetName();
        var compilationLibrary = new CompilationLibrary(
          "package",
          textJsonAssembly.Name,
          textJsonAssembly.Version.ToString(),
          "sha512-not-relevant",
          Enumerable.Empty<string>(),
          Enumerable.Empty<Dependency>(),
          true);

        var assemblies = new List<string>();
        bool resolved = resolver.TryResolveAssemblyPaths(compilationLibrary, assemblies);

        Assert.True(resolved);
        Assert.NotEmpty(assemblies);
      }
      finally
      {
        Directory.Delete(tempDirectory, true);
      }
    }

    [Fact]
    public void RuntimeConfigurationReaderParsesFrameworksArray()
    {
      string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
      Directory.CreateDirectory(tempDirectory);

      try
      {
        string runtimeConfigPath = Path.Combine(tempDirectory, "testhost.runtimeconfig.json");
        File.WriteAllText(runtimeConfigPath,
          """
          {
            "runtimeOptions": {
              "tfm": "net8.0",
              "frameworks": [
                {
                  "name": "Microsoft.NETCore.App",
                  "version": "8.0.0"
                },
                {
                  "name": "Microsoft.AspNetCore.App",
                  "version": "8.0.0"
                }
              ]
            }
          }
          """);

        var reader = new RuntimeConfigurationReader(runtimeConfigPath);

        (string Name, string Version)[] frameworks = reader.GetFrameworks().ToArray();

        Assert.Equal(2, frameworks.Length);
        Assert.Equal("Microsoft.NETCore.App", frameworks[0].Name);
        Assert.Equal("8.0.0", frameworks[0].Version);
        Assert.Equal("Microsoft.AspNetCore.App", frameworks[1].Name);
        Assert.Equal("8.0.0", frameworks[1].Version);
      }
      finally
      {
        Directory.Delete(tempDirectory, true);
      }
    }

    private static void WriteSingleFrameworkRuntimeConfig(string runtimeConfigPath)
    {
      string runtimeVersion = new DirectoryInfo(Path.GetDirectoryName(typeof(object).Assembly.Location)!).Name;
      Version runtimeAssemblyVersion = typeof(object).Assembly.GetName().Version!;

      File.WriteAllText(runtimeConfigPath,
        $$"""
        {
          "runtimeOptions": {
            "tfm": "net{{runtimeAssemblyVersion.Major}}.{{runtimeAssemblyVersion.Minor}}",
            "framework": {
              "name": "Microsoft.NETCore.App",
              "version": "{{runtimeVersion}}"
            }
          }
        }
        """);
    }
  }
}
