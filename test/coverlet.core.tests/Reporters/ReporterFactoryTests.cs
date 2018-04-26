using System;
using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace Coverlet.Core.Reporters.Tests
{
    [ExcludeFromCodeCoverage]
    public class ReporterFactoryTests
    {
        [Fact]
        public void CreateReporter__JsonReporter__CreatedReporterHasValidType()
        {
            // Arrange
            var reporterFactory = new ReporterFactory();
            // Act
            var reporter = reporterFactory.CreateReporter("json");
            // Assert
            var validReporterType = typeof(JsonReporter);
            Assert.Equal(validReporterType, reporter.GetType());
        }

        [Fact]
        public void CreateReporter__LcovReporter__CreatedReporterHasValidType()
        {
            // Arrange
            var reporterFactory = new ReporterFactory();
            // Act
            var reporter = reporterFactory.CreateReporter("lcov");
            // Assert
            var validReporterType = typeof(LcovReporter);
            Assert.Equal(validReporterType, reporter.GetType());
        }

        [Fact]
        public void CreateReporter__OpenCoverReporter__CreatedReporterHasValidType()
        {
            // Arrange
            var reporterFactory = new ReporterFactory();
            // Act
            var reporter = reporterFactory.CreateReporter("opencover");
            // Assert
            var validReporterType = typeof(OpenCoverReporter);
            Assert.Equal(validReporterType, reporter.GetType());
        }

        [Fact]
        public void CreateReporter__CoberturaReporter__CreatedReporterHasValidType()
        {
            // Arrange
            var reporterFactory = new ReporterFactory();
            // Act
            var reporter = reporterFactory.CreateReporter("cobertura");
            // Assert
            var validReporterType = typeof(CoberturaReporter);
            Assert.Equal(validReporterType, reporter.GetType());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void CreateReporter__EmptyReporter__CreatedReporterIsNull(string reporterType)
        {
            // Arrange
            var reporterFactory = new ReporterFactory();
            // Act
            var reporter = reporterFactory.CreateReporter(reporterType);
            // Assert
            Assert.Null(reporter);
        }
    }
}