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
    /// <summary>
    /// Regression test for issue #1633: records without primary constructor showed no coverage.
    /// Verifies that both "record Foo { }" and "record Foo() { }" are correctly instrumented
    /// and their methods show as covered when called.
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void RecordWithoutPrimaryConstructorIsCovered(bool skipAutoProps)
    {
      string path = Path.GetTempFileName();
      try
      {
        FunctionExecutor.Run(async (string[] parameters) =>
        {
          CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<ClassWithRecordsNoPrimaryConstructor>(instance =>
            {
              return Task.CompletedTask;
            },
            persistPrepareResultToFile: parameters[0], skipAutoProps: bool.Parse(parameters[1]));

          return 0;
        }, [path, skipAutoProps.ToString()]);

        // Lines 55 and 61: "Bar()" method bodies in RecordNoCtor and RecordEmptyCtor
        // Lines 66-67: constructor body calling both Bar() methods
        TestInstrumentationHelper.GetCoverageResult(path)
          .Document("Instrumentation.AutoProps.cs")
          .AssertLinesCovered(BuildConfiguration.Debug, (55, 1), (61, 1), (66, 1), (67, 1))
          .AssertLinesCovered(BuildConfiguration.Release, (55, 1), (61, 1));
      }
      finally
      {
        File.Delete(path);
      }
    }

    /// <summary>
    /// Regression test for issue #1633: abstract records with and without a primary constructor
    /// and their concrete subrecords are correctly instrumented.
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AbstractRecordWithoutPrimaryConstructorIsCovered(bool skipAutoProps)
    {
      string path = Path.GetTempFileName();
      try
      {
        FunctionExecutor.Run(async (string[] parameters) =>
        {
          CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<ClassWithAbstractRecordsNoPrimaryConstructor>(instance =>
            {
              return Task.CompletedTask;
            },
            persistPrepareResultToFile: parameters[0], skipAutoProps: bool.Parse(parameters[1]));

          return 0;
        }, [path, skipAutoProps.ToString()]);

        // Lines 85, 90: GetValue() bodies in ConcreteFromBase and ConcreteFromBaseCtor
        // Lines 95-96: constructor body calling GetValue() on both
        TestInstrumentationHelper.GetCoverageResult(path)
          .Document("Instrumentation.AutoProps.cs")
          .AssertLinesCovered(BuildConfiguration.Debug, (85, 1), (90, 1), (95, 1), (96, 1))
          .AssertLinesCovered(BuildConfiguration.Release, (85, 1), (90, 1));
      }
      finally
      {
        File.Delete(path);
      }
    }
  }
}
