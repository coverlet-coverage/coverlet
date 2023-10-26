﻿// Copyright (c) Toni Solarin-Sodara
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
    public void CatchBlock_Issue465()
    {
      string path = Path.GetTempFileName();
      try
      {
        FunctionExecutor.Run(async (string[] pathSerialize) =>
        {
          CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<CatchBlock>(async instance =>
                  {
              instance.Test();
              instance.Test_Catch();
              await (Task)instance.TestAsync();
              await (Task)instance.TestAsync_Catch();

              instance.Test(true);
              instance.Test_Catch(true);
              await (Task)instance.TestAsync(true);
              await (Task)instance.TestAsync_Catch(true);

              instance.Test(false);
              instance.Test_Catch(false);
              await (Task)instance.TestAsync(false);
              await (Task)instance.TestAsync_Catch(false);

              instance.Test_WithTypedCatch();
              instance.Test_Catch_WithTypedCatch();
              await (Task)instance.TestAsync_WithTypedCatch();
              await (Task)instance.TestAsync_Catch_WithTypedCatch();

              instance.Test_WithTypedCatch(true);
              instance.Test_Catch_WithTypedCatch(true);
              await (Task)instance.TestAsync_WithTypedCatch(true);
              await (Task)instance.TestAsync_Catch_WithTypedCatch(true);

              instance.Test_WithTypedCatch(false);
              instance.Test_Catch_WithTypedCatch(false);
              await (Task)instance.TestAsync_WithTypedCatch(false);
              await (Task)instance.TestAsync_Catch_WithTypedCatch(false);

              instance.Test_WithNestedCatch(true);
              instance.Test_Catch_WithNestedCatch(true);
              await (Task)instance.TestAsync_WithNestedCatch(true);
              await (Task)instance.TestAsync_Catch_WithNestedCatch(true);

              instance.Test_WithNestedCatch(false);
              instance.Test_Catch_WithNestedCatch(false);
              await (Task)instance.TestAsync_WithNestedCatch(false);
              await (Task)instance.TestAsync_Catch_WithNestedCatch(false);

            }, persistPrepareResultToFile: pathSerialize[0]);
          return 0;
        }, new string[] { path });

        CoverageResult res = TestInstrumentationHelper.GetCoverageResult(path);
        res.Document("Instrumentation.CatchBlock.cs")
            .AssertLinesCoveredAllBut(BuildConfiguration.Debug, 45, 59, 113, 127, 137, 138, 139, 153, 154, 155, 156, 175, 189, 199, 200, 201, 222, 223, 224, 225, 252, 266, 335, 349)
            .ExpectedTotalNumberOfBranches(BuildConfiguration.Debug, 6)
            .ExpectedTotalNumberOfBranches(BuildConfiguration.Release, 6);
      }
      finally
      {
        File.Delete(path);
      }
    }
  }
}
