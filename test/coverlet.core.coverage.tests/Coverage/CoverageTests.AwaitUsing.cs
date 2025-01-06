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
    [Fact]
    public void AwaitUsing()
    {
      string path = Path.GetTempFileName();
      try
      {
        FunctionExecutor.Run(async (string[] pathSerialize) =>
        {
          CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<AwaitUsing>(async instance =>
                  {
                    await (ValueTask)instance.HasAwaitUsing();
                    await (Task)instance.Issue914_Repro();
                    await (Task)instance.Issue1490_Repro<string>();

                  }, persistPrepareResultToFile: pathSerialize[0]);
          return 0;
        }, new string[] { path });

        TestInstrumentationHelper.GetCoverageResult(path)
        .Document("Instrumentation.AwaitUsing.cs")
        .AssertLinesCovered(BuildConfiguration.Debug,
                            // HasAwaitUsing()
                            (13, 1), (14, 1), (15, 1), (16, 1), (17, 1),
                            // Issue914_Repro()
                            (21, 1), (22, 1), (23, 1), (24, 1),
                            // Issue914_Repro_Example1()
                            (28, 1), (29, 1), (30, 1),
                            // Issue914_Repro_Example2()
                            (34, 1), (35, 1), (36, 1), (37, 1),
                            // Issue1490_Repro<T>()
                            (40, 1), (41, 1), (42, 1), (43, 1),
                            // MyTransaction.DisposeAsync()
                            (48, 3), (49, 3), (50, 3)
                            )
        .ExpectedTotalNumberOfBranches(BuildConfiguration.Debug, 0);
      }
      finally
      {
        File.Delete(path);
      }
    }
  }
}
