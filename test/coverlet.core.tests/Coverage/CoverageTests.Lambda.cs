using System.IO;
using System.Threading.Tasks;

using Coverlet.Core.Samples.Tests;
using Coverlet.Tests.Xunit.Extensions;
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
                FunctionExecutor.Run(async (string[] pathSerialize) =>
                {
                    CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<Lambda_Issue343>(instance =>
                    {
                        instance.InvokeAnonymous_Test();
                        ((Task<bool>)instance.InvokeAnonymousAsync_Test()).ConfigureAwait(false).GetAwaiter().GetResult();

                        return Task.CompletedTask;
                    }, persistPrepareResultToFile: pathSerialize[0]);
                    return 0;
                }, new string[] { path });

                TestInstrumentationHelper.GetCoverageResult(path)
                .Document("Instrumentation.Lambda.cs")
                .AssertLinesCoveredAllBut(BuildConfiguration.Debug, 24, 52)
                .AssertBranchesCovered(BuildConfiguration.Debug,
                // Expected branches
                (23, 0, 0),
                (23, 1, 1),
                (51, 0, 0),
                (51, 1, 1)
                );
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void AsyncAwait_Issue_730()
        {
            string path = Path.GetTempFileName();
            try
            {
                FunctionExecutor.Run(async (string[] pathSerialize) =>
                {
                    CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<Issue_730>(instance =>
                    {
                        ((Task)instance.Invoke()).ConfigureAwait(false).GetAwaiter().GetResult();
                        return Task.CompletedTask;
                    },
                    persistPrepareResultToFile: pathSerialize[0]);

                    return 0;
                }, new string[] { path });

                TestInstrumentationHelper.GetCoverageResult(path)
                .Document("Instrumentation.Lambda.cs")
                .AssertLinesCovered(BuildConfiguration.Debug, (73, 1), (74, 1), (75, 101), (76, 1), (77, 1))
                .ExpectedTotalNumberOfBranches(BuildConfiguration.Debug, 0);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void Lambda_Issue760()
        {
            string path = Path.GetTempFileName();
            try
            {
                FunctionExecutor.Run(async (string[] pathSerialize) =>
                {
                    CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<Issue_760>(instance =>
                    {
                        ((Task)instance.If()).ConfigureAwait(false).GetAwaiter().GetResult();
                        ((Task)instance.Foreach()).ConfigureAwait(false).GetAwaiter().GetResult();
                        return Task.CompletedTask;
                    },
                    persistPrepareResultToFile: pathSerialize[0]);

                    return 0;
                }, new string[] { path });

                TestInstrumentationHelper.GetCoverageResult(path)
                .Document("Instrumentation.Lambda.cs")
                .AssertLinesCoveredFromTo(BuildConfiguration.Debug, 83, 92)
                .AssertLinesCoveredFromTo(BuildConfiguration.Debug, 95, 104);
            }
            finally
            {
                File.Delete(path);
            }
        }
    }
}