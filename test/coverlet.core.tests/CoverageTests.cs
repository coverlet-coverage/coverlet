using System;
using System.IO;

using Xunit;
using Moq;

using Coverlet.Core;

namespace Coverlet.Core.Tests
{
    public class CoverageTests
    {
        [Fact]
        public void TestCoverage()
        {
            string module = typeof(CoverageTests).Assembly.Location;
            string identifier = Guid.NewGuid().ToString();

            var directory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), identifier));
            var tempModule = Path.Combine(directory.FullName, Path.GetFileName(module));

            File.Copy(module, tempModule, true);

            var coverage = new Coverage(tempModule, identifier, null);
            coverage.PrepareModules();

            var result = coverage.GetCoverageResult();
            Assert.Empty(result.Modules);

            directory.Delete(true);
        }
    }
}