using System.IO;
using System.Threading.Tasks;

using Coverlet.Core.Samples.Tests;
using Tmds.Utils;
using Xunit;

namespace Coverlet.Core.Tests
{
    public partial class CoverageTests : ExternalProcessExecutionTest
    {
        [Fact]
        public void DoesNotReturnSample()
        {
            string path = Path.GetTempFileName();
            try
            {
                // After debugging and before to push on PR change to Run for out of process test on CI
                // FunctionExecutor.Run(async (string[] pathSerialize) =>

                // For in-process debugging
                FunctionExecutor.RunInProcess(async (string[] pathSerialize) =>
                {
                    CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<DoesNotReturn>(instance =>
                    {
                        try
                        {
                            instance.NoBranches(); // call method to test it's a dynamic
                        }
                        catch
                        {
                            // will throw do nothing 
                        }
                        return Task.CompletedTask;
                    }, persistPrepareResultToFile: pathSerialize[0]);
                    return 0;
                }, new string[] { path });

                TestInstrumentationHelper.GetCoverageResult(path)
                .GenerateReport(show: true) // remove at the end of debugging it allows to open and show report for fast check
                .Document("Instrumentation.DoesNotReturn.cs") // chose cs source of samples where check rows
                // .AssertBranchesCovered(... Add coverage check
                ;
            }
            finally
            {
                File.Delete(path);
            }
        }
    }
}