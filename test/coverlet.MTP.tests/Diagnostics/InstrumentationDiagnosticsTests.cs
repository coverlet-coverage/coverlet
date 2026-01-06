// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Reflection;
using Coverlet.Core.Instrumentation;
using Coverlet.MTP.Diagnostics;
using Microsoft.Testing.Platform.Logging;
using Moq;
using Xunit;

namespace Coverlet.MTP.Tests.Diagnostics;

public class InstrumentationDiagnosticsTests : IDisposable
{
  private readonly Mock<ILogger> _mockLogger = new();
  private readonly InstrumentationDiagnostics _diagnostics;
  private readonly string _tempDirectory;
  private readonly CultureInfo _originalCulture;
  private readonly CultureInfo _originalUICulture;
  private static readonly string[] s_sourceArray = ["Module1.dll", "Module2.dll"];

  public InstrumentationDiagnosticsTests()
  {
    // Save original culture
    _originalCulture = CultureInfo.CurrentCulture;
    _originalUICulture = CultureInfo.CurrentUICulture;

    // Set InvariantCulture for tests
    CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
    CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

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

    // Restore original culture
    CultureInfo.CurrentCulture = _originalCulture;
    CultureInfo.CurrentUICulture = _originalUICulture;

    GC.SuppressFinalize(this);
  }

  #region LogInstrumentationResultsAsync Tests

  [Fact]
  public async Task LogInstrumentationResultsAsync_LogsModuleInfo()
  {
    // Arrange
    InstrumenterResult result = CreateInstrumenterResult("TestModule.dll", documentsCount: 5);

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
    InstrumenterResult result = CreateInstrumenterResult("TestModule.dll", documentsCount: 1, hasSourceLink: true);

    // Act
    await _diagnostics.LogInstrumentationResultsAsync(result);

    // Assert
    VerifyLogAsyncCalled(LogLevel.Information, "Source link: enabled", Times.Once());
  }

  [Fact]
  public async Task LogInstrumentationResultsAsync_WithoutSourceLink_LogsDisabled()
  {
    // Arrange
    InstrumenterResult result = CreateInstrumenterResult("TestModule.dll", documentsCount: 1, hasSourceLink: false);

    // Act
    await _diagnostics.LogInstrumentationResultsAsync(result);

    // Assert
    VerifyLogAsyncCalled(LogLevel.Information, "Source link: disabled", Times.Once());
  }

  [Fact]
  public async Task LogInstrumentationResultsAsync_WithDocuments_LogsDocumentNames()
  {
    // Arrange
    InstrumenterResult result = CreateInstrumenterResult("TestModule.dll", documentsCount: 3);

    // Act
    await _diagnostics.LogInstrumentationResultsAsync(result);

    // Assert
    VerifyLogAsyncCalled(LogLevel.Debug, "Documents:", Times.Once());
  }

  [Fact]
  public async Task LogInstrumentationResultsAsync_WithNoDocuments_DoesNotLogDocuments()
  {
    // Arrange
    InstrumenterResult result = CreateInstrumenterResult("TestModule.dll", documentsCount: 0);

    // Act
    await _diagnostics.LogInstrumentationResultsAsync(result);

    // Assert
    VerifyLogAsyncCalled(LogLevel.Debug, "Documents:", Times.Never());
  }

  [Fact]
  public async Task LogInstrumentationResultsAsync_WithMoreThan10Documents_LogsTruncationMessage()
  {
    // Arrange
    InstrumenterResult result = CreateInstrumenterResult("TestModule.dll", documentsCount: 15);

    // Act
    await _diagnostics.LogInstrumentationResultsAsync(result);

    // Assert
    VerifyLogAsyncCalled(LogLevel.Debug, "... and 5 more", Times.Once());
  }

  [Fact]
  public async Task LogInstrumentationResultsAsync_With10OrFewerDocuments_DoesNotLogTruncationMessage()
  {
    // Arrange
    InstrumenterResult result = CreateInstrumenterResult("TestModule.dll", documentsCount: 10);

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
        It.Is<string>(s => s.Contains(reportPath) && s.Contains('B')),
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

  #region Additional Integration Tests

  [Fact]
  public async Task LogInstrumentationResultsAsync_WithValidResult_LogsModuleInformation()
  {
    // Arrange
    InstrumenterResult result = CreateInstrumenterResultWithDocuments(
      "TestModule",
      new Dictionary<string, Document>
      {
        ["file1.cs"] = new Document(),
        ["file2.cs"] = new Document()
      },
      "https://example.com/sourcelink");

    // Act
    await _diagnostics.LogInstrumentationResultsAsync(result);

    // Assert
    VerifyLogAsyncCalled(LogLevel.Information, "Instrumented module: TestModule", Times.Once());
    VerifyLogAsyncCalled(LogLevel.Information, "Source files: 2", Times.Once());
    VerifyLogAsyncCalled(LogLevel.Information, "Source link: enabled", Times.Once());
  }

  [Fact]
  public async Task LogInstrumentationResultsAsync_WithNoSourceLink_LogsDisabled()
  {
    // Arrange
    InstrumenterResult result = CreateInstrumenterResultWithDocuments(
      "TestModule",
      [],
      null);

    // Act
    await _diagnostics.LogInstrumentationResultsAsync(result);

    // Assert
    VerifyLogAsyncCalled(LogLevel.Information, "Source link: disabled", Times.Once());
  }

  [Fact]
  public async Task LogInstrumentationResultsAsync_WithMoreThan10Documents_LogsTruncatedList()
  {
    // Arrange
    var documents = Enumerable.Range(1, 15)
      .ToDictionary(i => $"file{i}.cs", _ => new Document());

    InstrumenterResult result = CreateInstrumenterResultWithDocuments("TestModule", documents, null);

    // Act
    await _diagnostics.LogInstrumentationResultsAsync(result);

    // Assert
    VerifyLogAsyncCalled(LogLevel.Debug, "Documents:", Times.Once());
    _mockLogger.Verify(
      x => x.LogAsync(
        It.Is<LogLevel>(l => l == LogLevel.Debug),
        It.Is<string>(s => s.StartsWith("    - file")),
        It.IsAny<Exception?>(),
        It.IsAny<Func<string, Exception?, string>>()),
      Times.Exactly(10));
    VerifyLogAsyncCalled(LogLevel.Debug, "... and 5 more", Times.Once());
  }

  [Fact]
  public async Task LogInstrumentationResultsAsync_WithExactly10Documents_DoesNotLogMoreMessage()
  {
    // Arrange
    var documents = Enumerable.Range(1, 10)
      .ToDictionary(i => $"file{i}.cs", _ => new Document());

    InstrumenterResult result = CreateInstrumenterResultWithDocuments("TestModule", documents, null);

    // Act
    await _diagnostics.LogInstrumentationResultsAsync(result);

    // Assert
    VerifyLogAsyncCalled(LogLevel.Debug, "... and", Times.Never());
  }

  [Fact]
  public async Task LogInstrumentationSummaryAsync_WithResults_LogsSummary()
  {
    // Arrange
    var results = new List<InstrumenterResult>
    {
      CreateInstrumenterResultWithDocuments("Module1.dll", new Dictionary<string, Document> { ["file1.cs"] = new Document() }, null),
      CreateInstrumenterResultWithDocuments("Module2.dll", new Dictionary<string, Document> { ["file2.cs"] = new Document(), ["file3.cs"] = new Document() }, null)
    };

    // Act
    await _diagnostics.LogInstrumentationSummaryAsync(results);

    // Assert
    VerifyLogAsyncCalled(LogLevel.Information, "=== Instrumentation Summary ===", Times.Once());
    VerifyLogAsyncCalled(LogLevel.Information, "Total modules instrumented: 2", Times.Once());
    VerifyLogAsyncCalled(LogLevel.Information, "Instrumented modules:", Times.Once());
    VerifyLogAsyncCalled(LogLevel.Information, "Module1.dll (1 source files)", Times.Once());
    VerifyLogAsyncCalled(LogLevel.Information, "Module2.dll (2 source files)", Times.Once());
    VerifyLogAsyncCalled(LogLevel.Information, "===============================", Times.Once());
  }

  [Fact]
  public async Task LogExcludedModulesAsync_WithExcludedModules_LogsExclusions()
  {
    // Arrange
    var allModules = new[] { "Module1.dll", "Module2.dll", "Module3.dll" };
    var instrumentedModules = new[] { "Module1.dll" };
    var excludeFilters = new[] { "[Exclude]*", "[Test]*" };

    // Act
    await _diagnostics.LogExcludedModulesAsync(allModules, instrumentedModules, excludeFilters);

    // Assert
    VerifyLogAsyncCalled(LogLevel.Debug, "=== Excluded Modules ===", Times.Once());
    VerifyLogAsyncCalled(LogLevel.Debug, "Exclude filters: [Exclude]*, [Test]*", Times.Once());
    VerifyLogAsyncCalled(LogLevel.Debug, "Excluded modules:", Times.Once());
    VerifyLogAsyncCalled(LogLevel.Debug, "Module2.dll", Times.Once());
    VerifyLogAsyncCalled(LogLevel.Debug, "Module3.dll", Times.Once());
    VerifyLogAsyncCalled(LogLevel.Debug, "========================", Times.Once());
  }

  [Fact]
  public async Task LogExcludedModulesAsync_WithNoExclusions_DoesNotLog()
  {
    // Arrange
    var allModules = new[] { "Module1.dll", "Module2.dll" };
    var instrumentedModules = new[] { "Module1.dll", "Module2.dll" };
    var excludeFilters = new[] { "[Exclude]*" };

    // Act
    await _diagnostics.LogExcludedModulesAsync(allModules, instrumentedModules, excludeFilters);

    // Assert
    VerifyLogAsyncCalled(LogLevel.Debug, "=== Excluded Modules ===", Times.Never());
  }

  [Fact]
  public async Task LogCoverageResultGenerationAsync_WithFormats_LogsDetails()
  {
    // Arrange
    string outputPath = "/output/path";
    var formats = new[] { "json", "cobertura", "lcov" };

    // Act
    await _diagnostics.LogCoverageResultGenerationAsync(outputPath, formats);

    // Assert
    VerifyLogAsyncCalled(LogLevel.Information, "=== Coverage Result Generation ===", Times.Once());
    VerifyLogAsyncCalled(LogLevel.Information, "Output directory: /output/path", Times.Once());
    VerifyLogAsyncCalled(LogLevel.Information, "Formats: json, cobertura, lcov", Times.Once());
    VerifyLogAsyncCalled(LogLevel.Information, "==================================", Times.Once());
  }

  [Fact]
  public async Task LogGeneratedReportsAsync_WithNonExistingFiles_LogsWarning()
  {
    // Arrange
    string nonExistingFile = Path.Combine(Path.GetTempPath(), "nonexisting.json");
    var reportPaths = new[] { nonExistingFile };

    // Act
    await _diagnostics.LogGeneratedReportsAsync(reportPaths);

    // Assert
    VerifyLogAsyncCalled(LogLevel.Information, "Generated coverage reports:", Times.Once());
    VerifyLogAsyncCalled(LogLevel.Warning, $"  - {nonExistingFile} (file not found)", Times.Once());
  }

  [Fact]
  public async Task LogErrorAsync_WithException_LogsErrorDetails()
  {
    // Arrange
    string context = "instrumentation";
    var exception = new InvalidOperationException("Test error message");

    // Act
    await _diagnostics.LogErrorAsync(context, exception);

    // Assert
    VerifyLogAsyncCalled(LogLevel.Error, "Error during instrumentation: Test error message", Times.Once());
    _mockLogger.Verify(
      x => x.LogAsync(
        It.Is<LogLevel>(l => l == LogLevel.Debug),
        It.Is<string>(s => s.StartsWith("Stack trace:")),
        It.IsAny<Exception?>(),
        It.IsAny<Func<string, Exception?, string>>()),
      Times.Once());
  }

  [Fact]
  public async Task LogErrorAsync_WithInnerException_LogsInnerException()
  {
    // Arrange
    string context = "coverage collection";
    var innerException = new ArgumentException("Inner error");
    var exception = new InvalidOperationException("Outer error", innerException);

    // Act
    await _diagnostics.LogErrorAsync(context, exception);

    // Assert
    VerifyLogAsyncCalled(LogLevel.Error, "Error during coverage collection: Outer error", Times.Once());
    VerifyLogAsyncCalled(LogLevel.Debug, "Inner exception: Inner error", Times.Once());
  }

  [Theory]
  [InlineData(0, "0.00 B")]
  [InlineData(512, "512.00 B")]
  [InlineData(1024, "1.00 KB")]
  [InlineData(1536, "1.50 KB")]
  [InlineData(1048576, "1.00 MB")]
  [InlineData(1572864, "1.50 MB")]
  [InlineData(1073741824, "1.00 GB")]
  public async Task LogGeneratedReportsAsync_FormatsFileSizeCorrectly(long fileSize, string expectedFormat)
  {
    // Arrange
    string tempFile = Path.GetTempFileName();
    try
    {
      // Create a file with specific size
      using (var fs = new FileStream(tempFile, FileMode.Create))
      {
        fs.SetLength(fileSize);
      }

      var reportPaths = new[] { tempFile };

      // Act
      await _diagnostics.LogGeneratedReportsAsync(reportPaths);

      // Assert
      _mockLogger.Verify(
        x => x.LogAsync(
          It.Is<LogLevel>(l => l == LogLevel.Information),
          It.Is<string>(s => s.Contains(expectedFormat)),
          It.IsAny<Exception?>(),
          It.IsAny<Func<string, Exception?, string>>()),
        Times.Once);
    }
    finally
    {
      File.Delete(tempFile);
    }
  }

  [Fact]
  public async Task LogInstrumentationResultsAsync_WithPathSeparators_LogsFileNameOnly()
  {
    // Arrange
    var documents = new Dictionary<string, Document>
    {
      [@"C:\path\to\file1.cs"] = new Document(),
      ["/path/to/file2.cs"] = new Document()
    };

    InstrumenterResult result = CreateInstrumenterResultWithDocuments("TestModule", documents, null);

    // Act
    await _diagnostics.LogInstrumentationResultsAsync(result);

    // Assert
    VerifyLogAsyncCalled(LogLevel.Debug, "file1.cs", Times.Once());
    VerifyLogAsyncCalled(LogLevel.Debug, "file2.cs", Times.Once());
  }

  [Fact]
  public async Task LogInstrumentationSummaryAsync_ExtractsModuleNameFromPath()
  {
    // Arrange
    var results = new List<InstrumenterResult>
    {
      CreateInstrumenterResultWithDocuments(@"C:\path\to\Module1.dll", [], null)
    };

    // Act
    await _diagnostics.LogInstrumentationSummaryAsync(results);

    // Assert
    _mockLogger.Verify(
      x => x.LogAsync(
        It.Is<LogLevel>(l => l == LogLevel.Information),
        It.Is<string>(s => s.Contains("Module1.dll") && s.Contains("0 source files")),
        It.IsAny<Exception?>(),
        It.IsAny<Func<string, Exception?, string>>()),
      Times.Once);
  }

  [Fact]
  public async Task LogExcludedModulesAsync_HandlesNullModuleNames()
  {
    // Arrange
    IEnumerable<string> allModules = s_sourceArray.Where(m => m is not null);
    var instrumentedModules = new[] { "Module1.dll" };
    var excludeFilters = Array.Empty<string>();

    // Act & Assert
    await _diagnostics.LogExcludedModulesAsync(allModules, instrumentedModules, excludeFilters);

    // Should not throw and should handle gracefully
    VerifyLogAsyncCalled(LogLevel.Debug, "=== Excluded Modules ===", Times.Once());
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
    PropertyInfo? documentsProperty = typeof(InstrumenterResult)
      .GetProperty(nameof(InstrumenterResult.Documents));
    documentsProperty?.SetValue(result, documents);

    return result;
  }

  private static InstrumenterResult CreateInstrumenterResultWithDocuments(
    string moduleName,
    Dictionary<string, Document> documents,
    string? sourceLink)
  {
    var result = new InstrumenterResult
    {
      Module = moduleName,
      SourceLink = sourceLink
    };

    // Use reflection to set the private setter for Documents
    PropertyInfo? documentsProperty = typeof(InstrumenterResult)
      .GetProperty(nameof(InstrumenterResult.Documents));
    documentsProperty?.SetValue(result, documents);

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
