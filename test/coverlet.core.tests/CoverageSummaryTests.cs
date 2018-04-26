using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using Coverlet.Core;
using Moq;
using Xunit;

namespace Coverlet.Core.Tests
{
    [ExcludeFromCodeCoverage]
    public class CoverageSummaryTests
    {
        private readonly Modules _modules;

        public CoverageSummaryTests()
        {
            var lines = new Lines
            {
                {1, new LineInfo {Hits = 1, IsBranchPoint = true}},
                {2, new LineInfo {Hits = 0}}
            };
            var methods = new Methods
            {
                {"System.Void Coverlet.Core.Tests.CoverageSummaryTests::TestCalculateSummary()", lines}
            };
            var classes = new Classes
            {
                {"Coverlet.Core.Tests.CoverageSummaryTests", methods}
            };
            var documents = new Documents
            {
                {"doc.cs", classes}
            };
            _modules = new Modules {{"module", documents}};
        }

        #region Method: CalculateLineCoverage

        [Fact]
        public void CalculateLineCoverage__ForModule__ReturnValidCoverageResult()
        {
            // Arrange
            var module = _modules.First();
            var summary = new CoverageSummary();
            // Act
            var coverageResult = summary.CalculateLineCoverage(module.Value);
            // Assert
            Assert.Equal(0.5, coverageResult);
        }

        [Fact]
        public void CalculateLineCoverage__ForDocument__ReturnValidCoverageResult()
        {
            // Arrange
            var module = _modules.First();
            var document = module.Value.First();
            var summary = new CoverageSummary();
            // Act
            var coverageResult = summary.CalculateLineCoverage(document.Value);
            // Assert
            Assert.Equal(0.5, coverageResult);
        }

        [Fact]
        public void CalculateLineCoverage__ForClass__ReturnValidCoverageResult()
        {
            // Arrange
            var module = _modules.First();
            var document = module.Value.First();
            var @class = document.Value.First();
            var summary = new CoverageSummary();
            // Act
            var coverageResult = summary.CalculateLineCoverage(@class.Value);
            // Assert
            Assert.Equal(0.5, coverageResult);
        }

        [Fact]
        public void CalculateLineCoverage__ForMethod__ReturnValidCoverageResult()
        {
            // Arrange
            var module = _modules.First();
            var document = module.Value.First();
            var @class = document.Value.First();
            var method = @class.Value.First();
            var summary = new CoverageSummary();
            // Act
            var coverageResult = summary.CalculateLineCoverage(method.Value);
            // Assert
            Assert.Equal(0.5, coverageResult);
        }

        #endregion

        #region Method: CalculateBranchCoverage

        [Fact]
        public void CalculateBranchCoverage__ForModule__ReturnValidCoverageResult()
        {
            // Arrange
            var module = _modules.First();
            var summary = new CoverageSummary();
            // Act
            var coverageResult = summary.CalculateBranchCoverage(module.Value);
            // Assert
            Assert.Equal(1, coverageResult);
        }

        [Fact]
        public void CalculateBranchCoverage__ForDocument__ReturnValidCoverageResult()
        {
            // Arrange
            var module = _modules.First();
            var document = module.Value.First();
            var summary = new CoverageSummary();
            // Act
            var coverageResult = summary.CalculateBranchCoverage(document.Value);
            // Assert
            Assert.Equal(1, coverageResult);
        }

        [Fact]
        public void CalculateBranchCoverage__ForClass__ReturnValidCoverageResult()
        {
            // Arrange
            var module = _modules.First();
            var document = module.Value.First();
            var @class = document.Value.First();
            var summary = new CoverageSummary();
            // Act
            var coverageResult = summary.CalculateBranchCoverage(@class.Value);
            // Assert
            Assert.Equal(1, coverageResult);
        }

        [Fact]
        public void CalculateBranchCoverage__ForMethod__ReturnValidCoverageResult()
        {
            // Arrange
            var module = _modules.First();
            var document = module.Value.First();
            var @class = document.Value.First();
            var method = @class.Value.First();
            var summary = new CoverageSummary();
            // Act
            var coverageResult = summary.CalculateBranchCoverage(method.Value);
            // Assert
            Assert.Equal(1, coverageResult);
        }

        #endregion

        #region Method: CalculateMethodCoverage

        [Fact]
        public void CalculateMethodCoverage__ForModule__ReturnValidCoverageResult()
        {
            // Arrange
            var module = _modules.First();
            var summary = new CoverageSummary();
            // Act
            var coverageResult = summary.CalculateMethodCoverage(module.Value);
            // Assert
            Assert.Equal(1, coverageResult);
        }
        [Fact]
        public void CalculateMethodCoverage__ForDocument__ReturnValidCoverageResult()
        {
            // Arrange
            var module = _modules.First();
            var document = module.Value.First();
            var summary = new CoverageSummary();
            // Act
            var coverageResult = summary.CalculateMethodCoverage(document.Value);
            // Assert
            Assert.Equal(1, coverageResult);
        }
        [Fact]
        public void CalculateMethodCoverage__ForClass__ReturnValidCoverageResult()
        {
            // Arrange
            var module = _modules.First();
            var document = module.Value.First();
            var @class = document.Value.First();
            var summary = new CoverageSummary();
            // Act
            var coverageResult = summary.CalculateMethodCoverage(@class.Value);
            // Assert
            Assert.Equal(1, coverageResult);
        }
        [Fact]
        public void CalculateMethodCoverage__ForMethod__ReturnValidCoverageResult()
        {
            // Arrange
            var module = _modules.First();
            var document = module.Value.First();
            var @class = document.Value.First();
            var method = @class.Value.First();
            var summary = new CoverageSummary();
            // Act
            var coverageResult = summary.CalculateMethodCoverage(method.Value);
            // Assert
            Assert.Equal(1, coverageResult);
        }

        #endregion
    }
}