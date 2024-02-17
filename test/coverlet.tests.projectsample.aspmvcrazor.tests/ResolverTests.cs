// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Reflection;
using Coverlet.Core.Abstractions;
using Coverlet.Core.Instrumentation;
using Microsoft.Extensions.DependencyModel;
using Moq;
using Xunit;

namespace coverlet.tests.projectsample.aspmvcrazor.tests
{
  public class ResolverTests
  {
    [Fact]
    public void TestInstrument_NetCoreSharedFrameworkResolver()
    {
      Assembly assembly = GetType().Assembly;
      Mock<ILogger> mockLogger = new();
      NetCoreSharedFrameworkResolver resolver = new(assembly.Location, mockLogger.Object);
      CompilationLibrary compilationLibrary = new(
          "package",
          "Microsoft.AspNetCore.Mvc.Razor",
          "0.0.0.0",
          "sha512-not-relevant",
          [],
          [],
          true);

      List<string> assemblies = [];
      Assert.True(resolver.TryResolveAssemblyPaths(compilationLibrary, assemblies),
          "sample assembly shall be resolved");
      Assert.NotEmpty(assemblies);
    }
  }
}
