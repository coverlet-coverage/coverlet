// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
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
