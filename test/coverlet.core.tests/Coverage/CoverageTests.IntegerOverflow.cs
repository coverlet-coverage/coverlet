using System.IO;
using System.Threading.Tasks;
using Coverlet.Core.Samples.Tests;
using Xunit;

namespace Coverlet.Core.Tests
{
    public partial class CoverageTests
    {
        [Fact]
        public void Overflow_Issue_1266()
        {
            string path = Path.GetTempFileName();
            try
            {
                FunctionExecutor.Run(async (string[] pathSerialize) =>
                {
                    CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<IntegerOverflow>(instance =>
                        {
                            instance.Test();
                            return Task.CompletedTask;
                        },
                        persistPrepareResultToFile: pathSerialize[0]);

                    return 0;
                }, new string[] { path });

                TestInstrumentationHelper.GetCoverageResult(path)
                    .Document("Instrumentation.IntegerOverflow.cs")
                    .AssertLinesCovered(BuildConfiguration.Debug, (16, int.MaxValue), (18, int.MaxValue));
            }
            finally
            {
                File.Delete(path);
            }
        }
    }
}
