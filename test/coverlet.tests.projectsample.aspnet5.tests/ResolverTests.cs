// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Coverlet.Core.Abstractions;
using Coverlet.Core.Instrumentation;
using Microsoft.Extensions.DependencyModel;
using Moq;
using Xunit;

namespace coverlet.tests.projectsample.aspnet5.tests
{
    public class ResolverTests
    {
        [Fact]
        public void TestInstrument_NetCoreSharedFrameworkResolver()
        {
            Assembly assembly = GetType().Assembly;
            var mockLogger = new Mock<ILogger>();
            var resolver = new NetCoreSharedFrameworkResolver(assembly.Location, mockLogger.Object);
            var compilationLibrary = new CompilationLibrary(
                "package",
                "Microsoft.Extensions.Logging.Abstractions",
                "2.2.0",
                "sha512-B2WqEox8o+4KUOpL7rZPyh6qYjik8tHi2tN8Z9jZkHzED8ElYgZa/h6K+xliB435SqUcWT290Fr2aa8BtZjn8A==",
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
