using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using Coverlet.Core.Abstractions;
using Coverlet.Core.Helpers;
using Coverlet.Core.Samples.Tests;
using Coverlet.Core.Symbols;
using Coverlet.Tests.Xunit.Extensions;
using Moq;
using Xunit;

namespace Coverlet.Core.Tests
{
    public partial class CoverageTests
    {
        [Fact]
        public void TestCoverageSkipModule__AssemblyMarkedAsExcludeFromCodeCoverage()
        {
            Mock<FileSystem> partialMockFileSystem = new Mock<FileSystem>();
            partialMockFileSystem.CallBase = true;
            partialMockFileSystem.Setup(fs => fs.NewFileStream(It.IsAny<string>(), It.IsAny<FileMode>(), It.IsAny<FileAccess>())).Returns((string path, FileMode mode, FileAccess access) =>
            {
                return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            });
            var loggerMock = new Mock<ILogger>();

            string excludedbyattributeDll = Directory.GetFiles(Path.Combine(Directory.GetCurrentDirectory(), "TestAssets"), "coverlet.tests.projectsample.excludedbyattribute.dll").First();

            InstrumentationHelper instrumentationHelper =
                new InstrumentationHelper(new ProcessExitHandler(), new RetryHelper(), new FileSystem(), new Mock<ILogger>().Object,
                                          new SourceRootTranslator(excludedbyattributeDll, new Mock<ILogger>().Object, new FileSystem()));

            CoverageParameters parameters = new CoverageParameters
            {
                IncludeFilters = new string[] { "[coverlet.tests.projectsample.excludedbyattribute*]*" },
                IncludeDirectories = Array.Empty<string>(),
                ExcludeFilters = Array.Empty<string>(),
                ExcludedSourceFiles = Array.Empty<string>(),
                ExcludeAttributes = Array.Empty<string>(),
                IncludeTestAssembly = true,
                SingleHit = false,
                MergeWith = string.Empty,
                UseSourceLink = false
            };

            // test skip module include test assembly feature
            var coverage = new Coverage(excludedbyattributeDll, parameters, loggerMock.Object, instrumentationHelper, partialMockFileSystem.Object,
                                        new SourceRootTranslator(loggerMock.Object, new FileSystem()), new CecilSymbolHelper());
            CoveragePrepareResult result = coverage.PrepareModules();
            Assert.Empty(result.Results);
            loggerMock.Verify(l => l.LogVerbose(It.IsAny<string>()));
        }

        [Fact]
        public void ExcludeFromCodeCoverage_CompilerGeneratedMethodsAndTypes()
        {
            string path = Path.GetTempFileName();
            try
            {
                FunctionExecutor.Run(async (string[] pathSerialize) =>
                {
                    CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<MethodsWithExcludeFromCodeCoverageAttr>(instance =>
                    {
                        ((Task<int>)instance.Test("test")).ConfigureAwait(false).GetAwaiter().GetResult();
                        return Task.CompletedTask;
                    }, persistPrepareResultToFile: pathSerialize[0]);

                    return 0;

                }, new string[] { path });

                CoverageResult result = TestInstrumentationHelper.GetCoverageResult(path);

                var document = result.Document("Instrumentation.ExcludeFromCoverage.cs");

                // Invoking method "Test" of class "MethodsWithExcludeFromCodeCoverageAttr" we expect to cover 100% lines for MethodsWithExcludeFromCodeCoverageAttr 
                Assert.DoesNotContain(document.Lines, l =>
                    (l.Value.Class == "Coverlet.Core.Samples.Tests.MethodsWithExcludeFromCodeCoverageAttr" ||
                    // Compiler generated
                    l.Value.Class.StartsWith("Coverlet.Core.Samples.Tests.MethodsWithExcludeFromCodeCoverageAttr/")) &&
                    l.Value.Hits == 0);
                // and 0% for MethodsWithExcludeFromCodeCoverageAttr2
                Assert.DoesNotContain(document.Lines, l =>
                    (l.Value.Class == "Coverlet.Core.Samples.Tests.MethodsWithExcludeFromCodeCoverageAttr2" ||
                    // Compiler generated
                    l.Value.Class.StartsWith("Coverlet.Core.Samples.Tests.MethodsWithExcludeFromCodeCoverageAttr2/")) &&
                    l.Value.Hits == 1);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void ExcludeFromCodeCoverage_CompilerGeneratedMethodsAndTypes_NestedMembers()
        {
            string path = Path.GetTempFileName();
            try
            {
                FunctionExecutor.Run(async (string[] pathSerialize) =>
                {
                    CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<MethodsWithExcludeFromCodeCoverageAttr_NestedStateMachines>(instance =>
                    {
                        instance.Test();
                        return Task.CompletedTask;
                    }, persistPrepareResultToFile: pathSerialize[0]);

                    return 0;

                }, new string[] { path });

                CoverageResult result = TestInstrumentationHelper.GetCoverageResult(path);

                result.Document("Instrumentation.ExcludeFromCoverage.NestedStateMachines.cs")
                        .AssertLinesCovered(BuildConfiguration.Debug, (14, 1), (15, 1), (16, 1))
                        .AssertNonInstrumentedLines(BuildConfiguration.Debug, 9, 11);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void ExcludeFromCodeCoverageCompilerGeneratedMethodsAndTypes_Issue670()
        {
            string path = Path.GetTempFileName();
            try
            {
                FunctionExecutor.Run(async (string[] pathSerialize) =>
                {
                    CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<MethodsWithExcludeFromCodeCoverageAttr_Issue670>(instance =>
                    {
                        instance.Test("test");
                        return Task.CompletedTask;
                    }, persistPrepareResultToFile: pathSerialize[0]);

                    return 0;

                }, new string[] { path });

                CoverageResult result = TestInstrumentationHelper.GetCoverageResult(path);

                result.Document("Instrumentation.ExcludeFromCoverage.Issue670.cs")
                        .AssertLinesCovered(BuildConfiguration.Debug, (8, 1), (9, 1), (10, 1), (11, 1))
                        .AssertNonInstrumentedLines(BuildConfiguration.Debug, 15, 53);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void ExcludeFromCodeCoverageNextedTypes()
        {
            string path = Path.GetTempFileName();
            try
            {
                FunctionExecutor.Run(async (string[] pathSerialize) =>
                {
                    CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<ExcludeFromCoverageAttrFilterClass1>(instance =>
                    {
                        Assert.Equal(42, instance.Run());
                        return Task.CompletedTask;
                    }, persistPrepareResultToFile: pathSerialize[0]);

                    return 0;
                }, new string[] { path });

                TestInstrumentationHelper.GetCoverageResult(path)
                .Document("Instrumentation.ExcludeFromCoverage.cs")
                .AssertLinesCovered(BuildConfiguration.Debug, (143, 1))
                .AssertNonInstrumentedLines(BuildConfiguration.Debug, 146, 160);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void ExcludeFromCodeCoverage_Issue809()
        {
            string path = Path.GetTempFileName();
            try
            {
                FunctionExecutor.Run(async (string[] pathSerialize) =>
                {
                    CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<TaskRepo_Issue809>(instance =>
                    {
                        Assert.True(((Task<bool>)instance.EditTask(null, 10)).GetAwaiter().GetResult());
                        return Task.CompletedTask;
                    }, persistPrepareResultToFile: pathSerialize[0]);

                    return 0;
                }, new string[] { path });

                TestInstrumentationHelper.GetCoverageResult(path)
                .Document("Instrumentation.ExcludeFromCoverage.Issue809.cs")

                // public async Task<bool> EditTask(Tasks_Issue809 tasks, int val)
                .AssertNonInstrumentedLines(BuildConfiguration.Debug, 153, 162)
                // .AssertNonInstrumentedLines(BuildConfiguration.Debug, 167, 170) -> Shoud be not covered, issue with lambda
                .AssertNonInstrumentedLines(BuildConfiguration.Debug, 167, 197)

                // public List<Tasks_Issue809> GetAllTasks()
                // .AssertNonInstrumentedLines(BuildConfiguration.Debug, 263, 266) -> Shoud be not covered, issue with lambda
                .AssertNonInstrumentedLines(BuildConfiguration.Debug, 263, 264);
                // .AssertNonInstrumentedLines(BuildConfiguration.Debug, 269, 275) -> Shoud be not covered, issue with lambda
            }
            finally
            {
                File.Delete(path);
            }
        }
    }
}