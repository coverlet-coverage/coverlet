using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using ILogger = Coverlet.Core.ILogger;

namespace Coverlet.MSbuild.Tasks
{
    class MSBuildLogger : ILogger
    {
        private readonly TaskLoggingHelper _log;

        public MSBuildLogger(TaskLoggingHelper _log)
        {
            this._log = _log;
        }

        public void LogSuccess(string message)
        {
            LogInformation(message);
        }

        public void LogVerbose(string message)
        {
            _log.LogMessageFromText(message, MessageImportance.Low);
        }

        public void LogInformation(string message)
        {
            _log.LogMessageFromText(message, MessageImportance.Normal);
        }

        public void LogWarning(string message)
        {
            _log.LogWarning(message);
        }

        public void LogError(string message)
        {
            _log.LogError(message);
        }
    }
}
