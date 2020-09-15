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
        public void AsyncAwaitWithValueTask()
        {
            string path = Path.GetTempFileName();
            try
            {
                FunctionExecutor.Run(async (string[] pathSerialize) =>
                {
                    CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<AsyncAwaitValueTask>(async instance =>
                    {
                        instance.SyncExecution();

                        int res = await ((ValueTask<int>)instance.AsyncExecution(true)).ConfigureAwait(false);
                        res = await ((ValueTask<int>)instance.AsyncExecution(1)).ConfigureAwait(false);
                        res = await ((ValueTask<int>)instance.AsyncExecution(2)).ConfigureAwait(false);
                        res = await ((ValueTask<int>)instance.AsyncExecution(3)).ConfigureAwait(false);
                        res = await ((ValueTask<int>)instance.ConfigureAwait()).ConfigureAwait(false);
                        res = ((Task<int>)instance.WrappingValueTaskAsTask()).ConfigureAwait(false).GetAwaiter().GetResult();
                    }, persistPrepareResultToFile: pathSerialize[0]);
                    return 0;
                }, new string[] { path });

                TestInstrumentationHelper.GetCoverageResult(path)
                .Document("Instrumentation.AsyncAwaitValueTask.cs")
                .AssertLinesCovered(BuildConfiguration.Debug,
                                    // AsyncExecution(bool)
                                    (12, 1), (13, 1), (15, 1), (16, 1), (18, 1), (19, 1), (21, 1), (23, 1), (24, 0), (25, 0), (26, 0), (28, 1), (29, 1),
                                    // Async
                                    (32, 10), (33, 10), (34, 10), (35, 10), (36, 10),
                                    // AsyncExecution(int)
                                    (39, 3), (40, 3), (42, 3), (43, 3), (45, 3), (46, 3),
                                    (49, 1), (50, 1), (51, 1), (54, 1), (55, 1), (56, 1), (59, 1), (60, 1), (62, 1),
                                    (65, 0), (66, 0), (67, 0), (68, 0), (71, 0),
                                    // SyncExecution
                                    (77, 1), (78, 1), (79, 1),
                                    // Sync
                                    (82, 1), (83, 1), (84, 1),
                                    // ConfigureAwait
                                    (87, 1), (88, 1), (90, 1), (91, 1), (93, 1), (94, 1), (95, 1),
                                    // WrappingValueTaskAsTask
                                    (98, 1), (99, 1), (101, 1), (102, 1), (104, 1), (106, 1), (107, 1)
                                    )
                .AssertBranchesCovered(BuildConfiguration.Debug,
                                       // AsyncExecution(bool) if statement
                                       (23, 0, 0), (23, 1, 1),
                                       // AsyncExecution(int) switch statement
                                       (46, 0, 3), (46, 1, 1), (46, 2, 1), (46, 3, 1), (46, 4, 0)
                                       )
                .ExpectedTotalNumberOfBranches(BuildConfiguration.Debug, 2);
            }
            finally
            {
                File.Delete(path);
            }
        }
    }
}
