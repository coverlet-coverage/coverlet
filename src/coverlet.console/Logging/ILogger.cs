namespace Coverlet.Console.Logging
{
    interface ILogger
    {
        void LogSuccess(string message);
        void LogVerbose(string message);
        void LogInformation(string message);
        void LogWarning(string message);
        void LogError(string message);
    }
}