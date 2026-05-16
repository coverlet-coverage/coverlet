// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

namespace Coverlet.Core.Tests.Infrastructure;

/// <summary>
/// Helper class to detect the test execution environment.
/// Used to conditionally skip tests that don't work in Visual Studio
/// but should run in CI/terminal environments.
/// </summary>
public static class TestEnvironment
{
  /// <summary>
  /// Returns true if tests are running in a CI environment.
  /// Uses the same detection as Directory.Build.props (TF_BUILD, GITHUB_ACTIONS).
  /// When in CI, we run ALL tests including those that might hang in VS.
  /// </summary>
  public static bool IsCI =>
    // Azure DevOps - same as $(TF_BUILD) in Directory.Build.props
    string.Equals(Environment.GetEnvironmentVariable("TF_BUILD"), "true", StringComparison.OrdinalIgnoreCase) ||
    // GitHub Actions - same as $(GITHUB_ACTIONS) in Directory.Build.props
    string.Equals(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"), "true", StringComparison.OrdinalIgnoreCase);

  /// <summary>
  /// Returns true if tests are running inside Visual Studio Test Explorer (interactively).
  /// This checks for VS-specific environment variables that are ONLY set by VS Test Explorer.
  /// </summary>
  public static bool IsVisualStudio
  {
    get
    {
      // If we're in CI, never report as Visual Studio
      if (IsCI)
      {
        return false;
      }

      try
      {
        // ServiceHubLogSessionKey is set by VS when running tests via Test Explorer
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ServiceHubLogSessionKey")))
        {
          return true;
        }

        // VS Test Explorer sets VSTEST_RUNNER_DEBUG_TRACE or similar
        // Check the process name for testhost spawned by VS
        using Process currentProcess = Process.GetCurrentProcess();
        string processName = currentProcess.ProcessName;

        // testhost processes spawned by VS have specific parent
        if (processName.Contains("testhost", StringComparison.OrdinalIgnoreCase))
        {
          // Check for VS-specific environment variable that only VS sets
          // VisualStudioEdition is set when running from VS
          if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("VisualStudioEdition")))
          {
            return true;
          }
        }
      }
      catch
      {
        // If we can't determine, assume not VS to run tests
      }

      return false;
    }
  }

  /// <summary>
  /// Returns true if tests are running from command line (dotnet test) or CI.
  /// </summary>
  public static bool IsCommandLine => !IsVisualStudio;

  /// <summary>
  /// Returns true when stdin is connected to an interactive terminal (not piped or redirected).
  /// Tests that call Console.ReadKey() will block indefinitely in this state.
  /// </summary>
  public static bool HasInteractiveStdin => !Console.IsInputRedirected;

  /// <summary>
  /// Message to use when skipping tests in Visual Studio.
  /// </summary>
  public const string VisualStudioSkipMessage = "This test is skipped in Visual Studio due to execution environment limitations. Run from CLI or CI.";

  /// <summary>
  /// Message to use when skipping tests that call Console.ReadKey() in interactive terminals.
  /// </summary>
  public const string InteractiveStdinSkipMessage = "This test calls Console.ReadKey() which blocks in an interactive terminal. Run in CI or pipe stdin.";
}
