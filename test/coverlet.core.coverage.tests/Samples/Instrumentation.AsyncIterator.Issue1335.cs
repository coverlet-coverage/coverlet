// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Coverlet.Core.CoverageSamples.Tests
{
  public class AsyncIteratorIssue1335
  {
        public async IAsyncEnumerable<IAsyncEnumerable<T>> ExecuteReproduction<T>(IAsyncEnumerable<T> source, int batchSize)
        {
            var enumerator = source.GetAsyncEnumerator();
            while (await enumerator.MoveNextAsync())
            {
                yield return YieldBatch(enumerator, batchSize);
            }
        }

        private async IAsyncEnumerable<T> YieldBatch<T>(IAsyncEnumerator<T> source, int batchSize)
        {
            yield return source.Current;
            for (int i = 1; i < batchSize && await source.MoveNextAsync(); i++)
            {
                yield return source.Current;
            }
        }
    }
}