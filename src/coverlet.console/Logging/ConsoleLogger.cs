using System;
using Coverlet.Core.Logging;
using static System.Console;

namespace Coverlet.Console.Logging
{
    class ConsoleLogger : ILogger
    {
        private static readonly object _sync = new object();

        public void LogError(string message) => Log(message, ConsoleColor.Red);

        public void LogError(Exception exception) => LogError(exception.ToString());

        public void LogInformation(string message) => Log(message, ForegroundColor);

        public void LogVerbose(string message) => throw new NotImplementedException();

        public void LogWarning(string message) => Log(message, ConsoleColor.Yellow);

        private void Log(string message, ConsoleColor color)
        {
            lock (_sync)
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