// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Coverlet.Collector.Utilities
{
    [Serializable()]
    public class CoverletDataCollectorException : Exception
    {
        public CoverletDataCollectorException() : base() { }
        public CoverletDataCollectorException(string message) : base(message) { }

        public CoverletDataCollectorException(string message, Exception innerException) : base(message, innerException) { }

        protected CoverletDataCollectorException(System.Runtime.Serialization.SerializationInfo info,
    System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

}
