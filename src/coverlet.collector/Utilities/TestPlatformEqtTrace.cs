using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace Coverlet.Collector.Utilities
{
    /// <summary>
    /// Test platform eqttrace
    /// </summary>
    internal class TestPlatformEqtTrace
    {
        public bool IsInfoEnabled => EqtTrace.IsInfoEnabled;
        public bool IsVerboseEnabled => EqtTrace.IsVerboseEnabled;

        /// <summary>
        /// Verbose logger
        /// </summary>
        /// <param name="format">Format</param>
        /// <param name="args">Args</param>
        public void Verbose(string format, params object[] args)
        {
            EqtTrace.Verbose(format, args);
        }

        /// <summary>
        /// Warning logger
        /// </summary>
        /// <param name="format">Format</param>
        /// <param name="args">Args</param>
        public void Warning(string format, params object[] args)
        {
            EqtTrace.Warning(format, args);
        }

        /// <summary>
        /// Info logger
        /// </summary>
        /// <param name="format">Format</param>
        /// <param name="args">Args</param>
        public void Info(string format, params object[] args)
        {
            EqtTrace.Info(format, args);
        }
    }
}
