using System;

namespace Coverlet.Core.Abstracts
{
    internal interface ILogger
    {
        void LogVerbose(string message);
        void LogInformation(string message, bool important = false);
        void LogWarning(string message);
        void LogError(string message);
        void LogError(Exception exception);
    }
}