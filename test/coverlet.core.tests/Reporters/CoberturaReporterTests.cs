using System;
using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace Coverlet.Core.Reporters.Tests
{
    [ExcludeFromCodeCoverage]
    public class CoberturaReporterTests
    {
        [Fact]
        public void Ctor__Format__IsCobertura()
        {
            // Arrange
            // Act
            var reporter = new CoberturaReporter();
            // Assert
            Assert.Equal("cobertura", reporter.Format);
        }
        [Fact]
        public void Ctor__Extension__IsCobertura()
        {
            // Arrange
            // Act
            var reporter = new CoberturaReporter();
            // Assert
            Assert.Equal("xml", reporter.Extension);
        }

        [Fact]
        public void Report__CoverageResultExists__ReturnedReportStringIsNotNullOrEmpty()
        {
            // Arrange
            var lines = new Lines
            {
                {1, new LineInfo {Hits = 1}},
                {2, new LineInfo {Hits = 0}}
            };
            var methods = new Methods
            {
                {"System.Void Coverlet.Core.Reporters.Tests.CoberturaReporterTests::TestReport()", lines}
            };
            var classes = new Classes
            {
                {"Coverlet.Core.Reporters.Tests.CoberturaReporterTests", methods}
            };
            var documents = new Documents
            {
                {"doc.cs", classes}
            };
            var coverageResult = new CoverageResult
            {
                Identifier = Guid.NewGuid().ToString(),
                Modules = new Modules
                {
                    {"module", documents}
                }
            };
            var reporter = new CoberturaReporter();
            // Act
            var report = reporter.Report(coverageResult);
            // Assert
            Assert.False(string.IsNullOrEmpty(report));
        }
    }
}