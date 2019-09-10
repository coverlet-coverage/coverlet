using System;
using System.IO;
using System.Threading.Tasks;

using Coverlet.Core.Helpers;
using Coverlet.Core.Logging;
using Coverlet.Core.Samples.Tests;
using Coverlet.Tests.RemoteExecutor;
using Moq;
using Xunit;


namespace Coverlet.Core.Tests
{
    public class CoverageTests
    {
        private readonly InstrumentationHelper _instrumentationHelper = new InstrumentationHelper(new ProcessExitHandler(), new RetryHelper());
        private readonly Mock<ILogger> _mockLogger = new Mock<ILogger>();

        [Fact]
        public void TestCoverage()
        {
            string module = GetType().Assembly.Location;
            string pdb = Path.Combine(Path.GetDirectoryName(module), Path.GetFileNameWithoutExtension(module) + ".pdb");

            var directory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));

            File.Copy(module, Path.Combine(directory.FullName, Path.GetFileName(module)), true);
            File.Copy(pdb, Path.Combine(directory.FullName, Path.GetFileName(pdb)), true);

            // TODO: Find a way to mimick hits

            var coverage = new Coverage(Path.Combine(directory.FullName, Path.GetFileName(module)), Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(), false, false, string.Empty, false, _mockLogger.Object, _instrumentationHelper);
            coverage.PrepareModules();

            var result = coverage.GetCoverageResult();

            Assert.Empty(result.Modules);

            directory.Delete(true);
        }

        [Fact]
        public void TestCoverageWithTestAssembly()
        {
            string module = GetType().Assembly.Location;
            string pdb = Path.Combine(Path.GetDirectoryName(module), Path.GetFileNameWithoutExtension(module) + ".pdb");

            var directory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));

            File.Copy(module, Path.Combine(directory.FullName, Path.GetFileName(module)), true);
            File.Copy(pdb, Path.Combine(directory.FullName, Path.GetFileName(pdb)), true);

            // TODO: Find a way to mimick hits

            var coverage = new Coverage(Path.Combine(directory.FullName, Path.GetFileName(module)), Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(), true, false, string.Empty, false, _mockLogger.Object, _instrumentationHelper);
            coverage.PrepareModules();

            var result = coverage.GetCoverageResult();

            Assert.NotEmpty(result.Modules);

            directory.Delete(true);
        }

        [Fact]
        public void SelectionStatements_If()
        {
            // We need to pass file name to remote process where it save instrumentation result
            // Similar to msbuild input/output
            string path = Path.GetTempFileName();
            try
            {
                // Lambda will run in a custom process to avoid issue with statics and file locking
                RemoteExecutor.Invoke(async pathSerialize =>
                {
                    // Run load and call a delegate passing class as dynamic to simplify method call
                    CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<SelectionStatements>(instance =>
                    {
                        // We call method to trigger coverage hits
                        instance.If(true);

                        // For now we have only async Run helper
                        return Task.CompletedTask;
                    }, pathSerialize);

                    // we return 0 if we return something different assert fail
                    return 0;
                }, path).Dispose();

                // We retrive and load CoveragePrepareResult and run coverage calculation
                // Similar to msbuild coverage result task
                CoverageResult result = TestInstrumentationHelper.GetCoverageResult(path);

                // Asserts on doc/lines/branches
                result.Document("Instrumentation.SelectionStatements.cs")
                      // (line, hits)
                      .AssertLinesCovered((11, 1), (15, 0))
                      // (line,ordinal,hits)
                      .AssertBranchesCovered((9, 0, 1), (9, 1, 0));

                // if need to generate html report for debugging purpose
                // TestInstrumentationHelper.GenerateHtmlReport(result);
            }
            finally
            {
                // Cleanup tmp file
                File.Delete(path);
            }
        }

        [Fact]
        public void SelectionStatements_Switch()
        {
            string path = Path.GetTempFileName();
            try
            {
                RemoteExecutor.Invoke(async pathSerialize =>
                {
                    CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<SelectionStatements>(instance =>
                    {
                        instance.Switch(1);
                        return Task.CompletedTask;
                    }, pathSerialize);
                    return 0;
                }, path).Dispose();

                CoverageResult result = TestInstrumentationHelper.GetCoverageResult(path);

                result.Document("Instrumentation.SelectionStatements.cs")
                      .AssertLinesCovered(BuildConfiguration.Release, (24, 1), (26, 0), (28, 0))
                      .AssertBranchesCovered(BuildConfiguration.Release, (24, 1, 1))
                      .AssertLinesCovered(BuildConfiguration.Debug, (20, 1), (21, 1), (24, 1), (30, 1))
                      .AssertBranchesCovered(BuildConfiguration.Debug, (21, 0, 0), (21, 1, 1), (21, 2, 0), (21, 3, 0));
            }
            finally
            {
                File.Delete(path);
            }
        }
    }
}