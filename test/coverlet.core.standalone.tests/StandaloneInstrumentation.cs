using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

using Coverlet.Core.Abstracts;
using Coverlet.Core.Instrumentation;
using Coverlet.Core.ObjectModel;
using Coverlet.Tests.RemoteExecutor;
using Xunit;

namespace Coverlet.Core.Standalone.Tests
{
    public class StandaloneInstrumentation : BaseTest
    {
        readonly ICoverageEngineFactory _coverageFactory = new BuildInCoverageEngineFactory();

        [Fact]
        async public Task Instrument_Run_GetResult()
        {
            // Copy file to instrument to avoid lock
            string fileToInstrument = GetLibToInstrument();

            // Create engine
            InstrumentationOptions options = new InstrumentationOptions();
            options.Module = fileToInstrument;
            options.IncludeTestAssembly = true;
            options.IncludeFilters = new string[] { $"[{Path.GetFileNameWithoutExtension(options.Module)}*]*" };
            ICoverageEngine coverageEngine = _coverageFactory.CreateEngine(options);

            // Instrument modules
            Stream instrumentationResult = coverageEngine.PrepareModules();
            // Persist stream
            string instrumentationResultPath = Path.GetTempFileName();
            using (var fs = File.OpenWrite(instrumentationResultPath))
            {
                await instrumentationResult.CopyToAsync(fs);
            }
            instrumentationResult.Seek(0, SeekOrigin.Begin);

            // Run in new process
            RemoteExecutor.Invoke(assets =>
            {
                string[] assetsPath = assets.Split("|");
                ICoverageEngineFactory coverageFactory = new BuildInCoverageEngineFactory();

                // We need to pass instrumentation result to engine
                IInProcessCoverageEngine inProcessEngine = coverageFactory.CreateInProcessEngine(File.OpenRead(assetsPath[1]));

                // Use object
                var sampleWrapper = new SampleWrapper(assetsPath[0]);

                // Get app domain loaded instrumented assemblies
                Assembly[] assemblies = inProcessEngine.GetInstrumentedAssemblies();
                Assert.NotEmpty(assemblies);

                Assert.Equal(3, sampleWrapper.Add(1, 2));

                // Read current coverage state
                CoverageResult result = inProcessEngine.ReadCurrentCoverage();
                AssertCoverage(result, 1);

                Assert.Equal(3, sampleWrapper.Add(1, 2));

                // Read current coverage state
                result = inProcessEngine.ReadCurrentCoverage();
                AssertCoverage(result, 2);

                return Task.FromResult(0);
            }, $"{fileToInstrument}|{instrumentationResultPath}").Dispose();

            // Get results
            CoverageResult result = coverageEngine.GetCoverageResult(instrumentationResult);
            AssertCoverage(result, 2);

            // Create report
            IReporter reporter = _coverageFactory.CreateReporter(Reporters.Cobertura);
            string reportResult = reporter.Report(result);
            Assert.NotEmpty(reportResult);

            return;

            // Assertion helper
            static void AssertCoverage(CoverageResult result, int coverageValue)
            {
                foreach (var module in result.Modules)
                {
                    foreach (var document in module.Value)
                    {
                        foreach (var @class in document.Value)
                        {
                            foreach (var method in @class.Value)
                            {
                                if (method.Key == "System.Int32 Coverlet.Core.Standalone.Sample.Calculator::Add(System.Int32,System.Int32)")
                                {
                                    foreach (var line in method.Value.Lines)
                                    {
                                        Assert.Equal(coverageValue, line.Value);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}