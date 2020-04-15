using System.IO;
using System.Threading.Tasks;

using Coverlet.Core.Samples.Tests;
using Coverlet.Tests.Xunit.Extensions;
using Tmds.Utils;
using Xunit;

namespace Coverlet.Core.Tests
{
    public partial class CoverageTests : ExternalProcessExecutionTest
    {
        [ConditionalFact]
        [SkipOnOS(OS.MacOS)]
        public void Yield_Single()
        {
            // We need to pass file name to remote process where it save instrumentation result
            // Similar to msbuild input/output
            string path = Path.GetTempFileName();
            try
            {
                // Lambda will run in a custom process to avoid issue with statics and file locking
                FunctionExecutor.Run(async (string[] pathSerialize) =>
                {
                    // Run load and call a delegate passing class as dynamic to simplify method call
                    CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<Yield>(instance =>
                    {
                        // We call method to trigger coverage hits
                        foreach(var _ in instance.One());

                        // For now we have only async Run helper
                        return Task.CompletedTask;
                    }, persistPrepareResultToFile: pathSerialize[0]);

                    // we return 0 if we return something different assert fail
                    return 0;
                }, new string[] { path });

                // We retrive and load CoveragePrepareResult and run coverage calculation
                // Similar to msbuild coverage result task
                CoverageResult result = TestInstrumentationHelper.GetCoverageResult(path);

                // Generate html report to check
                // TestInstrumentationHelper.GenerateHtmlReport(result);

                // Asserts on doc/lines/branches
                result.Document("Instrumentation.Yield.cs")
                      // (line, hits)
                      .AssertLinesCovered((9, 1))
                      .ExpectedTotalNumberOfBranches(0);
            }
            finally
            {
                // Cleanup tmp file
                File.Delete(path);
            }
        }

        [ConditionalFact]
        [SkipOnOS(OS.MacOS)]
        public void Yield_Multiple()
        {
            string path = Path.GetTempFileName();
            try
            {
                FunctionExecutor.Run(async (string[] pathSerialize) =>
                {
                    CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<Yield>(instance =>
                    {
                        foreach (var _ in instance.Two());
                        return Task.CompletedTask;
                    }, persistPrepareResultToFile: pathSerialize[0]);
                    return 0;
                }, new string[] { path });

                CoverageResult result = TestInstrumentationHelper.GetCoverageResult(path);

                result.Document("Instrumentation.Yield.cs")
                      .AssertLinesCovered((14, 1), (15, 1))
                      .ExpectedTotalNumberOfBranches(0);
            }
            finally
            {
                File.Delete(path);
            }
        }
    }
}
