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
                        int readVal = instance.AutoPropsNonInit;
                        readVal = instance.AutoPropsInit;
                        return Task.CompletedTask;
                    },
                    persistPrepareResultToFile: parameters[0], skipAutoProps: bool.Parse(parameters[1]));

                    return 0;
                }, new string[] { path, skipAutoProps.ToString() });

                if (skipAutoProps)
                {
                    TestInstrumentationHelper.GetCoverageResult(path)
                    .Document("Instrumentation.AutoProps.cs")
                    .AssertNonInstrumentedLines(BuildConfiguration.Debug, 12, 12)
                    .AssertLinesCoveredFromTo(BuildConfiguration.Debug, 7, 11)
                    .AssertLinesCovered(BuildConfiguration.Debug, (13, 1));
                }
                else
                {
                    TestInstrumentationHelper.GetCoverageResult(path)
                    .Document("Instrumentation.AutoProps.cs")
                    .AssertLinesCoveredFromTo(BuildConfiguration.Debug, 7, 13);
                }
            }
            finally
            {
                File.Delete(path);
            }
        }
    }
}
