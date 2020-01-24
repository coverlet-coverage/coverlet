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

                        instance.InvokeAnonymous_MoreTests();

                        return Task.CompletedTask;
                    }, persistPrepareResultToFile: pathSerialize, disableRestoreModules: true);
                    return 0;
                }, path, invokeInProcess: true).Dispose();

                CoverageResult result = TestInstrumentationHelper.GetCoverageResult(path);

                result.GenerateReport(show: true)
                      .Document("Instrumentation.Lambda.cs")
                      .AssertLinesCoveredAllBut(BuildConfiguration.Debug, 23, 51, 69, 74, 75, 91, 92, 112)
                      .AssertBranchesCovered(BuildConfiguration.Debug,
                        // Expected branches
                        (22, 0, 0),
                        (22, 1, 1),
                        (50, 0, 0),
                        (50, 1, 1),

                        (66, 0, 0),
                        (66, 1, 1),
                        (68, 0, 0),
                        (68, 1, 1),
                        (90, 0, 0),
                        (90, 1, 1),
                        (111, 0, 0),
                        (111, 1, 2)

                        // Unexpected branches
                      // Lamda issue - Fixed
                      //(20, 0, 1),
                      //(20, 1, 1),
                      //(48, 0, 1),
                      //(48, 1, 1)
                      // State machine issues - Fixed
                      //(49, 0, 1),
                      //(49, 1, 0),
                      //(54, 4, 0),
                      //(54, 5, 1),
                      );
            }
            finally
            {
                File.Delete(path);
            }
        }
    }
}