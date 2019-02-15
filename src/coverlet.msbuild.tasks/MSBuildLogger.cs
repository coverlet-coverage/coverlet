using System;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using ILogger = Coverlet.Core.Logging.ILogger;

namespace Coverlet.MSbuild.Tasks
{
    class MSBuildLogger : ILogger
    {
        private readonly TaskLoggingHelper _log;

        public MSBuildLogger(TaskLoggingHelper log) => _log = log;

        public void LogVerbose(string message) => _log.LogMessage(message, MessageImportance.Low);

        public void LogInformation(string message)=> _log.LogMessage(message);

        public void LogWarning(string message) => _log.LogWarning(message);

        public void LogError(string message) => _log.LogError(message);
      
        public void LogError(Exception exception) => _log.LogErrorFromException(exception);
    }
}
