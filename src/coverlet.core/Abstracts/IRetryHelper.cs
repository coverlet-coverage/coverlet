using System;

namespace Coverlet.Core.Abstracts
{
    internal interface IRetryHelper
    {
        void Retry(Action action, Func<TimeSpan> backoffStrategy, int maxAttemptCount = 3);
        T Do<T>(Func<T> action, Func<TimeSpan> backoffStrategy, int maxAttemptCount = 3);
    }
}
