// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Coverlet.Core.Abstractions;
using static System.Console;

namespace Coverlet.Console.Logging
{
    class ConsoleLogger : ILogger
    {
        private static readonly object s_sync = new();
        
        public LogLevel Level { get; set; } = LogLevel.Normal;

        public void LogError(string message) => Log(LogLevel.Quiet, message, ConsoleColor.Red);

        public void LogError(Exception exception) => LogError(exception.ToString());

        public void LogInformation(string message, bool important = false) => Log(important ? LogLevel.Minimal : LogLevel.Normal, message, ForegroundColor);

        public void LogVerbose(string message) => Log(LogLevel.Detailed, message, ForegroundColor);

        public void LogWarning(string message) => Log(LogLevel.Quiet, message, ConsoleColor.Yellow);

        private void Log(LogLevel level, string message, ConsoleColor color)
        {
            if (level < Level) return;

            lock (s_sync)
            {
                ConsoleColor currentForegroundColor;
                if (color != (currentForegroundColor = ForegroundColor))
                {
                    ForegroundColor = color;
                    WriteLine(message);
                    ForegroundColor = currentForegroundColor;
                }
                else
                {
                    WriteLine(message);
                }
            }
        }
    }
}
