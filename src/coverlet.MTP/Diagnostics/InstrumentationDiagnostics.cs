// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Coverlet.Core.Instrumentation;
using Microsoft.Testing.Platform.Logging;

namespace Coverlet.MTP.Diagnostics;

/// <summary>
/// Provides diagnostic logging for instrumentation process.
/// Similar to CoverageManager diagnostics in coverlet.collector.
/// </summary>
internal sealed class InstrumentationDiagnostics
{
  private readonly ILogger _logger;

  public InstrumentationDiagnostics(ILogger logger)
  {
    _logger = logger;
  }

  /// <summary>
  /// Logs detailed information about instrumentation results.
  /// </summary>
  public async Task LogInstrumentationResultsAsync(InstrumenterResult result)
  {
    await _logger.LogInformationAsync($"Instrumented module: {result.Module}");
    await _logger.LogInformationAsync($"  Source files: {result.Documents.Count}");
    await _logger.LogInformationAsync($"  Source link: {(result.SourceLink != null ? "enabled" : "disabled")}");

    if (result.Documents.Count > 0)
    {
      await _logger.LogDebugAsync("  Documents:");
      foreach (string doc in result.Documents.Keys.Take(10))
      {
        await _logger.LogDebugAsync($"    - {Path.GetFileName(doc)}");
      }

      if (result.Documents.Count > 10)
      {
        await _logger.LogDebugAsync($"    ... and {result.Documents.Count - 10} more");
      }
    }
  }

  /// <summary>
  /// Logs summary of all instrumented modules.
  /// </summary>
  public async Task LogInstrumentationSummaryAsync(IEnumerable<InstrumenterResult> results)
  {
    var resultList = results.ToList();

    await _logger.LogInformationAsync("=== Instrumentation Summary ===");
    await _logger.LogInformationAsync($"Total modules instrumented: {resultList.Count}");

    if (resultList.Count == 0)
    {
      await _logger.LogWarningAsync("No modules were instrumented. Check your include/exclude filters.");
      return;
    }

    await _logger.LogInformationAsync("Instrumented modules:");
    foreach (InstrumenterResult result in resultList)
    {
      string moduleName = Path.GetFileName(result.Module);
      await _logger.LogInformationAsync($"  - {moduleName} ({result.Documents.Count} source files)");
    }

    await _logger.LogInformationAsync("===============================");
  }

  /// <summary>
  /// Logs modules that were excluded from instrumentation.
  /// </summary>
  public async Task LogExcludedModulesAsync(
    IEnumerable<string> allModules,
    IEnumerable<string> instrumentedModules,
    string[] excludeFilters)
  {
    var excludedModules = allModules
      .Select(Path.GetFileName)
      .Except(instrumentedModules.Select(Path.GetFileName))
      .ToList();

    if (excludedModules.Count == 0)
    {
      return;
    }

    await _logger.LogDebugAsync("=== Excluded Modules ===");
    await _logger.LogDebugAsync($"Exclude filters: {string.Join(", ", excludeFilters)}");
    await _logger.LogDebugAsync("Excluded modules:");
    foreach (string? module in excludedModules)
    {
      await _logger.LogDebugAsync($"  - {module}");
    }

    await _logger.LogDebugAsync("========================");
  }

  /// <summary>
  /// Logs coverage result generation details.
  /// </summary>
  public async Task LogCoverageResultGenerationAsync(string outputPath, string[] formats)
  {
    await _logger.LogInformationAsync("=== Coverage Result Generation ===");
    await _logger.LogInformationAsync($"Output directory: {outputPath}");
    await _logger.LogInformationAsync($"Formats: {string.Join(", ", formats)}");
    await _logger.LogInformationAsync("==================================");
  }

  /// <summary>
  /// Logs generated coverage report files.
  /// </summary>
  public async Task LogGeneratedReportsAsync(IEnumerable<string> reportPaths)
  {
    await _logger.LogInformationAsync("Generated coverage reports:");
    foreach (string path in reportPaths)
    {
      if (File.Exists(path))
      {
        var fileInfo = new FileInfo(path);
        await _logger.LogInformationAsync($"  - {path} ({FormatFileSize(fileInfo.Length)})");
      }
      else
      {
        await _logger.LogWarningAsync($"  - {path} (file not found)");
      }
    }
  }

  /// <summary>
  /// Logs error during instrumentation or coverage collection.
  /// </summary>
  public async Task LogErrorAsync(string context, Exception ex)
  {
    await _logger.LogErrorAsync($"Error during {context}: {ex.Message}");
    await _logger.LogDebugAsync($"Stack trace: {ex.StackTrace}");

    if (ex.InnerException != null)
    {
      await _logger.LogDebugAsync($"Inner exception: {ex.InnerException.Message}");
    }
  }

  private static string FormatFileSize(long bytes)
  {
    string[] suffixes = ["B", "KB", "MB", "GB"];
    int suffixIndex = 0;
    double size = bytes;

    while (size >= 1024 && suffixIndex < suffixes.Length - 1)
    {
      size /= 1024;
      suffixIndex++;
    }

    return $"{size:F2} {suffixes[suffixIndex]}";
  }
}
