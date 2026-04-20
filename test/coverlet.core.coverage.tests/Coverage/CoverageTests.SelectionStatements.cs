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
      // Test for issue #1786: if without else reports incorrect branch coverage
      // When only the true branch runs, branch coverage should be 50% (1 of 2 branches)
      string path = Path.GetTempFileName();
      try
      {
        FunctionExecutor.Run(async (string[] pathSerialize) =>
        {
          CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<SelectionStatements>(instance =>
                  {
                    // Only execute true branch
                    instance.IfWithoutElse(true);
                    return Task.CompletedTask;
                  }, persistPrepareResultToFile: pathSerialize[0]);
          return 0;
        }, [path]);

        CoverageResult result = TestInstrumentationHelper.GetCoverageResult(path);

        // Generate html report to check
        // TestInstrumentationHelper.GenerateHtmlReport(result);

        // Expected behavior: 2 branches exist, only 1 should be covered
        result.Document("Instrumentation.SelectionStatements.cs")
              // Line 47 (return 1) should be hit, line 50 (return 0) should NOT be hit
              .AssertLinesCovered((47, 1), (50, 0))
              // Branch at line 45: ordinal 0 (true path) hit, ordinal 1 (false path) NOT hit
              .AssertBranchesCovered((45, 0, 1), (45, 1, 0));
      }
      finally
      {
        File.Delete(path);
      }
    }

    [Fact]
    public void SelectionStatements_IfWithoutElse_OnlyFalseBranch()
    {
      // Verify the false branch also works correctly
      string path = Path.GetTempFileName();
      try
      {
        FunctionExecutor.Run(async (string[] pathSerialize) =>
        {
          CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<SelectionStatements>(instance =>
                  {
                    // Only execute false branch (skip the if block)
                    instance.IfWithoutElse(false);
                    return Task.CompletedTask;
                  }, persistPrepareResultToFile: pathSerialize[0]);
          return 0;
        }, [path]);

        CoverageResult result = TestInstrumentationHelper.GetCoverageResult(path);

        // Expected behavior: 2 branches exist, only the false branch covered
        result.Document("Instrumentation.SelectionStatements.cs")
              // Line 47 (return 1) should NOT be hit, line 50 (return 0) should be hit
              .AssertLinesCovered((47, 0), (50, 1))
              // Branch at line 45: ordinal 0 (true path) NOT hit, ordinal 1 (false path) hit
              .AssertBranchesCovered((45, 0, 0), (45, 1, 1));
      }
      finally
      {
        File.Delete(path);
      }
    }

    [Fact]
    public void SelectionStatements_IfWithoutElse_BothBranches()
    {
      // Verify 100% coverage when both branches are executed
      string path = Path.GetTempFileName();
      try
      {
        FunctionExecutor.Run(async (string[] pathSerialize) =>
        {
          CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<SelectionStatements>(instance =>
                  {
                    // Execute both branches
                    instance.IfWithoutElse(true);
                    instance.IfWithoutElse(false);
                    return Task.CompletedTask;
                  }, persistPrepareResultToFile: pathSerialize[0]);
          return 0;
        }, [path]);

        CoverageResult result = TestInstrumentationHelper.GetCoverageResult(path);

        // Expected behavior: 100% branch coverage
        result.Document("Instrumentation.SelectionStatements.cs")
              .AssertLinesCovered((47, 1), (50, 1))
              // Both branches covered
              .AssertBranchesCovered((45, 0, 1), (45, 1, 1));
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
