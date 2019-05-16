using System;

namespace Coverlet.Collector.Utilities
{
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
