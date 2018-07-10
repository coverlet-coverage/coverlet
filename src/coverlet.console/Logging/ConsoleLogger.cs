using System;
using static System.Console;

namespace Coverlet.Console.Logging
{
    class ConsoleLogger : ILogger
    {
        public void LogError(string message)
        {
            ForegroundColor = ConsoleColor.Red;
            WriteLine(message);
            ForegroundColor = ConsoleColor.White;
        }

        public void LogInformation(string message)
        {
            WriteLine(message);
        }

        public void LogSuccess(string message)
        {
            ForegroundColor = ConsoleColor.Green;
            WriteLine(message);
            ForegroundColor = ConsoleColor.White;
        }

        public void LogVerbose(string message)
        {
            throw new System.NotImplementedException();
        }

        public void LogWarning(string message)
        {
            ForegroundColor = ConsoleColor.Yellow;
            WriteLine(message);
            ForegroundColor = ConsoleColor.White;
        }
    }
}