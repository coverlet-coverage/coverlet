using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Coverlet.Core.Attributes;
using Coverlet.Core.Extensions;

namespace Coverlet.Core
{
    public static class CoverageTracker
    {
        private static Dictionary<string, List<string>> _markers;
        private static Dictionary<string, int> _markerFileCount;

        [ExcludeFromCoverage]
        static CoverageTracker()
        {
            _markers = new Dictionary<string, List<string>>();
            _markerFileCount = new Dictionary<string, int>();
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(CurrentDomain_ProcessExit);
        }

        [ExcludeFromCoverage]
        public static void MarkExecuted(string path, string marker)
        {
            lock (_markers)
            {
                _markers.TryAdd(path, new List<string>());
                _markers[path].Add(marker);
                _markerFileCount.TryAdd(path, 0);
                if (_markers[path].Count >= 100000)
                {
                    using (var fs = new FileStream($"{path}_compressed_{_markerFileCount[path]}", FileMode.OpenOrCreate))
                    using (var gz = new GZipStream(fs, CompressionMode.Compress))
                    using (var sw = new StreamWriter(gz))
                    {
                        foreach(var line in _markers[path])
                        {
                            sw.WriteLine(line);
                        }
                    }
                    _markers[path].Clear();
                    _markerFileCount[path] = _markerFileCount[path] + 1;
                }
            }
        }

        [ExcludeFromCoverage]
        public static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            foreach (var kvp in _markers)
            {
                using (var fs = new FileStream($"{kvp.Key}_compressed_{_markerFileCount[kvp.Key]}", FileMode.OpenOrCreate))
                using (var gz = new GZipStream(fs, CompressionMode.Compress))
                using (var sw = new StreamWriter(gz))
                {
                    foreach(var line in kvp.Value)
                    {
                        sw.WriteLine(line);
                    }
                }
            }
        }
    }
}