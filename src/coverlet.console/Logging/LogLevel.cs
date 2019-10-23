namespace Coverlet.Console.Logging
{
    /// <summary>
    /// Defines logging severity levels.
    /// </summary>
    enum LogLevel
    {
        /// <summary>
        /// Logs that track the general flow of the application. These logs should have long-term value.
        /// </summary>
        Detailed = 0,

        /// <summary>
        /// Logs that highlight an abnormal or unexpected event in the application flow, but do not otherwise cause the
        /// application execution to stop.
        /// </summary>
        Normal = 1,

        /// <summary>
        /// Logs that highlight when the current flow of execution is stopped due to a failure. These should indicate a
        /// failure in the current activity, not an application-wide failure.
        /// </summary>
        Minimal = 2,

        /// <summary>
        /// Not used for writing log messages. Specifies that a logging category should not write any messages except warning and errors.
        /// </summary>
        Quiet = 3
    }
}
