// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Coverlet.MTP.Diagnostics;

/// <summary>
/// Environment variable names for debugging and tracing coverlet.MTP.
/// </summary>
public static class CoverletMtpDebugConstants
{
  /// <summary>
  /// Enable debugger launch popup when coverage extension initializes.
  /// Set to "1" to enable.
  /// </summary>
  public const string DebugLaunch = "COVERLET_MTP_DEBUG";

  /// <summary>
  /// Enable wait loop for debugger attach during extension initialization.
  /// Set to "1" to enable. Extension will wait until debugger is attached.
  /// </summary>
  public const string DebugWaitForAttach = "COVERLET_MTP_DEBUG_WAIT";

  /// <summary>
  /// Enable detailed tracker logging for instrumented modules.
  /// Set to "1" to enable. Logs will be written near module location.
  /// </summary>
  public const string EnableTrackerLog = "COVERLET_ENABLETRACKERLOG";

  /// <summary>
  /// Enable verbose instrumentation logging.
  /// Set to "1" to enable detailed instrumentation diagnostics.
  /// </summary>
  public const string InstrumentationDebug = "COVERLET_MTP_INSTRUMENTATION_DEBUG";

  /// <summary>
  /// Enable coverage result collection debugging.
  /// Set to "1" to enable.
  /// </summary>
  public const string CollectionDebug = "COVERLET_MTP_COLLECTION_DEBUG";

  /// <summary>
  /// Enable exception logging for coverage operations.
  /// Set to "1" to enable detailed exception logging.
  /// </summary>
  public const string ExceptionLogEnabled = "COVERLET_MTP_EXCEPTIONLOG_ENABLED";
}
