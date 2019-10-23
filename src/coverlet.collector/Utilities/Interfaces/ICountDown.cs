using System;

namespace Coverlet.Collector.Utilities.Interfaces
{

    /// <summary>
    /// Factory for ICountDownEvent
    /// </summary>
    internal interface ICountDownEventFactory
    {
        /// <summary>
        /// Create ICountDownEvent instance
        /// </summary>
        /// <param name="count">count of CountDownEvent</param>
        /// <param name="waitTimeout">max wait</param>
        /// <returns></returns>
        ICountDownEvent Create(int count, TimeSpan waitTimeout);
    }

    /// <summary>
    /// Wrapper interface for CountDownEvent
    /// </summary>
    internal interface ICountDownEvent
    {
        /// <summary>
        /// Signal event
        /// </summary>
        void Signal();

        /// <summary>
        /// Wait for event
        /// </summary>
        void Wait();
    }
}
