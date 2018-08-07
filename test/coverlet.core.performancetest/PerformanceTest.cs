using coverlet.testsubject;
using System.Threading.Tasks;
using System.Collections.Generic;
using Xunit;

namespace coverlet.core.performancetest
{
    /// <summary>
    /// Test the performance of coverlet by running a unit test that calls a reasonably big and complex test class.
    /// Enable the test, compile, then run the test in the command line:
    /// <code>
    /// dotnet test -p:CollectCoverage=true -p:CoverletOutputFormat=opencover test/coverlet.core.performancetest/
    /// </code>
    /// </summary>
    public class PerformanceTest
    {
        [Theory(Skip = "Only enabled when explicitly testing performance.")]
        [InlineData(150)]
        public void TestPerformance(int iterations)
        {
            var big = new BigClass();

            List<Task> tasks = new List<Task>();

            for (var i = 0; i < iterations; i++)
            {
                tasks.Add(Task.Run(() => big.Do(i)));
            }

            Task.WaitAll(tasks.ToArray());
        }
    }
}