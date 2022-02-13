using coverlet.testsubject;
using System.Threading.Tasks;
using System.Collections.Generic;
using Xunit;

namespace Coverlet.Core.PerformanceTest
{
    /// <summary>
    /// Test the performance of coverlet by running a unit test that calls a reasonably big and complex test class.
    /// Enable the test, compile, then run the test in the command line:
    /// <code>
    /// dotnet test /p:CollectCoverage=true test/Coverlet.Core.PerformanceTest/
    /// </code>
    /// </summary>
    public class PerformanceTest
    {
        [Theory]
        [InlineData(20_000)]
        public void TestPerformance(int iterations)
        {
            var big = new BigClass();

            var tasks = new List<Task>();

            for (var i = 0; i < iterations; i++)
            {
                var j = i;
                tasks.Add(Task.Run(() => big.Do(j)));
            }

            Task.WaitAll(tasks.ToArray());
        }
    }
}