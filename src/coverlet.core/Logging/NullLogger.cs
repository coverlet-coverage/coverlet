using Coverlet.Core;

namespace coverlet.console.Logging
{
    public sealed class NullLogger :ILogger
    {
        public static ILogger Instance {get; } = new NullLogger();

        public void LogSuccess(string message)
        {
        }

        public void LogVerbose(string message)
        {
        }

        public void LogInformation(string message)
        {
        }

        public void LogWarning(string message)
        {
        }

        public void LogError(string message)
        {
        }
    }
}
