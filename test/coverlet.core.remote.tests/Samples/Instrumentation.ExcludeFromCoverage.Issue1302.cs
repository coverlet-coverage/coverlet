using System;

namespace coverlet.core.remote.samples.tests
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
