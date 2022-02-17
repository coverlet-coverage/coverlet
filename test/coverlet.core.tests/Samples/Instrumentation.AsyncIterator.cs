// Remember to use full name because adding new using directives change line numbers

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Coverlet.Core.Samples.Tests
{
    public class AsyncIterator
    {
        async public Task<int> Issue1104_Repro()
        {
            int sum = 0;

            await foreach (int result in CreateSequenceAsync())
            {
                sum += result;
            }

            return sum;
        }

        async private IAsyncEnumerable<int> CreateSequenceAsync()
        {
            for (int i = 0; i < 100; ++i)
            {
                await Task.CompletedTask;
                yield return i;
            }
        }
    }
}
