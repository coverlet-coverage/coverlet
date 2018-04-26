using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;

using Xunit;
using Moq;

using Coverlet.Core;

namespace Coverlet.Core.Tests
{
    [ExcludeFromCodeCoverage]
    public class CoverageTests : IDisposable
    {
        private readonly string _module;
        private readonly string _identifier;
        private readonly DirectoryInfo _tempDirectory;
        private readonly string _tempModule;

        public CoverageTests()
        {
            _module = typeof(CoverageTests).Assembly.Location;
            _identifier = Guid.NewGuid().ToString();

            var tempDirectoryPath = Path.Combine(Path.GetTempPath(), _identifier);
            _tempDirectory = Directory.CreateDirectory(tempDirectoryPath);

            _tempModule = Path.Combine(_tempDirectory.FullName, Path.GetFileName(_module));

            File.Copy(_module, _tempModule, true);
        }

        [Fact]
        public void Coverage__CoverageResultModulesIsNotNull()
        {
            // Arrange
            var coverage = new Coverage(_tempModule, _identifier);
            coverage.PrepareModules();
            // Act
            var result = coverage.GetCoverageResult();
            // Assert
            Assert.NotNull(result.Modules);
        }

        [Fact]
        public void Coverage__CoverageResultModulesIsEmpty()
        {
            // Arrange
            var coverage = new Coverage(_tempModule, _identifier);
            coverage.PrepareModules();
            // Act
            var result = coverage.GetCoverageResult();
            // Assert
            Assert.Empty(result.Modules);
        }

        public void Dispose()
        {
            _tempDirectory.Delete(true);
        }
    }
}