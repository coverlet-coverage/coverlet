// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Coverlet.Core.CoverageSamples.Tests
{
    public class AsyncIteratorIssue1836
    {
        public async IAsyncEnumerable<T> Issue1836_GenericFunctionWithCancellationThatReturnsIAsyncEnumerable<T>
        ([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            T[] items = [default, default];
            foreach (var item in items)
            {
                await Task.CompletedTask;
                yield return !cancellationToken.IsCancellationRequested ? item : throw new OperationCanceledException();
            }
        }
    }
}
