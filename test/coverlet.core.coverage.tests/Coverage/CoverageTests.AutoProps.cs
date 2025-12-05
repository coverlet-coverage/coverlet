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
  public partial class CoverageTests
  {
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SkipClassWithAutoProps(bool skipAutoProps)
    {
      string path = Path.GetTempFileName();
      try
      {
        FunctionExecutor.Run(async (string[] parameters) =>
        {
          CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<ClassWithAutoProps>(instance =>
                  {
                    instance.AutoPropsNonInit = 10;
                    instance.AutoPropsInit = 20;
                    int readValue = instance.AutoPropsNonInit;
                    readValue = instance.AutoPropsInit;
                    readValue = instance.AutoPropsInitKeyword;
                    return Task.CompletedTask;
                  },
                  persistPrepareResultToFile: parameters[0], skipAutoProps: bool.Parse(parameters[1]));

          return 0;
        }, [path, skipAutoProps.ToString()]);

        if (skipAutoProps)
        {
          TestInstrumentationHelper.GetCoverageResult(path)
              .Document("Instrumentation.AutoProps.cs")
              .AssertNonInstrumentedLines(BuildConfiguration.Debug, 12, 14)
              .AssertNonInstrumentedLines(BuildConfiguration.Release, 12, 14)
              .AssertLinesCoveredFromTo(BuildConfiguration.Debug, 9, 11)
              .AssertLinesCovered(BuildConfiguration.Debug, (7, 1))
              .AssertLinesCovered(BuildConfiguration.Release, (10, 1));
        }
        else
        {
          TestInstrumentationHelper.GetCoverageResult(path)
              .Document("Instrumentation.AutoProps.cs")
              .AssertLinesCoveredFromTo(BuildConfiguration.Debug, 7, 14)
              .AssertLinesCoveredFromTo(BuildConfiguration.Release, 10, 10)
              .AssertLinesCoveredFromTo(BuildConfiguration.Release, 12, 14);
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
    public void SkipClassWithAutoPropsPrimaryConstructor(bool skipAutoProps)
    {
      string path = Path.GetTempFileName();
      try
      {
        FunctionExecutor.Run(async (string[] parameters) =>
        {
          CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<ClassWithAutoPropsPrimaryConstructor>(instance =>
            {
              return Task.CompletedTask;
            },
            persistPrepareResultToFile: parameters[0], skipAutoProps: bool.Parse(parameters[1]));

          return 0;
        }, [path, skipAutoProps.ToString()]);

        if (skipAutoProps)
        {
          TestInstrumentationHelper.GetCoverageResult(path)
            .Document("Instrumentation.AutoProps.cs")
            .AssertLinesCoveredFromTo(BuildConfiguration.Debug, 28, 28)
            .AssertLinesCoveredFromTo(BuildConfiguration.Release, 28, 28)
            .AssertNonInstrumentedLines(BuildConfiguration.Debug, 30, 31, 32)
            .AssertNonInstrumentedLines(BuildConfiguration.Release, 30, 31, 32);
        }
        else
        {
          TestInstrumentationHelper.GetCoverageResult(path)
            .Document("Instrumentation.AutoProps.cs")
            .AssertLinesCoveredFromTo(BuildConfiguration.Debug, 28, 28)
            .AssertLinesCoveredFromTo(BuildConfiguration.Debug, 30, 32)
            .AssertLinesCoveredFromTo(BuildConfiguration.Release, 28, 28)
            .AssertLinesCoveredFromTo(BuildConfiguration.Release, 30, 32);
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
    public void SkipRecordWithAutoProps(bool skipAutoProps)
    {
      string path = Path.GetTempFileName();
      try
      {
        FunctionExecutor.Run(async (string[] parameters) =>
        {
          CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<RecordWithAutoProps>(instance =>
            {
              instance.AutoPropsNonInit = 10;
              instance.AutoPropsInit = 20;
              int readValue = instance.AutoPropsNonInit;
              readValue = instance.AutoPropsInit;
              readValue = instance.AutoPropsInitKeyword;
              return Task.CompletedTask;
            },
            persistPrepareResultToFile: parameters[0], skipAutoProps: bool.Parse(parameters[1]));

          return 0;
        }, [path, skipAutoProps.ToString()]);

        if (skipAutoProps)
        {
          TestInstrumentationHelper.GetCoverageResult(path)
            .Document("Instrumentation.AutoProps.cs")
            .AssertNonInstrumentedLines(BuildConfiguration.Debug, 43, 45)
            .AssertNonInstrumentedLines(BuildConfiguration.Release, 43, 45)
            .AssertLinesCoveredFromTo(BuildConfiguration.Debug, 40, 42)
            .AssertLinesCovered(BuildConfiguration.Debug, (39, 1))
            .AssertLinesCovered(BuildConfiguration.Release, (39, 1));
        }
        else
        {
          TestInstrumentationHelper.GetCoverageResult(path)
            .Document("Instrumentation.AutoProps.cs")
            .AssertLinesCoveredFromTo(BuildConfiguration.Debug, 38, 45)  // go on here
            .AssertLinesCoveredFromTo(BuildConfiguration.Release, 39, 39)
            .AssertLinesCoveredFromTo(BuildConfiguration.Release, 41, 45);
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
    public void SkipRecordWithAutoPropsPrimaryConstructor(bool skipAutoProps)
    {
      string path = Path.GetTempFileName();
      try
      {
        FunctionExecutor.Run(async (string[] parameters) =>
        {
          CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<RecordsWithPrimaryConstructor>(instance =>
            {
              return Task.CompletedTask;
            },
            persistPrepareResultToFile: parameters[0], skipAutoProps: bool.Parse(parameters[1]));

          return 0;
        }, [path, skipAutoProps.ToString()]);

        if (skipAutoProps)
        {
          TestInstrumentationHelper.GetCoverageResult(path)
            .Document("Instrumentation.AutoProps.cs")
            .AssertLinesCoveredFromTo(BuildConfiguration.Debug, 50, 50)
            .AssertLinesCoveredFromTo(BuildConfiguration.Release, 50, 50);
        }
        else
        {
          TestInstrumentationHelper.GetCoverageResult(path)
            .Document("Instrumentation.AutoProps.cs")
            .AssertLinesCoveredFromTo(BuildConfiguration.Debug, 50, 50)
            .AssertLinesCoveredFromTo(BuildConfiguration.Release, 50, 50);
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
    public void SkipRecordWithAutoPropsPrimaryConstructorMultiline(bool skipAutoProps)
    {
      string path = Path.GetTempFileName();
      try
      {
        FunctionExecutor.Run(async (string[] parameters) =>
        {
          CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<RecordsWithPrimaryConstructor>(instance =>
            {
              return Task.CompletedTask;
            },
            persistPrepareResultToFile: parameters[0], skipAutoProps: bool.Parse(parameters[1]));

          return 0;
        }, [path, skipAutoProps.ToString()]);

        if (skipAutoProps)
        {
          TestInstrumentationHelper.GetCoverageResult(path)
            .Document("Instrumentation.AutoProps.cs")
            .AssertLinesCoveredFromTo(BuildConfiguration.Debug, 52, 55)
            .AssertLinesCoveredFromTo(BuildConfiguration.Debug, 52, 55);
        }
        else
        {
          TestInstrumentationHelper.GetCoverageResult(path)
            .Document("Instrumentation.AutoProps.cs")
            .AssertLinesCovered(BuildConfiguration.Debug, 52, 55)
            .AssertLinesNotCovered(BuildConfiguration.Debug, 53, 54)
            .AssertLinesCovered(BuildConfiguration.Release, 52, 55)
            .AssertLinesNotCovered(BuildConfiguration.Release, 53, 54);
        }
      }
      finally
      {
        File.Delete(path);
      }
    }
  }
}
