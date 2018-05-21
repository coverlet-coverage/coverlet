using System;

namespace Coverlet.Core.Logging
{
    public class ConsoleLogger : ILogger
    {
        private ConsoleLogger()
        {
        }

        public static ILogger Instance { get; } = new ConsoleLogger();

        public void Log(string s)
        {
            if (s == null)
                return;
            Console.WriteLine(s);
        }
    }
}