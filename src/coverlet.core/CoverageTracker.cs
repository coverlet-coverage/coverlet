using System;
using System.Collections.Generic;
using System.IO;

using Coverlet.Core.Attributes;
using Coverlet.Core.Extensions;

namespace Coverlet.Core
{
    public static class CoverageTracker
    {
        private static Dictionary<string, List<string>> _markers;

        [ExcludeFromCoverage]
        static CoverageTracker()
        {
            _markers = new Dictionary<string, List<string>>();
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(CurrentDomain_ProcessExit);
        }

        [ExcludeFromCoverage]
        public static void MarkExecuted(string path, string marker)
        {
            lock (_markers)
            {
                _markers.TryAdd(path, new List<string>());
                _markers[path].Add(marker);
            }
        }

        public static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            foreach (var kvp in _markers)
                File.WriteAllLines(kvp.Key, kvp.Value);
        }
    }
}