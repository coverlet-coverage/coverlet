// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Coverlet.MTP.validation.tests;

/// <summary>
/// Holds information about the generated test project structure.
/// Implements cleanup to remove the solution directory on disposal.
/// Set environment variable <c>COVERLET_KEEP_TESTPROJECTS=1</c> (or <c>true</c>) to opt out of
/// cleanup for local debugging.
/// </summary>
internal sealed class TestProjectInfo : IDisposable
{
    public string SolutionPath { get; }
    public string TestProjectPath { get; }
    public string OutputDirectory { get; }
    public string SolutionDirectory { get; }

    public TestProjectInfo(
        string solutionPath,
        string testProjectPath,
        string outputDirectory,
        string solutionDirectory)
    {
        SolutionPath = solutionPath;
        TestProjectPath = testProjectPath;
        OutputDirectory = outputDirectory;
        SolutionDirectory = solutionDirectory;
    }

    public void Dispose()
    {
        string? keep = Environment.GetEnvironmentVariable("COVERLET_KEEP_TESTPROJECTS");
        if (keep is "1" || string.Equals(keep, "true", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (!Directory.Exists(SolutionDirectory))
        {
            return;
        }

        try
        {
            Directory.Delete(SolutionDirectory, recursive: true);
        }
        catch (Exception ex)
        {
            // Disposal is best-effort; a failure must not fail the test run.
            Debug.WriteLine($"Warning: cleanup failed for '{SolutionDirectory}': {ex.Message}");
        }
    }
}

/// <summary>Captures the output of a <c>dotnet exec</c> test run.</summary>
internal sealed class TestResult
{
    public int ExitCode { get; }
    public string StandardOutput { get; }
    public string ErrorText { get; }

    public string CombinedOutput =>
        $"=== STDOUT ===\n{StandardOutput}\n\n=== STDERR ===\n{ErrorText}";

    public TestResult(int exitCode, string standardOutput, string errorText)
    {
        ExitCode = exitCode;
        StandardOutput = standardOutput;
        ErrorText = errorText;
    }
}
