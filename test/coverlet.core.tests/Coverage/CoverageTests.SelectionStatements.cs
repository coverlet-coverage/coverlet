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
        public void SelectionStatements_If()
        {
            // We need to pass file name to remote process where it save instrumentation result
            // Similar to msbuild input/output
            string path = Path.GetTempFileName();
            try
            {
                // Lambda will run in a custom process to avoid issue with statics and file locking
                RemoteExecutor.Invoke(async pathSerialize =>
                {
                    // Run load and call a delegate passing class as dynamic to simplify method call
                    CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<SelectionStatements>(instance =>
                    {
                        // We call method to trigger coverage hits
                        instance.If(true);

                        // For now we have only async Run helper
                        return Task.CompletedTask;
                    }, persistPrepareResultToFile: pathSerialize);

                    // we return 0 if we return something different assert fail
                    return 0;
                }, path).Dispose();

                // We retrive and load CoveragePrepareResult and run coverage calculation
                // Similar to msbuild coverage result task
                CoverageResult result = TestInstrumentationHelper.GetCoverageResult(path);

                // Generate html report to check
                // TestInstrumentationHelper.GenerateHtmlReport(result);

                // Asserts on doc/lines/branches
                result.Document("Instrumentation.SelectionStatements.cs")
                      // (line, hits)
                      .AssertLinesCovered((11, 1), (15, 0))
                      // (line,ordinal,hits)
                      .AssertBranchesCovered((9, 0, 1), (9, 1, 0));
            }
            finally
            {
                // Cleanup tmp file
                File.Delete(path);
            }
        }

        [Fact]
        public void SelectionStatements_Switch()
        {
            string path = Path.GetTempFileName();
            try
            {
                RemoteExecutor.Invoke(async pathSerialize =>
                {
                    CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<SelectionStatements>(instance =>
                    {
                        instance.Switch(1);
                        return Task.CompletedTask;
                    }, persistPrepareResultToFile: pathSerialize);
                    return 0;
                }, path).Dispose();

                CoverageResult result = TestInstrumentationHelper.GetCoverageResult(path);

                result.Document("Instrumentation.SelectionStatements.cs")
                      .AssertLinesCovered(BuildConfiguration.Release, (24, 1), (26, 0), (28, 0))
                      .AssertBranchesCovered(BuildConfiguration.Release, (24, 1, 1))
                      .AssertLinesCovered(BuildConfiguration.Debug, (20, 1), (21, 1), (24, 1), (30, 1))
                      .AssertBranchesCovered(BuildConfiguration.Debug, (21, 0, 0), (21, 1, 1), (21, 2, 0), (21, 3, 0));
            }
            finally
            {
                File.Delete(path);
            }
        }
    }
}