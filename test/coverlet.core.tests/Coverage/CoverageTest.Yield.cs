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
        public void Yield_Single()
        {
            string path = Path.GetTempFileName();
            try
            {
                FunctionExecutor.Run(async (string[] pathSerialize) =>
                {
                    CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<Yield>(instance =>
                    {
                        foreach(var _ in instance.One());

                        return Task.CompletedTask;
                    }, persistPrepareResultToFile: pathSerialize[0]);

                    return 0;
                }, new string[] { path });

                CoverageResult result = TestInstrumentationHelper.GetCoverageResult(path);

                result.Document("Instrumentation.Yield.cs")
                      .AssertLinesCovered((9, 1))
                      .ExpectedTotalNumberOfBranches(0);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
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
