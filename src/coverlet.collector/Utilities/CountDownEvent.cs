// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using Coverlet.Collector.Utilities.Interfaces;

namespace Coverlet.Collector.Utilities
{
    internal class CollectorCountdownEventFactory : ICountDownEventFactory
    {
        public ICountDownEvent Create(int count, TimeSpan waitTimeout)
        {
            return new CollectorCountdownEvent(count, waitTimeout);
        }
    }

    internal class CollectorCountdownEvent : ICountDownEvent
    {
        private readonly CountdownEvent _countDownEvent;
        private readonly TimeSpan _waitTimeout;

        public CollectorCountdownEvent(int count, TimeSpan waitTimeout)
        {
            _countDownEvent = new CountdownEvent(count);
            _waitTimeout = waitTimeout;
        }

        public void Signal()
        {
            _countDownEvent.Signal();
        }

        public void Wait()
        {
            if (!_countDownEvent.Wait(_waitTimeout))
            {
                throw new TimeoutException($"CollectorCountdownEvent timeout after {_waitTimeout}");
            }
        }
    }
}
