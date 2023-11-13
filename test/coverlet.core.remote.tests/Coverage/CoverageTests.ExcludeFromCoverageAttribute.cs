// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Threading.Tasks;
using Coverlet.Core.Samples.Tests;
using Xunit;

namespace Coverlet.Core.Tests
{
  public partial class CoverageTests
  {
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
        .GenerateReport(show: true)
        .Document("Instrumentation.ExcludeFromCoverage.cs")
        .AssertLinesCovered(BuildConfiguration.Debug, (145, 1))
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
          CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<TaskRepo_Issue809>(async instance =>
          {
            Assert.True(await ((Task<bool>)instance.EditTask(null, 10)));
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
        .AssertNonInstrumentedLines(BuildConfiguration.Debug, 167)
        .AssertLinesCovered(BuildConfiguration.Debug, 169);
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
        .AssertNonInstrumentedLines(BuildConfiguration.Debug, 177)
        .AssertLinesCovered(BuildConfiguration.Debug, 178, 181);
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
  }
}
