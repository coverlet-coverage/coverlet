using System;

using Coverlet.Core.Abstractions;

namespace Coverlet.Core.Helpers
{
    public class SystemConsole : IConsole
    {
        public void WriteLine(string value)
        {
            Console.WriteLine(value);
        }
    }
}
