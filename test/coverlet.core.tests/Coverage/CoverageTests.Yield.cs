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
                        foreach (var _ in instance.One()) ;

                        return Task.CompletedTask;
                    }, persistPrepareResultToFile: pathSerialize[0]);

                    return 0;
                }, new string[] { path });

                CoverageResult result = TestInstrumentationHelper.GetCoverageResult(path);

                result.Document("Instrumentation.Yield.cs")
                      .Method("System.Boolean Coverlet.Core.Samples.Tests.Yield/<One>d__0::MoveNext()")
                      .AssertLinesCovered((9, 1))
                      .ExpectedTotalNumberOfBranches(0);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void Yield_Two()
        {
            string path = Path.GetTempFileName();
            try
            {
                FunctionExecutor.Run(async (string[] pathSerialize) =>
                {
                    CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<Yield>(instance =>
                    {
                        foreach (var _ in instance.Two()) ;

                        return Task.CompletedTask;
                    }, persistPrepareResultToFile: pathSerialize[0]);
                    return 0;
                }, new string[] { path });

                CoverageResult result = TestInstrumentationHelper.GetCoverageResult(path);

                result.Document("Instrumentation.Yield.cs")
                      .Method("System.Boolean Coverlet.Core.Samples.Tests.Yield/<Two>d__1::MoveNext()")
                      .AssertLinesCovered((14, 1), (15, 1))
                      .ExpectedTotalNumberOfBranches(0);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void Yield_SingleWithSwitch()
        {
            string path = Path.GetTempFileName();
            try
            {
                FunctionExecutor.Run(async (string[] pathSerialize) =>
                {
                    CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<Yield>(instance =>
                    {
                        foreach (var _ in instance.OneWithSwitch(2)) ;

                        return Task.CompletedTask;
                    }, persistPrepareResultToFile: pathSerialize[0]);

                    return 0;
                }, new string[] { path });

                CoverageResult result = TestInstrumentationHelper.GetCoverageResult(path);

                result.Document("Instrumentation.Yield.cs")
                      .Method("System.Boolean Coverlet.Core.Samples.Tests.Yield/<OneWithSwitch>d__2::MoveNext()")
                      .AssertLinesCovered(BuildConfiguration.Debug, (21, 1), (30, 1), (31, 1), (37, 1))
                      .ExpectedTotalNumberOfBranches(1);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void Yield_Three()
        {
            string path = Path.GetTempFileName();
            try
            {
                FunctionExecutor.Run(async (string[] pathSerialize) =>
                {
                    CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<Yield>(instance =>
                    {
                        foreach (var _ in instance.Three()) ;

                        return Task.CompletedTask;
                    }, persistPrepareResultToFile: pathSerialize[0]);
                    return 0;
                }, new string[] { path });

                CoverageResult result = TestInstrumentationHelper.GetCoverageResult(path);

                result.Document("Instrumentation.Yield.cs")
                      .Method("System.Boolean Coverlet.Core.Samples.Tests.Yield/<Three>d__3::MoveNext()")
                      .AssertLinesCovered((42, 1), (43, 1), (44, 1))
                      .ExpectedTotalNumberOfBranches(0);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void Yield_Enumerable()
        {
            string path = Path.GetTempFileName();
            try
            {
                FunctionExecutor.Run(async (string[] pathSerialize) =>
                {
                    CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<Yield>(instance =>
                    {
                        foreach (var _ in instance.Enumerable(new[] { "one", "two", "three", "four" })) ;

                        return Task.CompletedTask;
                    }, persistPrepareResultToFile: pathSerialize[0]);
                    return 0;
                }, new string[] { path });

                CoverageResult result = TestInstrumentationHelper.GetCoverageResult(path);

                result.Document("Instrumentation.Yield.cs")
                      .Method("System.Boolean Coverlet.Core.Samples.Tests.Yield/<Enumerable>d__4::MoveNext()")
                      .AssertLinesCovered(BuildConfiguration.Debug, (48, 1), (49, 1), (50, 4), (51, 5), (52, 1), (54, 4), (55, 4), (56, 4), (57, 1))
                      .ExpectedTotalNumberOfBranches(1);
            }
            finally
            {
                File.Delete(path);
            }
        }
    }
}
