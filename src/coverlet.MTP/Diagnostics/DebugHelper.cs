// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Coverlet.MTP.Diagnostics;

/// <summary>
/// Helper methods for debugging coverlet.MTP extension.
/// </summary>
internal static class DebugHelper
{
  /// <summary>
  /// Checks debug environment variables and handles debugger attachment.
  /// Call this at extension initialization points.
  /// </summary>
  /// <param name="componentName">Name of the component for logging purposes.</param>
  public static void HandleDebuggerAttachment(string componentName)
  {
    // Check for immediate debugger launch
    if (IsEnvironmentVariableEnabled(CoverletMtpDebugConstants.DebugLaunch))
    {
      Console.WriteLine($"[Coverlet.MTP] {componentName}: Launching debugger...");
      Debugger.Launch();
    }

    // Check for wait-for-attach loop
    if (IsEnvironmentVariableEnabled(CoverletMtpDebugConstants.DebugWaitForAttach))
    {
      int processId = Process.GetCurrentProcess().Id;
      Console.WriteLine($"[Coverlet.MTP] {componentName}: Waiting for debugger to attach...");
      Console.WriteLine($"[Coverlet.MTP] Process Id: {processId}, Name: {Process.GetCurrentProcess().ProcessName}");

      while (!Debugger.IsAttached)
      {
        Thread.Sleep(1000);
      }

      Console.WriteLine($"[Coverlet.MTP] {componentName}: Debugger attached, continuing execution.");
    }
  }

  /// <summary>
  /// Checks if a debug/trace feature is enabled via environment variable.
  /// </summary>
  public static bool IsEnvironmentVariableEnabled(string variableName)
  {
    string? value = Environment.GetEnvironmentVariable(variableName);
    return value == "1" || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
  }

  /// <summary>
  /// Gets whether tracker logging is enabled.
  /// </summary>
  public static bool IsTrackerLogEnabled =>
    IsEnvironmentVariableEnabled(CoverletMtpDebugConstants.EnableTrackerLog);

  /// <summary>
  /// Gets whether instrumentation debugging is enabled.
  /// </summary>
  public static bool IsInstrumentationDebugEnabled =>
    IsEnvironmentVariableEnabled(CoverletMtpDebugConstants.InstrumentationDebug);

  /// <summary>
  /// Gets whether exception logging is enabled.
  /// </summary>
  public static bool IsExceptionLogEnabled =>
    IsEnvironmentVariableEnabled(CoverletMtpDebugConstants.ExceptionLogEnabled);
}
