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
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SkipAutoProps(bool skipAutoProps)
    {
      string path = Path.GetTempFileName();
      try
      {
        RemoteInvokeHandle h = RemoteExecutor.Invoke(async (string arg0, string arg1) =>
        {
          string[] parameters = new string[] { arg0, arg1 };
          CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<AutoProps>(instance =>
                  {
                    instance.AutoPropsNonInit = 10;
                    instance.AutoPropsInit = 20;
                    int readValue = instance.AutoPropsNonInit;
                    readValue = instance.AutoPropsInit;
                    return Task.CompletedTask;
                  },
                  persistPrepareResultToFile: parameters[0], skipAutoProps: bool.Parse(parameters[1]));

          }, path, skipAutoProps.ToString() );
        using (h)
        {
          Assert.Equal(RemoteExecutor.SuccessExitCode, h.ExitCode);
        }

        if (skipAutoProps)
        {
          TestInstrumentationHelper.GetCoverageResult(path)
              .Document("Instrumentation.AutoProps.cs")
              .AssertNonInstrumentedLines(BuildConfiguration.Debug, 12, 13)
              .AssertNonInstrumentedLines(BuildConfiguration.Release, 12, 13)
              .AssertLinesCoveredFromTo(BuildConfiguration.Debug, 9, 11)
              .AssertLinesCovered(BuildConfiguration.Debug, (7, 1))
              .AssertLinesCovered(BuildConfiguration.Release, (10, 1));
        }
        else
        {
          TestInstrumentationHelper.GetCoverageResult(path)
              .Document("Instrumentation.AutoProps.cs")
              .AssertLinesCoveredFromTo(BuildConfiguration.Debug, 7, 13)
              .AssertLinesCoveredFromTo(BuildConfiguration.Release, 10, 10)
              .AssertLinesCoveredFromTo(BuildConfiguration.Release, 12, 13);
        }
      }
      finally
      {
        File.Delete(path);
      }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SkipAutoPropsInRecords(bool skipAutoProps)
    {
      string path = Path.GetTempFileName();
      try
      {
        RemoteInvokeHandle h = RemoteExecutor.Invoke(async (string arg0, string arg1) =>
        {
          string[] parameters = new string[] { arg0 , arg1};
          CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<RecordWithPropertyInit>(instance =>
                      {
                        instance.RecordAutoPropsNonInit = string.Empty;
                        instance.RecordAutoPropsInit = string.Empty;
                        string readValue = instance.RecordAutoPropsInit;
                        readValue = instance.RecordAutoPropsNonInit;
                        return Task.CompletedTask;
                      },
                  persistPrepareResultToFile: parameters[0], skipAutoProps: bool.Parse(parameters[1]));

        }, path, skipAutoProps.ToString() );
        using (h)
        {
          Assert.Equal(RemoteExecutor.SuccessExitCode, h.ExitCode);
        }

        if (skipAutoProps)
        {
          TestInstrumentationHelper.GetCoverageResult(path)
              .Document("Instrumentation.AutoProps.cs")
              .AssertNonInstrumentedLines(BuildConfiguration.Debug, 23, 24)
              .AssertNonInstrumentedLines(BuildConfiguration.Release, 23, 24)
              .AssertLinesCovered(BuildConfiguration.Debug, (18, 1), (20, 1), (21, 1), (22, 1))
              .AssertLinesCovered(BuildConfiguration.Release, (21, 1));
        }
        else
        {
          TestInstrumentationHelper.GetCoverageResult(path)
              .Document("Instrumentation.AutoProps.cs")
              .AssertLinesCoveredFromTo(BuildConfiguration.Debug, 18, 24)
              .AssertLinesCoveredFromTo(BuildConfiguration.Release, 21, 21)
              .AssertLinesCoveredFromTo(BuildConfiguration.Release, 23, 24);
        }
      }
      finally
      {
        File.Delete(path);
      }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SkipRecordWithProperties(bool skipAutoProps)
    {
      string path = Path.GetTempFileName();
      try
      {
        RemoteInvokeHandle h = RemoteExecutor.Invoke(async (string arg0, string arg1) =>
        {
          string[] parameters = new string[] { arg0, arg1};
          CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<RecordWithPropertyInit>(instance =>
          {
            return Task.CompletedTask;
          },
          persistPrepareResultToFile: parameters[0], skipAutoProps: bool.Parse(parameters[1]));

        }, path, skipAutoProps.ToString() );
        using (h)
        {
          Assert.Equal(RemoteExecutor.SuccessExitCode, h.ExitCode);
        }

        if (skipAutoProps)
        {
          TestInstrumentationHelper.GetCoverageResult(path)
              .Document("Instrumentation.AutoProps.cs")
              .AssertNonInstrumentedLines(BuildConfiguration.Debug, 23, 24)
              .AssertNonInstrumentedLines(BuildConfiguration.Release, 23, 24)
              .AssertLinesCovered(BuildConfiguration.Debug, (18, 1), (20, 1), (21, 1), (22, 1))
              .AssertLinesCovered(BuildConfiguration.Release, (21, 1));
        }
        else
        {
          TestInstrumentationHelper.GetCoverageResult(path)
              .Document("Instrumentation.AutoProps.cs")
              //.AssertLinesCoveredFromTo(BuildConfiguration.Debug, 18, 24) ????
              .AssertLinesCovered(BuildConfiguration.Debug, (18, 1), (19, 1), (20, 1), (21, 1), (22, 1), (24, 1))
              .AssertLinesCoveredFromTo(BuildConfiguration.Release, 21, 21)
              .AssertLinesCoveredFromTo(BuildConfiguration.Release, 23, 24);
        }
      }
      finally
      {
        File.Delete(path);
      }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SkipInheritingRecordsWithProperties(bool skipAutoProps)
    {
      string path = Path.GetTempFileName();
      try
      {
        RemoteInvokeHandle h = RemoteExecutor.Invoke(async (string arg0, string arg1) =>
        {
          string[] parameters = new string[] { arg0, arg1 };
          CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<ClassWithInheritingRecordsAndAutoProperties>(instance =>
            {
              return Task.CompletedTask;
            },
            persistPrepareResultToFile: parameters[0], skipAutoProps: bool.Parse(parameters[1]));
        }, path, skipAutoProps.ToString() );
        using (h)
        {
          Assert.Equal(RemoteExecutor.SuccessExitCode, h.ExitCode);
        }
        if (skipAutoProps)
        {
          TestInstrumentationHelper.GetCoverageResult(path)
            .Document("Instrumentation.AutoProps.cs")
            .AssertNonInstrumentedLines(BuildConfiguration.Debug, 39, 39)
            .AssertNonInstrumentedLines(BuildConfiguration.Release, 39, 39)
            .AssertLinesCovered(BuildConfiguration.Debug, (41, 1), (44, 1), (45, 1), (46, 1))
            .AssertLinesCovered(BuildConfiguration.Release, (45, 1));

        }
        else
        {
          TestInstrumentationHelper.GetCoverageResult(path)
            .Document("Instrumentation.AutoProps.cs")
            .AssertLinesCovered(BuildConfiguration.Debug, (39, 1), (41, 1), (44, 1), (45, 1), (46, 1))
            .AssertLinesCovered(BuildConfiguration.Release, (39, 1), (41, 1), (45, 1));
        }
      }
      finally
      {
        File.Delete(path);
      }
    }
  }
}
