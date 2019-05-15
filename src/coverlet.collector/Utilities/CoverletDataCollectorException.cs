namespace Coverlet.Collector.Utilities
{
    using System;

    internal class CoverletDataCollectorException : Exception
    {
        public CoverletDataCollectorException(string message) : base(message)
        {
        }

        public CoverletDataCollectorException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
