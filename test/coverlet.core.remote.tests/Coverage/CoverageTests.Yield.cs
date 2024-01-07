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
    public void Yield_Single()
    {
      string path = Path.GetTempFileName();
      try
      {
        RemoteInvokeHandle h = RemoteExecutor.Invoke(async (string arg0) =>
        {
          string[] pathSerialize = [arg0];
          CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<Yield>(instance =>
                  {
                    foreach (dynamic _ in instance.One()) ;

                    return Task.CompletedTask;
                  }, persistPrepareResultToFile: pathSerialize[0]);

        }, path );
        using (h)
        {
          Assert.Equal(RemoteExecutor.SuccessExitCode, h.ExitCode);
        }

        CoverageResult result = TestInstrumentationHelper.GetCoverageResult(path);

        result.Document("Instrumentation.Yield.cs")
              .Method("System.Boolean coverlet.core.remote.samples.tests.Yield/<One>d__0::MoveNext()")
              .AssertLinesCovered((9, 1))
              .ExpectedTotalNumberOfBranches(0);
      }
      finally
      {
        File.Delete(path);
      }
    }

    [Fact]
    public void Yield_Two()
    {
      string path = Path.GetTempFileName();
      try
      {
        RemoteInvokeHandle h = RemoteExecutor.Invoke(async (string arg0) =>
        {
          string[] pathSerialize = [arg0];
          CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<Yield>(instance =>
                  {
                    foreach (dynamic _ in instance.Two()) ;

                    return Task.CompletedTask;
                  }, persistPrepareResultToFile: pathSerialize[0]);
        }, path, new RemoteInvokeOptions { RollForward = "Major" });
        using (h)
        {
          Assert.Equal(RemoteExecutor.SuccessExitCode, h.ExitCode);
        }

        CoverageResult result = TestInstrumentationHelper.GetCoverageResult(path);

        result.Document("Instrumentation.Yield.cs")
              .Method("System.Boolean coverlet.core.remote.samples.tests.Yield/<Two>d__1::MoveNext()")
              .AssertLinesCovered((14, 1), (15, 1))
              .ExpectedTotalNumberOfBranches(0);
      }
      finally
      {
        File.Delete(path);
      }
    }

    [Fact]
    public void Yield_SingleWithSwitch()
    {
      string path = Path.GetTempFileName();
      try
      {
        RemoteInvokeHandle h = RemoteExecutor.Invoke(async (string arg0) =>
        {
          string[] pathSerialize = [arg0];
          CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<Yield>(instance =>
                  {
                    foreach (dynamic _ in instance.OneWithSwitch(2)) ;

                    return Task.CompletedTask;
                  }, persistPrepareResultToFile: pathSerialize[0]);

        }, path, new RemoteInvokeOptions { RollForward = "Major" });
        using (h)
        {
          Assert.Equal(RemoteExecutor.SuccessExitCode, h.ExitCode);
        }

        CoverageResult result = TestInstrumentationHelper.GetCoverageResult(path);

        result.Document("Instrumentation.Yield.cs")
              .Method("System.Boolean coverlet.core.remote.samples.tests.Yield/<OneWithSwitch>d__2::MoveNext()")
              .AssertLinesCovered(BuildConfiguration.Debug, (21, 1), (30, 1), (31, 1), (37, 1))
              .ExpectedTotalNumberOfBranches(1);
      }
      finally
      {
        File.Delete(path);
      }
    }

    [Fact]
    public void Yield_Three()
    {
      string path = Path.GetTempFileName();
      try
      {
        RemoteInvokeHandle h = RemoteExecutor.Invoke(async (string arg0) =>
        {
          string[] pathSerialize = [arg0];
          CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<Yield>(instance =>
                  {
                    foreach (dynamic _ in instance.Three()) ;

                    return Task.CompletedTask;
                  }, persistPrepareResultToFile: pathSerialize[0]);
        }, path, new RemoteInvokeOptions { RollForward = "Major" });
        using (h)
        {
          Assert.Equal(RemoteExecutor.SuccessExitCode, h.ExitCode);
        }

        CoverageResult result = TestInstrumentationHelper.GetCoverageResult(path);

        result.Document("Instrumentation.Yield.cs")
              .Method("System.Boolean coverlet.core.remote.samples.tests.Yield/<Three>d__3::MoveNext()")
              .AssertLinesCovered((42, 1), (43, 1), (44, 1))
              .ExpectedTotalNumberOfBranches(0);
      }
      finally
      {
        File.Delete(path);
      }
    }

    [Fact]
    public void Yield_Enumerable()
    {
      string path = Path.GetTempFileName();
      try
      {
        RemoteInvokeHandle h = RemoteExecutor.Invoke(async (string arg0) =>
        {
          string[] pathSerialize = [arg0];
          CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<Yield>(instance =>
                  {
                    foreach (dynamic _ in instance.Enumerable(new[] { "one", "two", "three", "four" })) ;

                    return Task.CompletedTask;
                  }, persistPrepareResultToFile: pathSerialize[0]);
        }, path );
        using (h)
        {
          Assert.Equal(RemoteExecutor.SuccessExitCode, h.ExitCode);
        }

        CoverageResult result = TestInstrumentationHelper.GetCoverageResult(path);

        result.Document("Instrumentation.Yield.cs")
              .Method("System.Boolean coverlet.core.remote.samples.tests.Yield/<Enumerable>d__4::MoveNext()")
              .AssertLinesCovered(BuildConfiguration.Debug, (48, 1), (49, 1), (50, 4), (51, 5), (52, 1), (54, 4), (55, 4), (56, 4), (57, 1))
              .ExpectedTotalNumberOfBranches(1);
      }
      finally
      {
        File.Delete(path);
      }
    }
  }
}
