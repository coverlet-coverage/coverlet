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
    public async Task Yield_SingleAsync()
    {
      string path = Path.GetTempFileName();
      try
      {
        CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<Yield>(instance =>
                {
                  foreach (dynamic _ in instance.One()) ;

                  return Task.CompletedTask;
                }, persistPrepareResultToFile: path);

        CoverageResult result = TestInstrumentationHelper.GetCoverageResult(path);

        result.Document("Instrumentation.Yield.cs")
              .Method("System.Boolean Coverlet.Core.CoverageSamples.Tests.Yield/<One>d__0::MoveNext()")
              .AssertLinesCovered((9, 1))
              .ExpectedTotalNumberOfBranches(0);
      }
      finally
      {
        File.Delete(path);
      }
    }

    [Fact]
    public async Task Yield_TwoAsync()
    {
      string path = Path.GetTempFileName();
      try
      {
        CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<Yield>(instance =>
                {
                  foreach (dynamic _ in instance.Two()) ;

                  return Task.CompletedTask;
                }, persistPrepareResultToFile: path);

        CoverageResult result = TestInstrumentationHelper.GetCoverageResult(path);

        result.Document("Instrumentation.Yield.cs")
              .Method("System.Boolean Coverlet.Core.CoverageSamples.Tests.Yield/<Two>d__1::MoveNext()")
              .AssertLinesCovered((14, 1), (15, 1))
              .ExpectedTotalNumberOfBranches(0);
      }
      finally
      {
        File.Delete(path);
      }
    }

    [Fact]
    public async Task Yield_SingleWithSwitchAsync()
    {
      string path = Path.GetTempFileName();
      try
      {
        CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<Yield>(instance =>
                {
                  foreach (dynamic _ in instance.OneWithSwitch(2)) ;

                  return Task.CompletedTask;
                }, persistPrepareResultToFile: path);

        CoverageResult result = TestInstrumentationHelper.GetCoverageResult(path);

        result.Document("Instrumentation.Yield.cs")
              .Method("System.Boolean Coverlet.Core.CoverageSamples.Tests.Yield/<OneWithSwitch>d__2::MoveNext()")
              .AssertLinesCovered(BuildConfiguration.Debug, (21, 1), (30, 1), (31, 1), (37, 1))
              .ExpectedTotalNumberOfBranches(1);
      }
      finally
      {
        File.Delete(path);
      }
    }

    [Fact]
    public async Task Yield_ThreeAsync()
    {
      string path = Path.GetTempFileName();
      try
      {
        CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<Yield>(instance =>
                {
                  foreach (dynamic _ in instance.Three()) ;

                  return Task.CompletedTask;
                }, persistPrepareResultToFile: path);

        CoverageResult result = TestInstrumentationHelper.GetCoverageResult(path);

        result.Document("Instrumentation.Yield.cs")
              .Method("System.Boolean Coverlet.Core.CoverageSamples.Tests.Yield/<Three>d__3::MoveNext()")
              .AssertLinesCovered((42, 1), (43, 1), (44, 1))
              .ExpectedTotalNumberOfBranches(0);
      }
      finally
      {
        File.Delete(path);
      }
    }

    private static readonly string[] s_stringArray = ["one", "two", "three", "four"];

    [Fact]
    public async Task Yield_EnumerableAsync()
    {
      string path = Path.GetTempFileName();
      try
      {
        CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<Yield>(instance =>
                {
                  foreach (dynamic _ in instance.Enumerable(s_stringArray)) ;

                  return Task.CompletedTask;
                }, persistPrepareResultToFile: path);

        CoverageResult result = TestInstrumentationHelper.GetCoverageResult(path);

        result.Document("Instrumentation.Yield.cs")
              .Method("System.Boolean Coverlet.Core.CoverageSamples.Tests.Yield/<Enumerable>d__4::MoveNext()")
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
