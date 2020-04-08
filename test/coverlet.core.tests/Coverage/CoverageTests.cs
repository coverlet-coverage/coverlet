using System;
using System.IO;

using Coverlet.Core.Abstracts;
using Coverlet.Core.Helpers;
using Moq;
using Xunit;

namespace Coverlet.Core.Tests
{
    public partial class CoverageTests
    {
        private readonly Mock<ILogger> _mockLogger = new Mock<ILogger>();

        [Fact]
        public void TestCoverage()
        {
            string module = GetType().Assembly.Location;
            string pdb = Path.Combine(Path.GetDirectoryName(module), Path.GetFileNameWithoutExtension(module) + ".pdb");

            var directory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));

            File.Copy(module, Path.Combine(directory.FullName, Path.GetFileName(module)), true);
            File.Copy(pdb, Path.Combine(directory.FullName, Path.GetFileName(pdb)), true);

            // TODO: Find a way to mimick hits
            InstrumentationHelper instrumentationHelper =
                new InstrumentationHelper(new ProcessExitHandler(), new RetryHelper(), new FileSystem(), new Mock<ILogger>().Object,
                                          new SourceRootTranslator(module, new Mock<ILogger>().Object, new FileSystem()));

            var coverage = new Coverage(Path.Combine(directory.FullName, Path.GetFileName(module)), Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(),
                                        Array.Empty<string>(), false, false, string.Empty, false, _mockLogger.Object, instrumentationHelper, new FileSystem(), new SourceRootTranslator(_mockLogger.Object, new FileSystem()));
            coverage.PrepareModules();

            var result = coverage.GetCoverageResult();

            Assert.Empty(result.Modules);

            directory.Delete(true);
        }

        [Fact]
        public void TestCoverageWithTestAssembly()
        {
            string module = GetType().Assembly.Location;
            string pdb = Path.Combine(Path.GetDirectoryName(module), Path.GetFileNameWithoutExtension(module) + ".pdb");

            var directory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));

            File.Copy(module, Path.Combine(directory.FullName, Path.GetFileName(module)), true);
            File.Copy(pdb, Path.Combine(directory.FullName, Path.GetFileName(pdb)), true);

            InstrumentationHelper instrumentationHelper =
                new InstrumentationHelper(new ProcessExitHandler(), new RetryHelper(), new FileSystem(), new Mock<ILogger>().Object,
                                          new SourceRootTranslator(module, new Mock<ILogger>().Object, new FileSystem()));

            var coverage = new Coverage(Path.Combine(directory.FullName, Path.GetFileName(module)), Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(),
                                        Array.Empty<string>(), true, false, string.Empty, false, _mockLogger.Object, instrumentationHelper, new FileSystem(),
                                        new SourceRootTranslator(module, _mockLogger.Object, new FileSystem()));
            coverage.PrepareModules();

            var result = coverage.GetCoverageResult();

            Assert.NotEmpty(result.Modules);

            directory.Delete(true);
        }
    }
}