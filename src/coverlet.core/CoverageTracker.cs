using System;
using System.Collections.Generic;
using System.IO;
using Coverlet.Core.Attributes;

namespace Coverlet.Core
{
    public static class CoverageTracker
    {
        private static List<string> _markers;
        private static string _path;
        private static bool _registered;

        [ExcludeFromCoverage]
        public static void MarkExecuted(string path, string marker)
        {
            if (_markers == null)
                _markers = new List<string>();

            if (!_registered)
            {
                AppDomain.CurrentDomain.ProcessExit += new EventHandler(CurrentDomain_ProcessExit);
                _registered = true;
            }

            _markers.Add(marker);
            _path = path;
        }

        public static void CurrentDomain_ProcessExit(object sender, EventArgs e)
            => File.WriteAllLines(_path, _markers);
    }
}