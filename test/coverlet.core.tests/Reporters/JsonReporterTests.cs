using System;
using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace Coverlet.Core.Reporters.Tests
{
    [ExcludeFromCodeCoverage]
    public class JsonReporterTests
    {
        [Fact]
        public void Ctor__FormatIsValid()
        {
            // Arrange
            // Act
            var reporter = new JsonReporter();
            // Assert
            Assert.Equal("json", reporter.Format);
        }
        [Fact]
        public void Ctor__ExtensionIsValid()
        {
            // Arrange
            // Act
            var reporter = new JsonReporter();
            // Assert
            Assert.Equal("json", reporter.Extension);
        }

        [Fact]
        public void Report__ResultAsJsonDoesNotContainsNewLineOnly()
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
                {"System.Void Coverlet.Core.Reporters.Tests.JsonReporterTests.TestReport()", lines}
            };
            var classes = new Classes
            {
                {"Coverlet.Core.Reporters.Tests.JsonReporterTests", methods}
            };
            var documents = new Documents
            {
                {"doc.cs", classes}
            };
            result.Modules = new Modules
            {
                {"module", documents}
            };
            var reporter = new JsonReporter();
            // Act
            var report = reporter.Report(result);
            // Assert
            Assert.NotEqual("{\n}", report);
        }

        [Fact]
        public void Report__ResultAsJsonIsNotEmpty()
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
                {"System.Void Coverlet.Core.Reporters.Tests.JsonReporterTests.TestReport()", lines}
            };
            var classes = new Classes
            {
                {"Coverlet.Core.Reporters.Tests.JsonReporterTests", methods}
            };
            var documents = new Documents
            {
                {"doc.cs", classes}
            };
            result.Modules = new Modules
            {
                {"module", documents}
            };
            var reporter = new JsonReporter();
            // Act
            var report = reporter.Report(result);
            // Assert
            Assert.NotEqual(string.Empty, report);
        }
    }
}