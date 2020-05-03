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
        private readonly TestPlatformEqtTrace _eqtTrace;
        private readonly TestPlatformLogger _logger;

        public CoverletLogger(TestPlatformEqtTrace eqtTrace, TestPlatformLogger logger)
        {
            _eqtTrace = eqtTrace;
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
            _eqtTrace.Info(message);
        }

        /// <summary>
        /// Logs verbose
        /// </summary>
        /// <param name="message">Verbose message</param>
        public void LogVerbose(string message)
        {
            _eqtTrace.Verbose(message);
        }

        /// <summary>
        /// Logs warning
        /// </summary>
        /// <param name="message">Warning message</param>
        public void LogWarning(string message)
        {
            _eqtTrace.Warning(message);
        }
    }
}
