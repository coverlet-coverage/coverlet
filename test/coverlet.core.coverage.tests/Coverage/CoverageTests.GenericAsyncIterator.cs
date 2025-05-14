// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
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
    public async Task GenericAsyncIteratorAsync()
    {
      string path = Path.GetTempFileName();
      try
      {
        CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<GenericAsyncIterator<int>>(async instance =>
                {
                  List<int> res = await (Task<List<int>>)instance.Issue1383();
                }, persistPrepareResultToFile: path);

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
