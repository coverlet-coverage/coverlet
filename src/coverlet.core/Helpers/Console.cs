using System;

using Coverlet.Core.Abstracts;

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
