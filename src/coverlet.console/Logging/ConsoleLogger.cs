using System;
using Coverlet.Core.Logging;
using SystemConsole = System.Console;

namespace Coverlet.Console.Logging
{
    class ConsoleLogger : ILogger
    {
        private static readonly object _sync = new object();

        public void LogError(string message) => WriteLine(message, ConsoleColor.Red);

        public void LogError(string message, Exception ex) => LogError($"{message}{Environment.NewLine}{ex}");

        public void LogInformation(string message) => WriteLine(message);

        public void LogVerbose(string message) => throw new System.NotImplementedException();

        public void LogWarning(string message) => WriteLine(message, ConsoleColor.Yellow);

        private static void WriteLine(string message, ConsoleColor color  = ConsoleColor.White)
        {
            ConsoleColor currentForegroundColor;
            if (color != (currentForegroundColor = SystemConsole.ForegroundColor))
            {
                lock (_sync)
                {
                    SystemConsole.ForegroundColor = color;
                    SystemConsole.WriteLine(message);
                    SystemConsole.ForegroundColor = currentForegroundColor;
                }
            }
            else
            {
                SystemConsole.WriteLine(message);
            }
        }
    }
}