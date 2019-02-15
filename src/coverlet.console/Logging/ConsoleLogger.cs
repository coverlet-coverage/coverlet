using System;
using Coverlet.Core.Logging;
using SystemConsole = System.Console;

namespace Coverlet.Console.Logging
{
    class ConsoleLogger : ILogger
    {
        private static readonly object _sync = new object();

        public void LogError(string message) => WriteLine(message, ConsoleColor.Red);

        public void LogError(Exception exception) => LogError(exception.ToString());

        public void LogInformation(string message) => WriteLine(message, SystemConsole.ForegroundColor);

        public void LogVerbose(string message) => throw new NotImplementedException();

        public void LogWarning(string message) => WriteLine(message, ConsoleColor.Yellow);

        private static void WriteLine(string message, ConsoleColor color)
        {
            lock (_sync)
            {
                ConsoleColor currentForegroundColor;
                if (color != (currentForegroundColor = SystemConsole.ForegroundColor))
                {
                    SystemConsole.ForegroundColor = color;
                    SystemConsole.WriteLine(message);
                    SystemConsole.ForegroundColor = currentForegroundColor;
                }
                else
                {
                    SystemConsole.WriteLine(message);
                }
            }
        }
    }
}