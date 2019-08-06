using System;

/// <summary>
/// Exit Codes returned from Coverlet console process.
/// </summary>
[Flags]
internal enum CommandExitCodes
{
    /// <summary>
    /// Indicates successful run of dotnet test without any test failure and coverage percentage above threshold if provided.
    /// </summary>
    Success = 0,

    /// <summary>
    /// Indicates test failure by dotnet test.
    /// </summary>
    TestFailed = 1,

    /// <summary>
    /// Indicates coverage percentage is below given threshold for one or more threshold type.
    /// </summary>
    CoverageBelowThreshold = 2,

    /// <summary>
    /// Indicates exception occurred during Coverlet process.
    /// </summary>
    Exception = 101,

    /// <summary>
    /// Indicates missing options or empty arguments for Coverlet process.
    /// </summary>
    CommandParsingException = 102
}

