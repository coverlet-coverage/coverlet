// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

#nullable disable

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
