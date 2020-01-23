using System;
using coverlet.core.Enums;

namespace Coverlet.Core.Abstracts
{
    internal interface ILogger
    {
        LogLevel Level { get; set; }
        void LogVerbose(string message);
        void LogInformation(string message, bool important = false);
        void LogWarning(string message);
        void LogError(string message);
        void LogError(Exception exception);
    }
}