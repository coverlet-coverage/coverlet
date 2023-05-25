// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Coverlet.Core.Samples.Tests;
using Xunit;

namespace Coverlet.Core.Tests
{
    public partial class CoverageTests
    {
        [Fact]
        public void GenericAsyncIterator()
        {
            string path = Path.GetTempFileName();
            try
            {
                FunctionExecutor.Run(async (string[] pathSerialize) =>
                {
                    CoveragePrepareResult coveragePrepareResult = await TestInstrumentationHelper.Run<GenericAsyncIterator<int>>(instance =>
                    {
                        List<int> res = ((Task<List<int>>)instance.Issue1383()).GetAwaiter().GetResult();

                        return Task.CompletedTask;
                    }, persistPrepareResultToFile: pathSerialize[0]).ConfigureAwait(false);
                    return 0;
                }, new string[] { path });

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
