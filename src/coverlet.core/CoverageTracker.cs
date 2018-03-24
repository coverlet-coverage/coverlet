using System;
using System.Collections.Generic;
using System.IO;
using Coverlet.Core.Attributes;

namespace Coverlet.Core
{
    public static class CoverageTracker
    {
        private static Dictionary<string, List<string>> _markers;
        private static bool _registered;

        [ExcludeFromCoverage]
        public static void MarkExecuted(string path, string marker)
        {
            if (_markers == null)
            {
                _markers = new Dictionary<string, List<string>>();
            }

            if (!_markers.ContainsKey(path))
            {
                _markers.Add(path, new List<string>());
            }

            if (!_registered)
            {
                AppDomain.CurrentDomain.ProcessExit += new EventHandler(CurrentDomain_ProcessExit);
                _registered = true;
            }

            _markers[path].Add(marker);
        }

        public static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            foreach (var kvp in _markers)
            {
                File.WriteAllLines(kvp.Key, kvp.Value);
            }
        }
    }
}