using System;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using ILogger = Coverlet.Core.Abstractions.ILogger;

namespace Coverlet.MSbuild.Tasks
{
    class MSBuildLogger : ILogger
    {
        private const string LogPrefix = "[coverlet] ";

        private readonly TaskLoggingHelper _log;

        public MSBuildLogger(TaskLoggingHelper log) => _log = log;

        public void LogVerbose(string message) => _log.LogMessage(MessageImportance.Low, $"{LogPrefix}{message}");

        // We use `MessageImportance.High` because with `MessageImportance.Normal` doesn't show anything
        public void LogInformation(string message, bool important = false) => _log.LogMessage(MessageImportance.High, $"{LogPrefix}{message}");

        public void LogWarning(string message) => _log.LogWarning($"{LogPrefix}{message}");

        public void LogError(string message) => _log.LogError($"{LogPrefix}{message}");

        public void LogError(Exception exception) => _log.LogErrorFromException(exception, true);
    }
}
