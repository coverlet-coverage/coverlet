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

            File.Copy(module, Path.Combine(directory.FullName, Path.GetFileName(module)), true);

            var coverage = new Coverage(module, identifier);
            coverage.PrepareModules();

            var result = coverage.GetCoverageResult();
            Assert.Empty(result.Modules);

            directory.Delete(true);
        }
    }
}