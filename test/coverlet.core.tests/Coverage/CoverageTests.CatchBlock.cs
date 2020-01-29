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
        public void CatchBlock_Issue465()
        {
            string path = Path.GetTempFileName();
            try
            {
                RemoteExecutor.Invoke(async pathSerialize =>
                {
                    CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<CatchBlock>(instance =>
                    {
                        instance.Test();
                        ((Task)instance.TestAsync()).ConfigureAwait(false).GetAwaiter().GetResult();

                        return Task.CompletedTask;
                    }, persistPrepareResultToFile: pathSerialize);
                    return 0;
                }, path).Dispose();

                CoverageResult result = TestInstrumentationHelper.GetCoverageResult(path);

                result.Document("Instrumentation.CatchBlock.cs")
                    .AssertLinesCoveredAllBut(BuildConfiguration.Debug,
                        41, 
                        51,
                        // expected not coverable line
                        32)
                    .ExpectedTotalNumberOfBranches(BuildConfiguration.Debug, 0) 
                    ;
            }
            finally
            {
                File.Delete(path);
            }
        }
    }
}