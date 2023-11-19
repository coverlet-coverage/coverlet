// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Coverlet.Core.Abstractions;
using Coverlet.Core.Helpers;
using Coverlet.Core.Samples.Tests;
using Coverlet.Core.Symbols;
using Moq;
using Xunit;

namespace Coverlet.Core.Tests
{
  public partial class CoverageTests
  {
    [Fact]
    public void TestCoverageSkipModule__AssemblyMarkedAsExcludeFromCodeCoverage()
    {
      var partialMockFileSystem = new Mock<FileSystem>();
      partialMockFileSystem.CallBase = true;
      partialMockFileSystem.Setup(fs => fs.NewFileStream(It.IsAny<string>(), It.IsAny<FileMode>(), It.IsAny<FileAccess>())).Returns((string path, FileMode mode, FileAccess access) =>
      {
        return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
      });
      var loggerMock = new Mock<ILogger>();

      string excludedbyattributeDll = Directory.GetFiles(Path.Combine(Directory.GetCurrentDirectory(), "TestAssets"), "coverlet.tests.projectsample.excludedbyattribute.dll").First();

      var instrumentationHelper =
          new InstrumentationHelper(new ProcessExitHandler(), new RetryHelper(), new FileSystem(), new Mock<ILogger>().Object,
                                    new SourceRootTranslator(excludedbyattributeDll, new Mock<ILogger>().Object, new FileSystem(), new AssemblyAdapter()));

      var parameters = new CoverageParameters
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
          CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<MethodsWithExcludeFromCodeCoverageAttr>(async instance =>
                  {
              await (Task<int>)instance.Test("test");
                  }, persistPrepareResultToFile: pathSerialize[0]);

          return 0;

        }, new string[] { path });

        CoverageResult result = TestInstrumentationHelper.GetCoverageResult(path);

        Core.Instrumentation.Document document = result.Document("Instrumentation.ExcludeFromCoverage.cs");

        int[] coveredLines = document.Lines.Where(x =>
            x.Value.Class == "Coverlet.Core.Samples.Tests.MethodsWithExcludeFromCodeCoverageAttr" ||
            x.Value.Class.StartsWith("Coverlet.Core.Samples.Tests.MethodsWithExcludeFromCodeCoverageAttr/"))
          .Select(x => x.Value.Number).ToArray();

        int[] notCoveredLines = document.Lines.Where(x =>
            x.Value.Class == "Coverlet.Core.Samples.Tests.MethodsWithExcludeFromCodeCoverageAttr2" ||
            x.Value.Class.StartsWith("Coverlet.Core.Samples.Tests.MethodsWithExcludeFromCodeCoverageAttr2/"))
          .Select(x => x.Value.Number).ToArray();

        document.AssertLinesCovered(BuildConfiguration.Debug, coveredLines);
        document.AssertLinesNotCovered(BuildConfiguration.Debug, notCoveredLines);
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

        CoverageResult result = TestInstrumentationHelper.GetCoverageResult(path)
          .GenerateReport(show: true);

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
        .GenerateReport(show: true)
        .Document("Instrumentation.ExcludeFromCoverage.cs")
        .AssertLinesCovered(BuildConfiguration.Debug, (148, 1))
        .AssertNonInstrumentedLines(BuildConfiguration.Debug, 153, 163);
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
          CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<TaskRepo_Issue809>(async instance =>
                  {
              Assert.True(await (Task<bool>)instance.EditTask(null, 10));
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

    [Fact]
    public void ExcludeFromCodeCoverageAutoGeneratedGetSet()
    {
      string path = Path.GetTempFileName();
      try
      {
        FunctionExecutor.Run(async (string[] pathSerialize) =>
        {
          CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<AutoGeneneratedGetSet>(instance =>
              {
                instance.SetId(10);
                Assert.Equal(10, instance.Id);
                return Task.CompletedTask;
              }, persistPrepareResultToFile: pathSerialize[0]);

          return 0;
        }, new string[] { path });

        TestInstrumentationHelper.GetCoverageResult(path)
        .Document("Instrumentation.ExcludeFromCoverage.cs")
        .AssertNonInstrumentedLines(BuildConfiguration.Debug, 170)
        .AssertLinesCovered(BuildConfiguration.Debug, 172);
      }
      finally
      {
        File.Delete(path);
      }
    }

    [Fact]
    public void ExcludeFromCodeCoverageAutoGeneratedGet()
    {
      string path = Path.GetTempFileName();
      try
      {
        FunctionExecutor.Run(async (string[] pathSerialize) =>
        {
          CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<AutoGeneneratedGetOnly>(instance =>
                  {
                    instance.SetId(10);
                    Assert.Equal(10, instance.Id);
                    return Task.CompletedTask;
                  }, persistPrepareResultToFile: pathSerialize[0]);

          return 0;
        }, new string[] { path });

        TestInstrumentationHelper.GetCoverageResult(path)
        .Document("Instrumentation.ExcludeFromCoverage.cs")
        .AssertNonInstrumentedLines(BuildConfiguration.Debug, 180)
        .AssertLinesCovered(BuildConfiguration.Debug, 181, 184);
      }
      finally
      {
        File.Delete(path);
      }
    }

    [Fact]
    public void ExcludeFromCodeCoverage_Issue1302()
    {
      string path = Path.GetTempFileName();
      try
      {
        FunctionExecutor.Run(async (string[] pathSerialize) =>
        {
          CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<Issue1302>(instance =>
                  {
              instance.Run();
              return Task.CompletedTask;
            }, persistPrepareResultToFile: pathSerialize[0]);

          return 0;
        }, new string[] { path });

        TestInstrumentationHelper.GetCoverageResult(path)
            .Document("Instrumentation.ExcludeFromCoverage.Issue1302.cs")
            .AssertNonInstrumentedLines(BuildConfiguration.Debug, 10, 13);
      }
      finally
      {
        File.Delete(path);
      }
    }

    [Fact]
    public void MethodsWithExcludeFromCodeCoverageAttr()
    {
      string path = Path.GetTempFileName();
      try
      {
        FunctionExecutor.Run(async (string[] pathSerialize) =>
        {
          CoveragePrepareResult coveragePrepareResult =
            await TestInstrumentationHelper.Run<MethodsWithExcludeFromCodeCoverageAttr>(async instance =>
              {
                instance.TestLambda(string.Empty);
                instance.TestLambda(string.Empty, 1);
                foreach (dynamic _ in instance.TestYield("abc")) ;
                foreach (dynamic _ in instance.TestYield("abc", 1)) ;
                instance.TestLocalFunction(string.Empty);
                instance.TestLocalFunction(string.Empty, 1);
                await (Task)instance.TestAsyncAwait();
                await (Task)instance.TestAsyncAwait(1);
              },
              persistPrepareResultToFile: pathSerialize[0]);

          return 0;
        }, new string[] { path });

        TestInstrumentationHelper.GetCoverageResult(path)
          .GenerateReport(show: true)
          .Document("Instrumentation.ExcludeFromCoverage.cs")
          .AssertNonInstrumentedLines(BuildConfiguration.Debug, 15, 16, 28, 29, 30, 31, 45, 56, 58, 59, 60, 61)
          .AssertLinesCovered(BuildConfiguration.Debug, 21, 22, 36, 37, 38, 39, 50, 66, 69, 70, 71);
      }
      finally
      {
        File.Delete(path);
      }
    }

    [Fact]
    public void MethodsWithExcludeFromCodeCoverageAttr2()
    {
      string path = Path.GetTempFileName();
      try
      {
        FunctionExecutor.Run(async (string[] pathSerialize) =>
        {
          CoveragePrepareResult coveragePrepareResult =
            await TestInstrumentationHelper.Run<MethodsWithExcludeFromCodeCoverageAttr2>(async instance =>
              {
                instance.TestLambda(string.Empty);
                instance.TestLambda(string.Empty, 1);
                foreach (dynamic _ in instance.TestYield("abc")) ;
                foreach (dynamic _ in instance.TestYield("abc", 1)) ;
                instance.TestLocalFunction(string.Empty);
                instance.TestLocalFunction(string.Empty, 1);
                await (Task)instance.TestAsyncAwait();
                await (Task)instance.TestAsyncAwait(1);
              },
              persistPrepareResultToFile: pathSerialize[0]);

          return 0;
        }, new string[] { path });

        TestInstrumentationHelper.GetCoverageResult(path)
          .GenerateReport(show: true)
          .Document("Instrumentation.ExcludeFromCoverage.cs")
          .AssertNonInstrumentedLines(BuildConfiguration.Debug, 92, 93, 107, 108, 109, 110, 121, 137, 140, 141, 142)
          .AssertLinesCovered(BuildConfiguration.Debug, 85, 86, 98, 99, 100, 101, 115, 126, 129, 130, 131);
      }
      finally
      {
        File.Delete(path);
      }
    }
  }
}
