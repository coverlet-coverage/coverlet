using System;
using System.IO;
using Coverlet.Core.Logging;
using Xunit;
using Moq;


namespace Coverlet.Core.Tests
{
    public class CoverageTests
    {
        [Fact]
        public void TestCoverage()
        {
            string module = GetType().Assembly.Location;
            string pdb = Path.Combine(Path.GetDirectoryName(module), Path.GetFileNameWithoutExtension(module) + ".pdb");

            var directory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));

            File.Copy(module, Path.Combine(directory.FullName, Path.GetFileName(module)), true);
            File.Copy(pdb, Path.Combine(directory.FullName, Path.GetFileName(pdb)), true);

            // TODO: Find a way to mimick hits

            IInstrumenter instrumenter = new Instrumenter(Path.Combine(directory.FullName, Path.GetFileName(module)), Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(), false, false, string.Empty, false, new Mock<ILogger>().Object);
            InstrumenterState instrumenterState = instrumenter.PrepareModules();

            ICoverageCalculator coverageResult = new CoverageCalculator(instrumenterState, new Mock<ILogger>().Object);
            var result = coverageResult.GetCoverageResult();

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

            // TODO: Find a way to mimick hits

            IInstrumenter instrumenter = new Instrumenter(Path.Combine(directory.FullName, Path.GetFileName(module)), Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(), true, false, string.Empty, false, new Mock<ILogger>().Object);
            InstrumenterState instrumenterState = instrumenter.PrepareModules();

            ICoverageCalculator coverageResult = new CoverageCalculator(instrumenterState, new Mock<ILogger>().Object);
            var result = coverageResult.GetCoverageResult();

            Assert.NotEmpty(result.Modules);

            directory.Delete(true);
        }
    }
}