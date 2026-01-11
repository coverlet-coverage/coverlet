// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Coverlet.MTP.EnvironmentVariables;

/// <summary>
/// Environment variables used for communication between the controller process
/// (CoverletExtensionCollector) and the test host process (CoverletInProcessHandler).
/// </summary>
internal static class CoverletMtpEnvironmentVariables
{
  /// <summary>
  /// Indicates that coverage collection is enabled. Value: "true"
  /// </summary>
  public const string CoverageEnabled = "COVERLET_MTP_COVERAGE_ENABLED";

  /// <summary>
  /// Unique identifier for correlating coverage results. Value: GUID string
  /// </summary>
  public const string CoverageIdentifier = "COVERLET_MTP_COVERAGE_IDENTIFIER";

  /// <summary>
  /// Path where hit files are written. Value: directory path
  /// </summary>
  public const string HitsFilePath = "COVERLET_MTP_HITS_FILE_PATH";

  /// <summary>
  /// Debug flag to launch debugger in test host. Value: "1" to enable
  /// </summary>
  public const string InProcessDebug = "COVERLET_MTP_INPROC_DEBUG";

  /// <summary>
  /// Enable exception logging in test host. Value: "1" to enable
  /// </summary>
  public const string InProcessExceptionLog = "COVERLET_MTP_INPROC_EXCEPTIONLOG_ENABLED";
}
