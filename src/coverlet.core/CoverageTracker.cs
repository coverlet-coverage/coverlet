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
        }

        [ExcludeFromCoverage]
        public static void MarkExecuted(string path, string marker)
        {
            lock (_markers)
            {
                _markers.TryAdd(path, new List<string>());
                _markers[path].Add(marker);
                File.WriteAllLines(path, _markers[path]);
            }
        }
    }
}