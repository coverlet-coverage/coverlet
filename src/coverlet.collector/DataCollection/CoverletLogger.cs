// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Coverlet.Collector.Utilities;
using Coverlet.Core.Abstractions;

namespace Coverlet.Collector.DataCollection
{
    /// <summary>
    /// Coverlet logger
    /// </summary>
    internal class CoverletLogger : ILogger
    {
        private readonly TestPlatformLogger _logger;

        public CoverletLogger(TestPlatformLogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Logs error
        /// </summary>
        /// <param name="message">Error message</param>
        public void LogError(string message)
        {
            _logger.LogWarning(message);
        }

        /// <summary>
        /// Logs error
        /// </summary>
        /// <param name="exception">Exception to log</param>
        public void LogError(Exception exception)
        {
            _logger.LogWarning(exception.ToString());
        }

        /// <summary>
        /// Logs information
        /// </summary>
        /// <param name="message">Information message</param>
        /// <param name="important">importance</param>
        public void LogInformation(string message, bool important = false)
        {
            TestPlatformEqtTrace.Info(message);
        }

        /// <summary>
        /// Logs verbose
        /// </summary>
        /// <param name="message">Verbose message</param>
        public void LogVerbose(string message)
        {
            TestPlatformEqtTrace.Verbose(message);
        }

        /// <summary>
        /// Logs warning
        /// </summary>
        /// <param name="message">Warning message</param>
        public void LogWarning(string message)
        {
            TestPlatformEqtTrace.Warning(message);
        }
    }
}
