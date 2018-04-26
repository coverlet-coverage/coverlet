using System;
using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace Coverlet.Core.Reporters.Tests
{
    [ExcludeFromCodeCoverage]
    public class LcovReporterTests
    {
        [Fact]
        public void Ctor__FormatIsValid()
        {
            // Arrange
            // Act
            var reporter = new LcovReporter();
            // Assert
            Assert.Equal("lcov", reporter.Format);
        }
        [Fact]
        public void Ctor__ExtensionIsValid()
        {
            // Arrange
            // Act
            var reporter = new LcovReporter();
            // Assert
            Assert.Equal("info", reporter.Extension);
        }

        [Fact]
        public void Report__ResultIsNotEmpty()
        {
            // Arrange
            var result = new CoverageResult
            {
                Identifier = Guid.NewGuid().ToString()
            };
            var lines = new Lines
            {
                {1, new LineInfo {Hits = 1}},
                {2, new LineInfo {Hits = 0}}
            };
            var methods = new Methods
            {
                {"System.Void Coverlet.Core.Reporters.Tests.LcovReporterTests.TestReport()", lines}
            };
            var classes = new Classes
            {
                {"Coverlet.Core.Reporters.Tests.LcovReporterTests", methods}
            };
            var documents = new Documents
            {
                {"doc.cs", classes}
            };
            result.Modules = new Modules
            {
                {"module", documents}
            };
            var reporter = new LcovReporter();
            // Act
            var report = reporter.Report(result);
            // Assert
            Assert.NotEmpty(report);
        }

        [Fact]
        public void Report__ResultContainsValidClassNameWithPrefix()
        {
            // Arrange
            var result = new CoverageResult
            {
                Identifier = Guid.NewGuid().ToString()
            };
            var lines = new Lines
            {
                {1, new LineInfo {Hits = 1}},
                {2, new LineInfo {Hits = 0}}
            };
            var methods = new Methods
            {
                {"System.Void Coverlet.Core.Reporters.Tests.LcovReporterTests.TestReport()", lines}
            };
            var classes = new Classes
            {
                {"Coverlet.Core.Reporters.Tests.LcovReporterTests", methods}
            };
            var classFileName = "doc.cs";
            var documents = new Documents
            {
                {classFileName, classes}
            };
            result.Modules = new Modules
            {
                {"module", documents}
            };
            var reporter = new LcovReporter();
            // Act
            var report = reporter.Report(result);
            // Assert
            var validClassNameWithPrefix = $"SF:{classFileName}";
            Assert.Equal(validClassNameWithPrefix, report.Split(Environment.NewLine)[0]);
        }
    }
}