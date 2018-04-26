
using System.Diagnostics.CodeAnalysis;
using Xunit;
using Coverlet.Core.Instrumentation;

namespace Coverlet.Core.Instrumentation.Tests
{
    [ExcludeFromCodeCoverage]
    public class InstrumenterResultTests
    {
        [Fact]
        public void Ctor__DocumentsIsNotNull()
        {
            // Arrange
            // Act
            var result = new InstrumenterResult();
            // Assert
            Assert.NotNull(result.Documents);
        }
        [Fact]
        public void Ctor__DocumentsIsEmpty()
        {
            // Arrange
            // Act
            var result = new InstrumenterResult();
            // Assert
            Assert.Empty(result.Documents);
        }

        [Fact]
        public void Ctor__ModuleIsNull()
        {
            // Arrange
            // Act
            var result = new InstrumenterResult();
            // Assert
            Assert.Null(result.Module);
        }
        [Fact]
        public void Ctor__ModulePathIsNull()
        {
            // Arrange
            // Act
            var result = new InstrumenterResult();
            // Assert
            Assert.Null(result.ModulePath);
        }
        [Fact]
        public void Ctor__HitsFilePathIsNull()
        {
            // Arrange
            // Act
            var result = new InstrumenterResult();
            // Assert
            Assert.Null(result.HitsFilePath);
        }
    }
}