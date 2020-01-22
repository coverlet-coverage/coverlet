using System.IO;
using System.Threading.Tasks;

using Coverlet.Core.Samples.Tests;
using Coverlet.Tests.RemoteExecutor;
using Xunit;

namespace Coverlet.Core.Tests
{
    public partial class CoverageTests
    {
        [Fact]
        public void Lambda_Issue343()
        {
            string path = Path.GetTempFileName();
            try
            {
                RemoteExecutor.Invoke(async pathSerialize =>
                {
                    CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<Lambda_Issue343>(instance =>
                    {
                        instance.InvokeAnonymous_Test();
                        ((Task<bool>)instance.InvokeAnonymousAsync_Test()).ConfigureAwait(false).GetAwaiter().GetResult();
                        return Task.CompletedTask;
                    }, persistPrepareResultToFile: pathSerialize);
                    return 0;
                }, path).Dispose();

                CoverageResult result = TestInstrumentationHelper.GetCoverageResult(path);

                result.Document("Instrumentation.Lambda.cs")
                      .AssertLinesCoveredAllBut(BuildConfiguration.Debug, 23, 51)
                      .AssertBranchesCovered(BuildConfiguration.Debug,
                        // Expected branches
                        (22, 0, 0),
                        (22, 1, 1),
                        (50, 2, 0),
                        (50, 3, 1),
                        // Unexpected branches
                        (20, 0, 1),
                        (20, 1, 1),
                        (49, 0, 1),
                        (49, 1, 0),
                        (54, 4, 0),
                        (54, 5, 1),
                        (48, 0, 1),
                        (48, 1, 1)
                      );
            }
            finally
            {
                File.Delete(path);
            }
        }
    }
}