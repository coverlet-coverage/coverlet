// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
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
    public void GenericAsyncIterator()
    {
      string path = Path.GetTempFileName();
      try
      {
        RemoteInvokeHandle h = RemoteExecutor.Invoke(async (string arg0) =>
        {
          string[] pathSerialize = [arg0];
          CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<GenericAsyncIterator<int>>(async instance =>
                  {
                    List<int> res = await (Task<List<int>>)instance.Issue1383();
                  }, persistPrepareResultToFile: pathSerialize[0]);
        }, path );
        using (h)
        {
          Assert.Equal(RemoteExecutor.SuccessExitCode, h.ExitCode);
        }

        TestInstrumentationHelper.GetCoverageResult(path)
            .Document("Instrumentation.GenericAsyncIterator.cs")
            .AssertLinesCovered(BuildConfiguration.Debug, (13, 1), (14, 1), (20, 1), (21, 1), (22, 1))
            .ExpectedTotalNumberOfBranches(BuildConfiguration.Debug, 0);
      }
      finally
      {
        File.Delete(path);
      }
    }
  }
}
