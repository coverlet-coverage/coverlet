﻿// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Coverlet.Core.Abstractions;
using Coverlet.Core.Instrumentation;
using Coverlet.Tests.Xunit.Extensions;
using Microsoft.Extensions.DependencyModel;
using Moq;
using Xunit;

namespace Coverlet.Integration.Tests
{
  public class WpfResolverTests : BaseTest
  {
    [ConditionalFact]
    [SkipOnOS(OS.Linux, "WPF only runs on Windows")]
    [SkipOnOS(OS.MacOS, "WPF only runs on Windows")]
    public void TestInstrument_NetCoreSharedFrameworkResolver()
    {
      string wpfProjectPath = "../../../../coverlet.tests.projectsample.wpf6";
      Assert.True(DotnetCli($"build \"{wpfProjectPath}\"", out string output, out string error));
      string assemblyLocation = Directory.GetFiles($"{wpfProjectPath}/bin", "coverlet.tests.projectsample.wpf6.dll", SearchOption.AllDirectories).First();

      var mockLogger = new Mock<ILogger>();
      var resolver = new NetCoreSharedFrameworkResolver(assemblyLocation, mockLogger.Object);
      var compilationLibrary = new CompilationLibrary(
          "package",
          "System.Drawing",
          "0.0.0.0",
          "sha512-not-relevant",
          Enumerable.Empty<string>(),
          Enumerable.Empty<Dependency>(),
          true);

      var assemblies = new List<string>();
      Assert.True(resolver.TryResolveAssemblyPaths(compilationLibrary, assemblies),
          "sample assembly shall be resolved");
      Assert.NotEmpty(assemblies);
    }
  }
}
