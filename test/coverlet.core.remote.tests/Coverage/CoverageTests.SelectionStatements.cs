// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Threading.Tasks;
using coverlet.core.remote.samples.tests;
using Coverlet.Core;
using Coverlet.Tests.Utils;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace coverlet.core.remote.tests
{
  public partial class CoverageTests 
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
        RemoteInvokeHandle h = RemoteExecutor.Invoke(async (string arg0) =>
        {
          string[] pathSerialize = new string[] { arg0 };
           // Run load and call a delegate passing class as dynamic to simplify method call
           CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<SelectionStatements>(instance =>
          {
            // We call method to trigger coverage hits
            instance.If(true);

            // For now we have only async Run helper
            return Task.CompletedTask;
          }, persistPrepareResultToFile: pathSerialize[0]);
        },  path );
        using (h)
        {
          Assert.Equal(RemoteExecutor.SuccessExitCode, h.ExitCode);
        }

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
    public void SelectionStatements_Switch()
    {
      string path = Path.GetTempFileName();
      try
      {
        RemoteInvokeHandle h = RemoteExecutor.Invoke(async (string arg0) =>
        {
          string[] pathSerialize = new string[] { arg0 };
          CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<SelectionStatements>(instance =>
                  {
                    instance.Switch(1);
                    return Task.CompletedTask;
                  }, persistPrepareResultToFile: pathSerialize[0]);
        },  path );
        using (h)
        {
          Assert.Equal(RemoteExecutor.SuccessExitCode, h.ExitCode);
        }

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
        RemoteInvokeHandle h = RemoteExecutor.Invoke(async (string arg0) =>
        {
          string[] pathSerialize = new string[] { arg0 };
          CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<SelectionStatements>(instance =>
                  {
                    instance.SwitchCsharp8(int.MaxValue);
                    return Task.CompletedTask;
                  }, persistPrepareResultToFile: pathSerialize[0]);
        },  path );
        using (h)
        {
          Assert.Equal(RemoteExecutor.SuccessExitCode, h.ExitCode);
        }

        TestInstrumentationHelper.GetCoverageResult(path)
        .Document("Instrumentation.SelectionStatements.cs")
        .AssertLinesCovered(BuildConfiguration.Debug, 33, 34, 35, 36, 40)
        .AssertLinesNotCovered(BuildConfiguration.Debug, 37, 38, 39)
        .AssertBranchesCovered(BuildConfiguration.Debug, (34, 0, 1), (34, 1, 0), (34, 2, 0), (34, 3, 0), (34, 4, 0), (34, 5, 0))
        .ExpectedTotalNumberOfBranches(3);
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
        RemoteInvokeHandle h = RemoteExecutor.Invoke(async (string arg0) =>
        {
          string[] pathSerialize = new string[] { arg0 };
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
        },  path );
        using (h)
        {
          Assert.Equal(RemoteExecutor.SuccessExitCode, h.ExitCode);
        }

        TestInstrumentationHelper.GetCoverageResult(path)
        .Document("Instrumentation.SelectionStatements.cs")
        .AssertLinesCovered(BuildConfiguration.Debug, 33, 34, 35, 36, 37, 38, 39, 40)
        .AssertBranchesCovered(BuildConfiguration.Debug, (34, 0, 1), (34, 1, 3), (34, 2, 1), (34, 3, 2), (34, 4, 1), (34, 5, 1))
        .ExpectedTotalNumberOfBranches(3);
      }
      finally
      {
        File.Delete(path);
      }
    }
  }
}
