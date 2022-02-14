// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestPlatform.ObjectModel;

#nullable disable

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
            EqtTrace.Verbose($"[coverlet]{format}", args);
        }

        /// <summary>
        /// Warning logger
        /// </summary>
        /// <param name="format">Format</param>
        /// <param name="args">Args</param>
        public void Warning(string format, params object[] args)
        {
            EqtTrace.Warning($"[coverlet]{format}", args);
        }

        /// <summary>
        /// Info logger
        /// </summary>
        /// <param name="format">Format</param>
        /// <param name="args">Args</param>
        public void Info(string format, params object[] args)
        {
            EqtTrace.Info($"[coverlet]{format}", args);
        }

        /// <summary>
        /// Error logger
        /// </summary>
        /// <param name="format">Format</param>
        /// <param name="args">Args</param>
        public void Error(string format, params object[] args)
        {
            EqtTrace.Error($"[coverlet]{format}", args);
        }
    }
}
