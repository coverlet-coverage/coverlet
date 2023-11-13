using System;

namespace Coverlet.Core.Samples.Tests
{
    public class Issue1302
    {
        public void Run()
        {
            [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
            static Func<string, bool> LocalFunction()
            {
                return myString => myString.Length == 10;
            }

            LocalFunction();
        }
    }
}
