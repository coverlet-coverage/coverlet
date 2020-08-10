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
                .AssertLinesCoveredAllBut(BuildConfiguration.Debug, 23, 51)
                .AssertBranchesCovered(BuildConfiguration.Debug,
                // Expected branches
                (22, 0, 0),
                (22, 1, 1),
                (50, 0, 0),
                (50, 1, 1)
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
                .AssertLinesCovered(BuildConfiguration.Debug, (72, 1), (73, 1), (74, 101), (75, 1), (76, 1))
                .ExpectedTotalNumberOfBranches(BuildConfiguration.Debug, 0);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void EmptyLine_Issue_799_01()
        {
            string path = Path.GetTempFileName();
            try
            {
                FunctionExecutor.Run(async (string[] pathSerialize) =>
                {
                    CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<Issue_799_BodyStartsOneLineAfterSequencePoint>(instance => 
                        {
                            instance.Run();
                            return Task.CompletedTask;
                        }, 
                        persistPrepareResultToFile: pathSerialize[0]);
                    return 0;
                }, new string[] { path });

                TestInstrumentationHelper.GetCoverageResult(path)
                .Document("Instrumentation.Lambda.cs")
                .AssertLinesCovered(BuildConfiguration.Debug, (83, 1), (84, 1), (85, 1), (86, 1), (88, 1), (89, 1), (90, 1))
                .AssertLinesCoveredAllBut(BuildConfiguration.Debug, 87)
                .ExpectedTotalNumberOfBranches(BuildConfiguration.Debug, 0);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void EmptyLine_Issue_799_02()
        {
            string path = Path.GetTempFileName();
            try
            {
                FunctionExecutor.Run(async (string[] pathSerialize) =>
                {
                    CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<Issue_799_BodyStartsOnSameLineAsSequencePoint>(instance =>
                        {
                            instance.Run();
                            return Task.CompletedTask;
                        },
                        persistPrepareResultToFile: pathSerialize[0]);
                    return 0;
                }, new string[] { path });

                TestInstrumentationHelper.GetCoverageResult(path)
                .Document("Instrumentation.Lambda.cs")
                .AssertLinesCovered(BuildConfiguration.Debug, (96, 1), (97, 1), (98, 1), (100, 1), (101, 1), (102, 1))
                .AssertLinesCoveredAllBut(BuildConfiguration.Debug, 99)
                .ExpectedTotalNumberOfBranches(BuildConfiguration.Debug, 0);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void EmptyLine_Issue_799_03()
        {
            string path = Path.GetTempFileName();
            try
            {
                FunctionExecutor.Run(async (string[] pathSerialize) =>
                {
                    CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<Issue_799_BodyStartsTwoLinesAfterSequencePoint>(instance =>
                        {
                            instance.Run();
                            return Task.CompletedTask;
                        },
                        persistPrepareResultToFile: pathSerialize[0]);
                    return 0;
                }, new string[] { path });

                TestInstrumentationHelper.GetCoverageResult(path)
                    .Document("Instrumentation.Lambda.cs")
                    .AssertLinesCovered(BuildConfiguration.Debug, (108, 1), (109, 1), (110, 1), (111, 1), (112, 1), (114, 1), (115, 1), (116, 1))
                    .AssertLinesCoveredAllBut(BuildConfiguration.Debug, 113)
                    .ExpectedTotalNumberOfBranches(BuildConfiguration.Debug, 0);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void EmptyLine_Issue_799_04()
        {
            string path = Path.GetTempFileName();
            try
            {
                FunctionExecutor.Run(async (string[] pathSerialize) =>
                {
                    CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<Issue_799_SequencePointEndsOneLineAfterBody>(instance =>
                        {
                            instance.Run();
                            return Task.CompletedTask;
                        },
                        persistPrepareResultToFile: pathSerialize[0]);
                    return 0;
                }, new string[] { path });

                TestInstrumentationHelper.GetCoverageResult(path)
                    .Document("Instrumentation.Lambda.cs")
                    .AssertLinesCovered(BuildConfiguration.Debug, (122, 1), (123, 1), (124, 1), (125, 1), (127, 1), (128, 1), (129, 1), (130, 1))
                    .AssertLinesCoveredAllBut(BuildConfiguration.Debug, 126)
                    .ExpectedTotalNumberOfBranches(BuildConfiguration.Debug, 0);
            }
            finally
            {
                File.Delete(path);
            }
        }
    }
}