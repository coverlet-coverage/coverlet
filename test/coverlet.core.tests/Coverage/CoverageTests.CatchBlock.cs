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
        public void CatchBlock_Issue465()
        {
            string path = Path.GetTempFileName();
            try
            {
                FunctionExecutor.Run(async (string[] pathSerialize) =>
                {
                    CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<CatchBlock>(instance =>
                    {
                        instance.Test();
                        instance.Test_Catch();
                        ((Task)instance.TestAsync()).ConfigureAwait(false).GetAwaiter().GetResult();
                        ((Task)instance.TestAsync_Catch()).ConfigureAwait(false).GetAwaiter().GetResult();

                        instance.Test(true);
                        instance.Test_Catch(true);
                        ((Task)instance.TestAsync(true)).ConfigureAwait(false).GetAwaiter().GetResult();
                        ((Task)instance.TestAsync_Catch(true)).ConfigureAwait(false).GetAwaiter().GetResult();

                        instance.Test(false);
                        instance.Test_Catch(false);
                        ((Task)instance.TestAsync(false)).ConfigureAwait(false).GetAwaiter().GetResult();
                        ((Task)instance.TestAsync_Catch(false)).ConfigureAwait(false).GetAwaiter().GetResult();

                        instance.Test_WithTypedCatch();
                        instance.Test_Catch_WithTypedCatch();
                        ((Task)instance.TestAsync_WithTypedCatch()).ConfigureAwait(false).GetAwaiter().GetResult();
                        ((Task)instance.TestAsync_Catch_WithTypedCatch()).ConfigureAwait(false).GetAwaiter().GetResult();

                        instance.Test_WithTypedCatch(true);
                        instance.Test_Catch_WithTypedCatch(true);
                        ((Task)instance.TestAsync_WithTypedCatch(true)).ConfigureAwait(false).GetAwaiter().GetResult();
                        ((Task)instance.TestAsync_Catch_WithTypedCatch(true)).ConfigureAwait(false).GetAwaiter().GetResult();

                        instance.Test_WithTypedCatch(false);
                        instance.Test_Catch_WithTypedCatch(false);
                        ((Task)instance.TestAsync_WithTypedCatch(false)).ConfigureAwait(false).GetAwaiter().GetResult();
                        ((Task)instance.TestAsync_Catch_WithTypedCatch(false)).ConfigureAwait(false).GetAwaiter().GetResult();

                        instance.Test_WithNestedCatch(true);
                        instance.Test_Catch_WithNestedCatch(true);
                        ((Task)instance.TestAsync_WithNestedCatch(true)).ConfigureAwait(false).GetAwaiter().GetResult();
                        ((Task)instance.TestAsync_Catch_WithNestedCatch(true)).ConfigureAwait(false).GetAwaiter().GetResult();

                        instance.Test_WithNestedCatch(false);
                        instance.Test_Catch_WithNestedCatch(false);
                        ((Task)instance.TestAsync_WithNestedCatch(false)).ConfigureAwait(false).GetAwaiter().GetResult();
                        ((Task)instance.TestAsync_Catch_WithNestedCatch(false)).ConfigureAwait(false).GetAwaiter().GetResult();

                        return Task.CompletedTask;
                    }, persistPrepareResultToFile: pathSerialize[0]);
                    return 0;
                }, new string[] { path });

                var res = TestInstrumentationHelper.GetCoverageResult(path);
                res.Document("Instrumentation.CatchBlock.cs")
                    .AssertLinesCoveredAllBut(BuildConfiguration.Debug, 45, 59, 113, 127, 137, 138, 139, 153, 154, 155, 156, 175, 189, 199, 200, 201, 222, 223, 224, 225, 252, 266, 335, 349)
                    .ExpectedTotalNumberOfBranches(BuildConfiguration.Debug, 6);
            }
            finally
            {
                File.Delete(path);
            }
        }
    }
}