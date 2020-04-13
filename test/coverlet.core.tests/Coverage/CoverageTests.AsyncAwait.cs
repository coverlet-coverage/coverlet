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
        public void AsyncAwait()
        {
            string path = Path.GetTempFileName();
            try
            {
                FunctionExecutor.Run(async (string[] pathSerialize) =>
                {
                    CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<AsyncAwait>(instance =>
                    {
                        instance.SyncExecution();

                        int res = ((Task<int>)instance.AsyncExecution(true)).ConfigureAwait(false).GetAwaiter().GetResult();
                        res = ((Task<int>)instance.AsyncExecution(1)).ConfigureAwait(false).GetAwaiter().GetResult();
                        res = ((Task<int>)instance.AsyncExecution(2)).ConfigureAwait(false).GetAwaiter().GetResult();
                        res = ((Task<int>)instance.AsyncExecution(3)).ConfigureAwait(false).GetAwaiter().GetResult();
                        res = ((Task<int>)instance.ContinuationCalled()).ConfigureAwait(false).GetAwaiter().GetResult();
                        res = ((Task<int>)instance.ConfigureAwait()).ConfigureAwait(false).GetAwaiter().GetResult();

                        return Task.CompletedTask;
                    }, persistPrepareResultToFile: pathSerialize[0]);
                    return 0;
                }, new string[] { path });

                TestInstrumentationHelper.GetCoverageResult(path)
                .Document("Instrumentation.AsyncAwait.cs")
                .AssertLinesCovered(BuildConfiguration.Debug,
                                    // AsyncExecution(bool)
                                    (10, 1), (11, 1), (12, 1), (14, 1), (16, 1), (17, 0), (18, 0), (19, 0), (21, 1), (22, 1),
                                    // Async
                                    (25, 9), (26, 9), (27, 9), (28, 9),
                                    // SyncExecution
                                    (31, 1), (32, 1), (33, 1),
                                    // Sync
                                    (36, 1), (37, 1), (38, 1),
                                    // AsyncExecution(int)
                                    (41, 3), (42, 3), (43, 3), (46, 1), (47, 1), (48, 1), (51, 1),
                                    (52, 1), (53, 1), (56, 1), (57, 1), (58, 1), (59, 1),
                                    (62, 0), (63, 0), (64, 0), (65, 0), (68, 0), (70, 3), (71, 3),
                                    // ContinuationNotCalled
                                    (74, 0), (75, 0), (76, 0), (77, 0), (78, 0),
                                    // ContinuationCalled -> line 83 should be 1 hit some issue with Continuation state machine
                                    (81, 1), (82, 1), (83, 2), (84, 1), (85, 1),
                                    // ConfigureAwait
                                    (89, 1), (90, 1)
                                    )
                .AssertBranchesCovered(BuildConfiguration.Debug, (16, 0, 0), (16, 1, 1), (43, 0, 3), (43, 1, 1), (43, 2, 1), (43, 3, 1), (43, 4, 0))
                // Real branch should be 2, we should try to remove compiler generated branch in method ContinuationNotCalled/ContinuationCalled
                // for Continuation state machine
                .ExpectedTotalNumberOfBranches(BuildConfiguration.Debug, 2);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void AsyncAwait_Issue_669_1()
        {
            string path = Path.GetTempFileName();
            try
            {
                FunctionExecutor.Run(async (string[] pathSerialize) =>
                {
                    CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<Issue_669_1>(instance =>
                    {
                        ((Task)instance.Test()).ConfigureAwait(false).GetAwaiter().GetResult();
                        return Task.CompletedTask;
                    },
                    persistPrepareResultToFile: pathSerialize[0]);

                    return 0;
                }, new string[] { path });

                TestInstrumentationHelper.GetCoverageResult(path)
                .Document("Instrumentation.AsyncAwait.cs")
                .AssertLinesCovered(BuildConfiguration.Debug,
                (97, 1), (98, 1), (99, 1), (101, 1), (102, 1), (103, 1),
                (110, 1), (111, 1), (112, 1), (113, 1),
                (116, 1), (117, 1), (118, 1), (119, 1));
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void AsyncAwait_Issue_669_2()
        {
            string path = Path.GetTempFileName();
            try
            {
                FunctionExecutor.Run(async (string[] pathSerialize) =>
                {
                    CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<Issue_669_2>(instance =>
                    {
                        ((ValueTask<System.Net.Http.HttpResponseMessage>)instance.SendRequest()).ConfigureAwait(false).GetAwaiter().GetResult();
                        return Task.CompletedTask;
                    },
                    persistPrepareResultToFile: pathSerialize[0]);

                    return 0;
                }, new string[] { path });

                TestInstrumentationHelper.GetCoverageResult(path)
                .Document("Instrumentation.AsyncAwait.cs")
                .AssertLinesCovered(BuildConfiguration.Debug, (7, 1), (10, 1), (11, 1), (12, 1), (13, 1), (15, 1))
                .ExpectedTotalNumberOfBranches(BuildConfiguration.Debug, 0);
            }
            finally
            {
                File.Delete(path);
            }
        }
    }
}