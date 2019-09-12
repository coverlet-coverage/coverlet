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
            // We wait on another thread to avoid to block forever
            // We could use Task/Task.Delay timeout trick but this api and collector are sync so to
            // avoid too much GetAwaiter()/GetResult() I prefer keep code simple.
            // This thread is created only one time where we pass coverage files 
            var waitOnThread = new Thread(() => _countDownEvent.Wait());
            waitOnThread.Start();
            waitOnThread.Join(_waitTimeout);
        }
    }
}
