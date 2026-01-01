// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Coverlet.MTP.Diagnostics;
using Microsoft.Testing.Platform.Logging;
using Moq;
using Xunit;
using Coverlet.Core.Instrumentation;

namespace Coverlet.MTP.Tests.Diagnostics;

public class InstrumentationDiagnosticsTests : IDisposable
{
  private readonly Mock<ILogger> _mockLogger = new();
  private readonly InstrumentationDiagnostics _diagnostics;
  private readonly string _tempDirectory;

  public InstrumentationDiagnosticsTests()
  {
    _diagnostics = new InstrumentationDiagnostics(_mockLogger.Object);
    _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    Directory.CreateDirectory(_tempDirectory);
  }

  public void Dispose()
  {
    if (Directory.Exists(_tempDirectory))
    {
      Directory.Delete(_tempDirectory, true);
    }
    GC.SuppressFinalize(this);
  }

  #region LogInstrumentationResultsAsync Tests

  [Fact]
  public async Task LogInstrumentationResultsAsync_LogsModuleInfo()
  {
    // Arrange
    var result = CreateInstrumenterResult("TestModule.dll", documentsCount: 5);

    // Act
    await _diagnostics.LogInstrumentationResultsAsync(result);

    // Assert
    VerifyLogAsyncCalled(LogLevel.Information, "Instrumented module: TestModule.dll", Times.Once());
    VerifyLogAsyncCalled(LogLevel.Information, "Source files: 5", Times.Once());
  }

  [Fact]
  public async Task LogInstrumentationResultsAsync_WithSourceLink_LogsEnabled()
  {
    // Arrange
    var result = CreateInstrumenterResult("TestModule.dll", documentsCount: 1, hasSourceLink: true);

    // Act
    await _diagnostics.LogInstrumentationResultsAsync(result);

    // Assert
    VerifyLogAsyncCalled(LogLevel.Information, "Source link: enabled", Times.Once());
  }

  [Fact]
  public async Task LogInstrumentationResultsAsync_WithoutSourceLink_LogsDisabled()
  {
    // Arrange
    var result = CreateInstrumenterResult("TestModule.dll", documentsCount: 1, hasSourceLink: false);

    // Act
    await _diagnostics.LogInstrumentationResultsAsync(result);

    // Assert
    VerifyLogAsyncCalled(LogLevel.Information, "Source link: disabled", Times.Once());
  }

  [Fact]
  public async Task LogInstrumentationResultsAsync_WithDocuments_LogsDocumentNames()
  {
    // Arrange
    var result = CreateInstrumenterResult("TestModule.dll", documentsCount: 3);

    // Act
    await _diagnostics.LogInstrumentationResultsAsync(result);

    // Assert
    VerifyLogAsyncCalled(LogLevel.Debug, "Documents:", Times.Once());
  }

  [Fact]
  public async Task LogInstrumentationResultsAsync_WithNoDocuments_DoesNotLogDocuments()
  {
    // Arrange
    var result = CreateInstrumenterResult("TestModule.dll", documentsCount: 0);

    // Act
    await _diagnostics.LogInstrumentationResultsAsync(result);

    // Assert
    VerifyLogAsyncCalled(LogLevel.Debug, "Documents:", Times.Never());
  }

  [Fact]
  public async Task LogInstrumentationResultsAsync_WithMoreThan10Documents_LogsTruncationMessage()
  {
    // Arrange
    var result = CreateInstrumenterResult("TestModule.dll", documentsCount: 15);

    // Act
    await _diagnostics.LogInstrumentationResultsAsync(result);

    // Assert
    VerifyLogAsyncCalled(LogLevel.Debug, "... and 5 more", Times.Once());
  }

  [Fact]
  public async Task LogInstrumentationResultsAsync_With10OrFewerDocuments_DoesNotLogTruncationMessage()
  {
    // Arrange
    var result = CreateInstrumenterResult("TestModule.dll", documentsCount: 10);

    // Act
    await _diagnostics.LogInstrumentationResultsAsync(result);

    // Assert
    VerifyLogAsyncCalled(LogLevel.Debug, "... and", Times.Never());
  }

  #endregion

  #region LogInstrumentationSummaryAsync Tests

  [Fact]
  public async Task LogInstrumentationSummaryAsync_LogsSummaryHeader()
  {
    // Arrange
    var results = new List<InstrumenterResult>
    {
      CreateInstrumenterResult("Module1.dll", documentsCount: 5),
      CreateInstrumenterResult("Module2.dll", documentsCount: 3)
    };

    // Act
    await _diagnostics.LogInstrumentationSummaryAsync(results);

    // Assert
    VerifyLogAsyncCalled(LogLevel.Information, "=== Instrumentation Summary ===", Times.Once());
    VerifyLogAsyncCalled(LogLevel.Information, "Total modules instrumented: 2", Times.Once());
  }

  [Fact]
  public async Task LogInstrumentationSummaryAsync_WithNoResults_LogsWarning()
  {
    // Arrange
    var results = Enumerable.Empty<InstrumenterResult>();

    // Act
    await _diagnostics.LogInstrumentationSummaryAsync(results);

    // Assert
    VerifyLogAsyncCalled(LogLevel.Warning, "No modules were instrumented", Times.Once());
  }

  [Fact]
  public async Task LogInstrumentationSummaryAsync_WithResults_LogsModuleList()
  {
    // Arrange
    var results = new List<InstrumenterResult>
    {
      CreateInstrumenterResult("Module1.dll", documentsCount: 5)
    };

    // Act
    await _diagnostics.LogInstrumentationSummaryAsync(results);

    // Assert
    VerifyLogAsyncCalled(LogLevel.Information, "Instrumented modules:", Times.Once());
    VerifyLogAsyncCalled(LogLevel.Information, "Module1.dll (5 source files)", Times.Once());
  }

  [Fact]
  public async Task LogInstrumentationSummaryAsync_LogsFooter()
  {
    // Arrange
    var results = new List<InstrumenterResult>
    {
      CreateInstrumenterResult("Module1.dll", documentsCount: 1)
    };

    // Act
    await _diagnostics.LogInstrumentationSummaryAsync(results);

    // Assert
    VerifyLogAsyncCalled(LogLevel.Information, "===============================", Times.Once());
  }

  #endregion

  #region LogExcludedModulesAsync Tests

  [Fact]
  public async Task LogExcludedModulesAsync_WithExcludedModules_LogsExclusionInfo()
  {
    // Arrange
    var allModules = new[] { "Module1.dll", "Module2.dll", "Module3.dll" };
    var instrumentedModules = new[] { "Module1.dll" };
    var excludeFilters = new[] { "[Module2]*", "[Module3]*" };

    // Act
    await _diagnostics.LogExcludedModulesAsync(allModules, instrumentedModules, excludeFilters);

    // Assert
    VerifyLogAsyncCalled(LogLevel.Debug, "=== Excluded Modules ===", Times.Once());
    VerifyLogAsyncCalled(LogLevel.Debug, "Exclude filters:", Times.Once());
  }

  [Fact]
  public async Task LogExcludedModulesAsync_WithNoExcludedModules_DoesNotLog()
  {
    // Arrange
    var allModules = new[] { "Module1.dll" };
    var instrumentedModules = new[] { "Module1.dll" };
    var excludeFilters = Array.Empty<string>();

    // Act
    await _diagnostics.LogExcludedModulesAsync(allModules, instrumentedModules, excludeFilters);

    // Assert
    VerifyLogAsyncCalled(LogLevel.Debug, "=== Excluded Modules ===", Times.Never());
  }

  [Fact]
  public async Task LogExcludedModulesAsync_LogsExcludeFilters()
  {
    // Arrange
    var allModules = new[] { "Module1.dll", "Module2.dll" };
    var instrumentedModules = new[] { "Module1.dll" };
    var excludeFilters = new[] { "[Module2]*", "[Test]*" };

    // Act
    await _diagnostics.LogExcludedModulesAsync(allModules, instrumentedModules, excludeFilters);

    // Assert
    VerifyLogAsyncCalled(LogLevel.Debug, "[Module2]*, [Test]*", Times.Once());
  }

  #endregion

  #region LogCoverageResultGenerationAsync Tests

  [Fact]
  public async Task LogCoverageResultGenerationAsync_LogsOutputPath()
  {
    // Arrange
    string outputPath = "/path/to/coverage";
    string[] formats = ["cobertura", "json"];

    // Act
    await _diagnostics.LogCoverageResultGenerationAsync(outputPath, formats);

    // Assert
    VerifyLogAsyncCalled(LogLevel.Information, "=== Coverage Result Generation ===", Times.Once());
    VerifyLogAsyncCalled(LogLevel.Information, "Output directory: /path/to/coverage", Times.Once());
  }

  [Fact]
  public async Task LogCoverageResultGenerationAsync_LogsFormats()
  {
    // Arrange
    string outputPath = "/path/to/coverage";
    string[] formats = ["cobertura", "json", "lcov"];

    // Act
    await _diagnostics.LogCoverageResultGenerationAsync(outputPath, formats);

    // Assert
    VerifyLogAsyncCalled(LogLevel.Information, "Formats: cobertura, json, lcov", Times.Once());
  }

  [Fact]
  public async Task LogCoverageResultGenerationAsync_LogsFooter()
  {
    // Arrange
    string outputPath = "/path/to/coverage";
    string[] formats = ["cobertura"];

    // Act
    await _diagnostics.LogCoverageResultGenerationAsync(outputPath, formats);

    // Assert
    VerifyLogAsyncCalled(LogLevel.Information, "==================================", Times.Once());
  }

  #endregion

  #region LogGeneratedReportsAsync Tests

  [Fact]
  public async Task LogGeneratedReportsAsync_WithExistingFiles_LogsFileInfo()
  {
    // Arrange
    string reportPath = Path.Combine(_tempDirectory, "report.xml");
    File.WriteAllText(reportPath, "<coverage />");
    var reportPaths = new[] { reportPath };

    // Act
    await _diagnostics.LogGeneratedReportsAsync(reportPaths);

    // Assert
    VerifyLogAsyncCalled(LogLevel.Information, "Generated coverage reports:", Times.Once());
    _mockLogger.Verify(
      x => x.LogAsync(
        It.Is<LogLevel>(l => l == LogLevel.Information),
        It.Is<string>(s => s.Contains(reportPath) && s.Contains("B")),
        It.IsAny<Exception?>(),
        It.IsAny<Func<string, Exception?, string>>()),
      Times.Once());
  }

  [Fact]
  public async Task LogGeneratedReportsAsync_WithMissingFile_LogsWarning()
  {
    // Arrange
    string missingPath = Path.Combine(_tempDirectory, "missing.xml");
    var reportPaths = new[] { missingPath };

    // Act
    await _diagnostics.LogGeneratedReportsAsync(reportPaths);

    // Assert
    VerifyLogAsyncCalled(LogLevel.Warning, "file not found", Times.Once());
  }

  [Fact]
  public async Task LogGeneratedReportsAsync_FormatsFileSizeCorrectly()
  {
    // Arrange
    string reportPath = Path.Combine(_tempDirectory, "large_report.xml");
    // Create a file larger than 1KB
    File.WriteAllText(reportPath, new string('x', 2048));
    var reportPaths = new[] { reportPath };

    // Act
    await _diagnostics.LogGeneratedReportsAsync(reportPaths);

    // Assert
    _mockLogger.Verify(
      x => x.LogAsync(
        It.Is<LogLevel>(l => l == LogLevel.Information),
        It.Is<string>(s => s.Contains("KB")),
        It.IsAny<Exception?>(),
        It.IsAny<Func<string, Exception?, string>>()),
      Times.Once());
  }

  #endregion

  #region LogErrorAsync Tests

  [Fact]
  public async Task LogErrorAsync_LogsErrorMessage()
  {
    // Arrange
    var exception = new InvalidOperationException("Test error");

    // Act
    await _diagnostics.LogErrorAsync("instrumentation", exception);

    // Assert
    VerifyLogAsyncCalled(LogLevel.Error, "Error during instrumentation: Test error", Times.Once());
  }

  [Fact]
  public async Task LogErrorAsync_LogsStackTrace()
  {
    // Arrange
    Exception exception;
    try
    {
      throw new InvalidOperationException("Test error");
    }
    catch (Exception ex)
    {
      exception = ex;
    }

    // Act
    await _diagnostics.LogErrorAsync("coverage collection", exception);

    // Assert
    VerifyLogAsyncCalled(LogLevel.Debug, "Stack trace:", Times.Once());
  }

  [Fact]
  public async Task LogErrorAsync_WithInnerException_LogsInnerExceptionMessage()
  {
    // Arrange
    var innerException = new ArgumentException("Inner error");
    var exception = new InvalidOperationException("Outer error", innerException);

    // Act
    await _diagnostics.LogErrorAsync("processing", exception);

    // Assert
    VerifyLogAsyncCalled(LogLevel.Debug, "Inner exception: Inner error", Times.Once());
  }

  [Fact]
  public async Task LogErrorAsync_WithoutInnerException_DoesNotLogInnerException()
  {
    // Arrange
    var exception = new InvalidOperationException("Test error");

    // Act
    await _diagnostics.LogErrorAsync("processing", exception);

    // Assert
    VerifyLogAsyncCalled(LogLevel.Debug, "Inner exception:", Times.Never());
  }

  #endregion

  #region Helper Methods

  private static InstrumenterResult CreateInstrumenterResult(
    string moduleName,
    int documentsCount,
    bool hasSourceLink = false)
  {
    var documents = new Dictionary<string, Document>();
    for (int i = 0; i < documentsCount; i++)
    {
      documents[$"/path/to/Document{i}.cs"] = new Document { Path = $"/path/to/Document{i}.cs" };
    }

    var result = new InstrumenterResult
    {
      Module = moduleName,
      SourceLink = hasSourceLink ? "https://example.com/sourcelink" : null
    };

    // Use reflection to set the private setter for Documents
    var documentsProperty = typeof(InstrumenterResult)
      .GetProperty(nameof(InstrumenterResult.Documents));
    if (documentsProperty is not null)
    {
      documentsProperty.SetValue(result, documents);
    }

    return result;
  }

  private void VerifyLogAsyncCalled(LogLevel level, string messageContains, Times times)
  {
    _mockLogger.Verify(
      x => x.LogAsync(
        It.Is<LogLevel>(l => l == level),
        It.Is<string>(s => s.Contains(messageContains)),
        It.IsAny<Exception?>(),
        It.IsAny<Func<string, Exception?, string>>()),
      times);
  }

  #endregion
}
