using System;
using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace Coverlet.Core.Reporters.Tests
{
    [ExcludeFromCodeCoverage]
    public class OpenCoverReporterTests
    {
        [Fact]
        public void Ctor__FormatIsValid()
        {
            // Arrange
            // Act
            var reporter = new OpenCoverReporter();
            // Assert
            Assert.Equal("opencover", reporter.Format);
        }
        [Fact]
        public void Ctor__ExtensionIsValid()
        {
            // Arrange
            // Act
            var reporter = new OpenCoverReporter();
            // Assert
            Assert.Equal("xml", reporter.Extension);
        }

        [Fact]
        public void Report__ResultIsNotEmptyString()
        {
            // Arrange
            var result = new CoverageResult
            {
                Identifier = Guid.NewGuid().ToString(),
                Modules = new Modules
                {
                    {"Coverlet.Core.Reporters.Tests", CreateFirstDocuments()}
                }
            };
            var reporter = new OpenCoverReporter();
            // Act
            var report = reporter.Report(result);
            // Assert
            Assert.NotEmpty(report);
        }

        [Fact]
        public void Report__ReportHaveFilesWithUniqueIdsOverMultipleModules()
        {
            // Arrange
            var result = new CoverageResult
            {
                Identifier = Guid.NewGuid().ToString(),
                Modules = new Modules
                {
                    {"Coverlet.Core.Reporters.Tests", CreateFirstDocuments()},
                    {"Some.Other.Module", CreateSecondDocuments()}
                }
            };
            var reporter = new OpenCoverReporter();
            // Act
            var report = reporter.Report(result);
            // Assert
            Assert.Contains(@"<FileRef uid=""1"" />", report);
            Assert.Contains(@"<FileRef uid=""2"" />", report);
        }

        private static Documents CreateFirstDocuments()
        {
            var lines = new Lines {{1, new LineInfo {Hits = 1}}, {2, new LineInfo {Hits = 0}}};
            var methods = new Methods
            {
                { "System.Void Coverlet.Core.Reporters.Tests.OpenCoverReporterTests.TestReport()", lines }
            };
            var classes = new Classes
            {
                { "Coverlet.Core.Reporters.Tests.OpenCoverReporterTests", methods }
            };
            var documents = new Documents
            {
                { "doc.cs", classes }
            };
            return documents;
        }

        private static Documents CreateSecondDocuments()
        {
            var lines = new Lines
            {
                { 1, new LineInfo { Hits = 1 } },
                { 2, new LineInfo { Hits = 0 } }
            };
            var methods = new Methods
            {
                { "System.Void Some.Other.Module.TestClass.TestMethod()", lines }
            };
            var classes2 = new Classes
            {
                { "Some.Other.Module.TestClass", methods }
            };
            var documents = new Documents
            {
                { "TestClass.cs", classes2 }
            };
            return documents;
        }
    }
}