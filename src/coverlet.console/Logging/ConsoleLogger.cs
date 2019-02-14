using System;
using Coverlet.Core;
using static System.Console;

namespace Coverlet.Console.Logging
{
    class ConsoleLogger : ILogger
    {
        private static readonly object _sync = new object();

        public void LogError(string message)
        {
            lock (_sync)
            {
                var previous = ForegroundColor;
                ForegroundColor = ConsoleColor.Red;
                WriteLine(message);
                ForegroundColor = previous;
            }
        }

        public void LogInformation(string message)
        {
            lock (_sync)
            {
                WriteLine(message);
            }
        }

        public void LogSuccess(string message)
        {
            lock (_sync)
            {
                var previous = ForegroundColor;
                ForegroundColor = ConsoleColor.Green;
                WriteLine(message);
                ForegroundColor = previous;
            }
        }

        public void LogVerbose(string message)
        {
            throw new System.NotImplementedException();
        }

        public void LogWarning(string message)
        {
            lock (_sync)
            {
                var previous = ForegroundColor;
                ForegroundColor = ConsoleColor.Yellow;
                WriteLine(message);
                ForegroundColor = previous;
            }
        }
    }
}