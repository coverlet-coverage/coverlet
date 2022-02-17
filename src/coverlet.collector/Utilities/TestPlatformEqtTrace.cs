// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace Coverlet.Collector.Utilities
{
    /// <summary>
    /// Test platform eqttrace
    /// </summary>
    internal static class TestPlatformEqtTrace
    {
        public static bool IsInfoEnabled => EqtTrace.IsInfoEnabled;
        public static bool IsVerboseEnabled => EqtTrace.IsVerboseEnabled;

        /// <summary>
        /// Verbose logger
        /// </summary>
        /// <param name="format">Format</param>
        /// <param name="args">Args</param>
        public static void Verbose(string format, params object[] args)
        {
            EqtTrace.Verbose($"[coverlet]{format}", args);
        }

        /// <summary>
        /// Warning logger
        /// </summary>
        /// <param name="format">Format</param>
        /// <param name="args">Args</param>
        public static void Warning(string format, params object[] args)
        {
            EqtTrace.Warning($"[coverlet]{format}", args);
        }

        /// <summary>
        /// Info logger
        /// </summary>
        /// <param name="format">Format</param>
        /// <param name="args">Args</param>
        public static void Info(string format, params object[] args)
        {
            EqtTrace.Info($"[coverlet]{format}", args);
        }

        /// <summary>
        /// Error logger
        /// </summary>
        /// <param name="format">Format</param>
        /// <param name="args">Args</param>
        public static void Error(string format, params object[] args)
        {
            EqtTrace.Error($"[coverlet]{format}", args);
        }
    }
}
