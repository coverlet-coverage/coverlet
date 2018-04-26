using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace Coverlet.Core.Instrumentation.Tests
{
    [ExcludeFromCodeCoverage]
    public class DocumentTest
    {
        [Fact]
        public void Ctor__PathIsEmpty()
        {
            // Arrange
            // Act
            var document = new Document();
            // Assert
            Assert.Null(document.Path);
        }
        [Fact]
        public void Ctor__LinesIsNotNull()
        {
            // Arrange
            // Act
            var document = new Document();
            // Assert
            Assert.NotNull(document.Lines);
        }
        [Fact]
        public void Ctor__LinesIsEmpty()
        {
            // Arrange
            // Act
            var document = new Document();
            // Assert
            Assert.Empty(document.Lines);
        }
    }
}