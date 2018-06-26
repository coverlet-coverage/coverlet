using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Coverlet.Tracker
{
    public static class CoverageTracker
    {
        private static Dictionary<string, Dictionary<string, int>> _events;

        [ExcludeFromCodeCoverage]
        static CoverageTracker()
        {
            _events = new Dictionary<string, Dictionary<string, int>>();
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(CurrentDomain_ProcessExit);
            AppDomain.CurrentDomain.DomainUnload += new EventHandler(CurrentDomain_ProcessExit);
        }

        [ExcludeFromCodeCoverage]
        public static void MarkExecuted(string file, string evt)
        {
            lock (_events)
            {
                if (!_events.TryGetValue(file, out var fileEvents))
                {
                    fileEvents = new Dictionary<string, int>();
                    _events.Add(file, fileEvents);
                }

                if (!fileEvents.TryGetValue(evt, out var count))
                {
                    fileEvents.Add(evt, 1);
                }
                else if (count < int.MaxValue)
                {
                    fileEvents[evt] = count + 1;
                }
            }
        }

        [ExcludeFromCodeCoverage]
        public static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            lock (_events)
            {
                foreach (var files in _events)
                {
                    using (var fs = new FileStream(files.Key, FileMode.Create))
                    using (var sw = new StreamWriter(fs))
                    {
                        foreach (var evt in files.Value)
                        {
                            sw.WriteLine($"{evt.Key},{evt.Value}");
                        }
                    }
                }

                _events.Clear();
            }
        }
    }
}