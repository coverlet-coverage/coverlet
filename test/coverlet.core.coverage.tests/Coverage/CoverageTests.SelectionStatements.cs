// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Threading.Tasks;
using Coverlet.Core;
using Coverlet.Core.CoverageSamples.Tests;
using Coverlet.Core.Tests;
using Coverlet.Tests.Utils;
using Xunit;

namespace Coverlet.CoreCoverage.Tests
{
  public partial class CoverageTests : ExternalProcessExecutionTest
  {
    [Fact]
    public void SelectionStatements_If()
    {
      // We need to pass file name to remote process where it save instrumentation result
      // Similar to msbuild input/output
      string path = Path.GetTempFileName();
      try
      {
        // Lambda will run in a custom process to avoid issue with statics and file locking
        FunctionExecutor.Run(async (string[] pathSerialize) =>
        {
          // Run load and call a delegate passing class as dynamic to simplify method call
          CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<SelectionStatements>(instance =>
                  {
                    // We call method to trigger coverage hits
                    instance.If(true);

                    // For now we have only async Run helper
                    return Task.CompletedTask;
                  }, persistPrepareResultToFile: pathSerialize[0]);

          // we return 0 if we return something different assert fail
          return 0;
        }, [path]);

        // We retrieve and load CoveragePrepareResult and run coverage calculation
        // Similar to msbuild coverage result task
        CoverageResult result = TestInstrumentationHelper.GetCoverageResult(path);

        // Generate html report to check
        // TestInstrumentationHelper.GenerateHtmlReport(result);

        // Asserts on doc/lines/branches
        result.Document("Instrumentation.SelectionStatements.cs")
              // (line, hits)
              .AssertLinesCovered((11, 1), (15, 0))
              // (line,ordinal,hits)
              .AssertBranchesCovered((9, 0, 1), (9, 1, 0));
      }
      finally
      {
        // Cleanup tmp file
        File.Delete(path);
      }
    }

    [Fact]
    public void SelectionStatements_IfWithoutElse_OnlyTrueBranch()
    {
      // Test for issue #1786 using the current sample shape with a shared return statement
      // When only the true path runs, the assignment line and the shared continuation are both hit
      string path = Path.GetTempFileName();
      try
      {
        FunctionExecutor.Run(async (string[] pathSerialize) =>
        {
          CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<SelectionStatements>(instance =>
                  {
                    // Execute only the true path
                    instance.IfWithoutElse(true);
                    return Task.CompletedTask;
                  }, persistPrepareResultToFile: pathSerialize[0]);
          return 0;
        }, [path]);

        CoverageResult result = TestInstrumentationHelper.GetCoverageResult(path);

        // Generate html report to check
        // TestInstrumentationHelper.GenerateHtmlReport(result);

        // Current sample:
        // int ret = 0;
        // if (condition)
        // {
        //   ret = 1;
        // }
        // return ret;
        //
        // The shared return line is part of the continuation path, so it is reached even after the true path.
        result.Document("Instrumentation.SelectionStatements.cs")
              // Line 45 initializes ret, line 48 assigns 1, line 51 returns the shared continuation value
              .AssertLinesCovered((45, 1), (48, 1), (51, 1))
              // Branch at line 46: ordinal 0 (fall-through into true body) is hit; ordinal 1 (skip path
              // taken when condition=false) is NOT hit because we only ran the true path.
              .AssertBranchesCovered((46, 0, 1), (46, 1, 0));
      }
      finally
      {
        File.Delete(path);
      }
    }

    [Fact]
    public void SelectionStatements_IfWithoutElse_OnlyFalseBranch()
    {
      // Verify the false path skips the assignment and reaches the shared return
      string path = Path.GetTempFileName();
      try
      {
        FunctionExecutor.Run(async (string[] pathSerialize) =>
        {
          CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<SelectionStatements>(instance =>
                  {
                    // Execute only the false path
                    instance.IfWithoutElse(false);
                    return Task.CompletedTask;
                  }, persistPrepareResultToFile: pathSerialize[0]);
          return 0;
        }, [path]);

        CoverageResult result = TestInstrumentationHelper.GetCoverageResult(path);

        // Expected behavior for the current sample: initialization and shared return are hit, assignment is not
        result.Document("Instrumentation.SelectionStatements.cs")
              // Line 45 initializes ret, line 48 is skipped, line 51 returns the default value
              .AssertLinesCovered((45, 1), (48, 0), (51, 1))
              // Branch at line 46: ordinal 0 (true body) not hit, ordinal 1 (shared continuation) hit
              .AssertBranchesCovered((46, 0, 0), (46, 1, 1));
      }
      finally
      {
        File.Delete(path);
      }
    }

    [Fact]
    public void SelectionStatements_IfWithoutElse_BothBranches()
    {
      // Verify the current sample hits the true body once and the shared continuation on both executions
      string path = Path.GetTempFileName();
      try
      {
        FunctionExecutor.Run(async (string[] pathSerialize) =>
        {
          CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<SelectionStatements>(instance =>
                  {
                    // Execute both paths
                    instance.IfWithoutElse(true);
                    instance.IfWithoutElse(false);
                    return Task.CompletedTask;
                  }, persistPrepareResultToFile: pathSerialize[0]);
          return 0;
        }, [path]);

        CoverageResult result = TestInstrumentationHelper.GetCoverageResult(path);

        // Expected behavior for the current sample shape with a shared continuation
        result.Document("Instrumentation.SelectionStatements.cs")
              .AssertLinesCovered((45, 2), (48, 1), (51, 2))
              // The true body (ordinal 0) runs once for condition=true;
              // the skip path (ordinal 1) runs once for condition=false.
              .AssertBranchesCovered((46, 0, 1), (46, 1, 1));
      }
      finally
      {
        File.Delete(path);
      }
    }

    [Fact]
    public void SelectionStatements_Switch()
    {
      string path = Path.GetTempFileName();
      try
      {
        FunctionExecutor.Run(async (string[] pathSerialize) =>
        {
          CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<SelectionStatements>(instance =>
                  {
                    instance.Switch(1);
                    return Task.CompletedTask;
                  }, persistPrepareResultToFile: pathSerialize[0]);
          return 0;
        }, [path]);

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

    [Fact]
    public void SelectionStatements_Switch_CSharp8_OneBranch()
    {
      string path = Path.GetTempFileName();
      try
      {
        FunctionExecutor.Run(async (string[] pathSerialize) =>
        {
          CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<SelectionStatements>(instance =>
                  {
                    instance.SwitchCsharp8(int.MaxValue);
                    return Task.CompletedTask;
                  }, persistPrepareResultToFile: pathSerialize[0]);
          return 0;
        }, [path]);

        TestInstrumentationHelper.GetCoverageResult(path)
        .Document("Instrumentation.SelectionStatements.cs")
        .AssertLinesCovered(BuildConfiguration.Debug, 33, 34, 35, 36, 40)
        .AssertLinesNotCovered(BuildConfiguration.Debug, 37, 38, 39)
        .AssertBranchesCovered(BuildConfiguration.Debug, (34, 0, 1), (34, 1, 0), (34, 2, 0), (34, 3, 0), (34, 4, 0), (34, 5, 0))
        .ExpectedTotalNumberOfBranches(4);
      }
      finally
      {
        File.Delete(path);
      }
    }

    [Fact]
    public void SelectionStatements_Switch_CSharp8_AllBranches()
    {
      string path = Path.GetTempFileName();
      try
      {
        FunctionExecutor.Run(async (string[] pathSerialize) =>
        {
          CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<SelectionStatements>(instance =>
                  {
                    instance.SwitchCsharp8(int.MaxValue);
                    instance.SwitchCsharp8(uint.MaxValue);
                    instance.SwitchCsharp8(short.MaxValue);
                    try
                    {
                      instance.SwitchCsharp8("");
                    }
                    catch { }
                    return Task.CompletedTask;
                  }, persistPrepareResultToFile: pathSerialize[0]);
          return 0;
        }, [path]);

        TestInstrumentationHelper.GetCoverageResult(path)
        .Document("Instrumentation.SelectionStatements.cs")
        .AssertLinesCovered(BuildConfiguration.Debug, 33, 34, 35, 36, 37, 38, 39, 40)
        .AssertBranchesCovered(BuildConfiguration.Debug, (34, 0, 1), (34, 1, 3), (34, 2, 1), (34, 3, 2), (34, 4, 1), (34, 5, 1))
        .ExpectedTotalNumberOfBranches(4);
      }
      finally
      {
        File.Delete(path);
      }
    }
  }
}
