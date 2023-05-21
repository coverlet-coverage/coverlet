// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Coverlet.Core.Abstractions;
using Coverlet.Core.Instrumentation;
using Coverlet.Integration.Tests;
using Coverlet.Tests.Xunit.Extensions;
using Microsoft.Extensions.DependencyModel;
using Moq;
using Xunit;

namespace coverlet.integration.tests
{
    public class WpfResolverTests : BaseTest
    {
        [ConditionalFact]
        [SkipOnOS(OS.Linux, "WPF only runs on Windows")]
        [SkipOnOS(OS.MacOS, "WPF only runs on Windows")]
        public void TestInstrument_NetCoreSharedFrameworkResolver()
        {
            string wpfProjectPath = "../../../../coverlet.tests.projectsample.wpf";
            DotnetCli($"build \"{wpfProjectPath}\"", out string _, out string _);
            string assemblyLocation = Directory.GetFiles($"{wpfProjectPath}/bin", "coverlet.tests.projectsample.wpf.dll", SearchOption.AllDirectories).First();

            var mockLogger = new Mock<ILogger>();
            var resolver = new NetCoreSharedFrameworkResolver(assemblyLocation, mockLogger.Object);
            var compilationLibrary = new CompilationLibrary(
                "package",
                "System.Drawing",
                "5.0.17.0",
                "sha512-B2WqEox8o+4KUOpL7rZPyh6qYjik8tHi2tN8Z9jZkHzED8ElYgZa/h6K+xliB435SqUcWT290Fr2aa8BtZjn8A==",
                Enumerable.Empty<string>(),
                Enumerable.Empty<Dependency>(),
                true);

            var assemblies = new List<string>();
            Assert.True(resolver.TryResolveAssemblyPaths(compilationLibrary, assemblies),
                $"sample assembly shall be resolved - AssemblyLocation: {assemblyLocation}");
            Assert.NotEmpty(assemblies);
        }
    }
}
