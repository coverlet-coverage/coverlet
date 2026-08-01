// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Coverlet.Core.Abstractions;
using static System.Console;

namespace Coverlet.Console.Logging
{
  class ConsoleLogger : ILogger
  {
    private static readonly object s_sync = new();
    private StreamWriter _diagWriter;

    public LogLevel Level { get; set; } = LogLevel.Normal;

    /// <summary>
    /// Enables writing diagnostics to the specified file. All log messages (including trace-level
    /// messages that may be suppressed from the console by <see cref="Level"/>) are written to the file.
    /// Intended for CI troubleshooting (e.g. --diag).
    /// </summary>
    public void EnableDiagnosticFile(string path)
    {
      ArgumentNullException.ThrowIfNull(path);

      StreamWriter newWriter = null;
      try
      {
        string directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
          Directory.CreateDirectory(directory);
        }

        newWriter = new StreamWriter(path, append: false) { AutoFlush = true };

        lock (s_sync)
        {
          _diagWriter?.Dispose();
          _diagWriter = newWriter;
        }
      }
      catch (Exception ex)
      {
        newWriter?.Dispose();
        lock (s_sync)
        {
          _diagWriter = null;
        }

        LogWarning($"Unable to create diagnostic file '{path}': {ex.Message}");
      }
    }

    public void LogError(string message) => Log(LogLevel.Quiet, message, ConsoleColor.Red);

    public void LogError(Exception exception) => LogError(exception.ToString());

    public void LogInformation(string message, bool important = false) => Log(important ? LogLevel.Minimal : LogLevel.Normal, message, ForegroundColor);

    public void LogVerbose(string message) => Log(LogLevel.Detailed, message, ForegroundColor);

    /// <summary>
    /// Logs a trace-level diagnostic message. Only written to the console when verbosity is set to
    /// <see cref="LogLevel.Trace"/>, but always written to the diagnostic file when one is configured.
    /// </summary>
    public void LogTrace(string message) => Log(LogLevel.Trace, message, ForegroundColor);

    public void LogWarning(string message) => Log(LogLevel.Quiet, message, ConsoleColor.Yellow);

    private void Log(LogLevel level, string message, ConsoleColor color)
    {
      WriteToDiagnosticFile(level, message);

      if (level < Level) return;

      lock (s_sync)
      {
        ConsoleColor currentForegroundColor;
        if (color != (currentForegroundColor = ForegroundColor))
        {
          ForegroundColor = color;
          WriteLine(message);
          ForegroundColor = currentForegroundColor;
        }
        else
        {
          WriteLine(message);
        }
      }
    }

    private void WriteToDiagnosticFile(LogLevel level, string message)
    {
      if (_diagWriter is null)
      {
        return;
      }

      lock (s_sync)
      {
        try
        {
          _diagWriter.WriteLine($"[{DateTime.UtcNow:O}] [{level}] {message}");
        }
        catch
        {
          _diagWriter = null;
        }
      }
    }
  }
}
