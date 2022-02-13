// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Coverlet.Core.Abstractions
{
    internal interface IRetryHelper
    {
        void Retry(Action action, Func<TimeSpan> backoffStrategy, int maxAttemptCount = 3);
        T Do<T>(Func<T> action, Func<TimeSpan> backoffStrategy, int maxAttemptCount = 3);
    }
}
