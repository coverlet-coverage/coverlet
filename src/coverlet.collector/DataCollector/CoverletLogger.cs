using System;
using Coverlet.Collector.Utilities;
using Coverlet.Core.Logging;

namespace Coverlet.Collector.DataCollector
{
    /// <summary>
    /// Coverlet logger
    /// </summary>
    internal class CoverletLogger : ILogger
    {
        private readonly TestPlatformEqtTrace eqtTrace;
        private readonly TestPlatformLogger logger;

        public CoverletLogger(TestPlatformEqtTrace eqtTrace, TestPlatformLogger logger)
        {
            this.eqtTrace = eqtTrace;
            this.logger = logger;
        }

        /// <summary>
        /// Logs error
        /// </summary>
        /// <param name="message">Error message</param>
        public void LogError(string message)
        {
            this.logger.LogWarning(message);
        }

        /// <summary>
        /// Logs error
        /// </summary>
        /// <param name="exception">Exception to log</param>
        public void LogError(Exception exception)
        {
            this.logger.LogWarning(exception.ToString());
        }

        /// <summary>
        /// Logs information
        /// </summary>
        /// <param name="message">Information message</param>
        /// <param name="important">importance</param>
        public void LogInformation(string message, bool important = false)
        {
            this.eqtTrace.Info(message);
        }

        /// <summary>
        /// Logs verbose
        /// </summary>
        /// <param name="message">Verbose message</param>
        public void LogVerbose(string message)
        {
            this.eqtTrace.Verbose(message);
        }

        /// <summary>
        /// Logs warning
        /// </summary>
        /// <param name="message">Warning message</param>
        public void LogWarning(string message)
        {
            this.eqtTrace.Warning(message);
        }
    }
}
