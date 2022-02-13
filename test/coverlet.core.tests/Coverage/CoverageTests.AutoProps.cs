using System.IO;
using System.Threading.Tasks;
using Coverlet.Core.Samples.Tests;
using Xunit;

namespace Coverlet.Core.Tests
{
    public partial class CoverageTests
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void SkipAutoProps(bool skipAutoProps)
        {
            string path = Path.GetTempFileName();
            try
            {
                FunctionExecutor.Run(async (string[] parameters) =>
                {
                    CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<AutoProps>(instance =>
                    {
                        instance.AutoPropsNonInit = 10;
                        instance.AutoPropsInit = 20;
                        int readValue = instance.AutoPropsNonInit;
                        readValue = instance.AutoPropsInit;
                        return Task.CompletedTask;
                    },
                    persistPrepareResultToFile: parameters[0], skipAutoProps: bool.Parse(parameters[1]));

                    return 0;
                }, new string[] { path, skipAutoProps.ToString() });

                if (skipAutoProps)
                {
                    TestInstrumentationHelper.GetCoverageResult(path)
                        .Document("Instrumentation.AutoProps.cs")
                        .AssertNonInstrumentedLines(BuildConfiguration.Debug, 12, 13)
                        .AssertNonInstrumentedLines(BuildConfiguration.Release, 12, 13)
                        .AssertLinesCoveredFromTo(BuildConfiguration.Debug, 9, 11)
                        .AssertLinesCovered(BuildConfiguration.Debug, (7, 1))
                        .AssertLinesCovered(BuildConfiguration.Release, (10, 1));
                }
                else
                {
                    TestInstrumentationHelper.GetCoverageResult(path)
                        .Document("Instrumentation.AutoProps.cs")
                        .AssertLinesCoveredFromTo(BuildConfiguration.Debug, 7, 13)
                        .AssertLinesCoveredFromTo(BuildConfiguration.Release, 10, 10)
                        .AssertLinesCoveredFromTo(BuildConfiguration.Release, 12, 13);
                }
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void SkipAutoPropsInRecords(bool skipAutoProps)
        {
            string path = Path.GetTempFileName();
            try
            {
                FunctionExecutor.Run(async (string[] parameters) =>
                {
                    CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<RecordWithPropertyInit>(instance =>
                        {
                            instance.RecordAutoPropsNonInit = string.Empty;
                            instance.RecordAutoPropsInit = string.Empty;
                            string readValue = instance.RecordAutoPropsInit;
                            readValue = instance.RecordAutoPropsNonInit;
                            return Task.CompletedTask;
                        },
                    persistPrepareResultToFile: parameters[0], skipAutoProps: bool.Parse(parameters[1]));

                    return 0;
                }, new string[] { path, skipAutoProps.ToString() });

                if (skipAutoProps)
                {
                    TestInstrumentationHelper.GetCoverageResult(path).GenerateReport(show: true)
                        .Document("Instrumentation.AutoProps.cs")
                        .AssertNonInstrumentedLines(BuildConfiguration.Debug, 23, 24)
                        .AssertNonInstrumentedLines(BuildConfiguration.Release, 23, 24)
                        .AssertLinesCovered(BuildConfiguration.Debug, (18, 1), (20, 1), (21, 1), (22, 1))
                        .AssertLinesCovered(BuildConfiguration.Release, (21, 1));
                }
                else
                {
                    TestInstrumentationHelper.GetCoverageResult(path)
                        .Document("Instrumentation.AutoProps.cs")
                        .AssertLinesCoveredFromTo(BuildConfiguration.Debug, 18, 24)
                        .AssertLinesCoveredFromTo(BuildConfiguration.Release, 21, 21)
                        .AssertLinesCoveredFromTo(BuildConfiguration.Release, 23, 24);
                }
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void SkipRecordWithProperties(bool skipAutoProps)
        {
            string path = Path.GetTempFileName();
            try
            {
                FunctionExecutor.Run(async (string[] parameters) =>
                {
                    CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<ClassWithAutoRecordProperties>(instance =>
                        {
                            return Task.CompletedTask;
                        },
                        persistPrepareResultToFile: parameters[0], skipAutoProps: bool.Parse(parameters[1]));

                    return 0;
                }, new string[] { path, skipAutoProps.ToString() });

                if (skipAutoProps)
                {
                    TestInstrumentationHelper.GetCoverageResult(path)
                        .Document("Instrumentation.AutoProps.cs")
                        .AssertNonInstrumentedLines(BuildConfiguration.Debug, 29, 29)
                        .AssertNonInstrumentedLines(BuildConfiguration.Release, 29, 29)
                        .AssertLinesCovered(BuildConfiguration.Debug, (32, 1), (33, 1), (34, 1))
                        .AssertLinesCovered(BuildConfiguration.Release, (33, 1));

                }
                else
                {
                    TestInstrumentationHelper.GetCoverageResult(path)
                        .Document("Instrumentation.AutoProps.cs")
                        .AssertLinesCovered(BuildConfiguration.Debug, (29, 3), (31, 1), (32, 1), (33, 1), (34, 1))
                        .AssertLinesCovered(BuildConfiguration.Release, (29, 3), (31, 1), (33, 1));
                }
            }
            finally
            {
                File.Delete(path);
            }
        }
    }
}
