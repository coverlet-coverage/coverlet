using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Coverlet.Tracker
{
    public static class CoverageTracker
    {
        private static List<Dictionary<string, Dictionary<string, int>>> _events;

        [ThreadStatic]
        private static Dictionary<string, Dictionary<string, int>> t_events;

        [ExcludeFromCodeCoverage]
        static CoverageTracker()
        {
            _events = new List<Dictionary<string, Dictionary<string, int>>>();
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(CurrentDomain_ProcessExit);
            AppDomain.CurrentDomain.DomainUnload += new EventHandler(CurrentDomain_ProcessExit);
        }

        [ExcludeFromCodeCoverage]
        public static void MarkExecuted(string file, string evt)
        {
            if (t_events == null)
            {
                t_events = new Dictionary<string, Dictionary<string, int>>();
                lock (_events)
                {
                    _events.Add(t_events);
                }
            }

            // We are taking the lock only for synchronizing with CurrentDomain_ProcessExit event
            // but the lock should be fast as the thread shouldn't block before getting process exit event
            lock (t_events)
            {
                if (!t_events.TryGetValue(file, out var fileEvents))
                {
                    fileEvents = new Dictionary<string, int>();
                    t_events.Add(file, fileEvents);
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
            Dictionary<string, bool> encounteredFile = new Dictionary<string, bool>();

            lock (_events)
            {
                foreach (var localEvents in _events)
                {
                    lock (localEvents)
                    {
                        foreach (var files in localEvents)
                        {
                            FileMode mode = FileMode.Open;
                            if (!encounteredFile.TryGetValue(files.Key, out bool b))
                            {
                                encounteredFile.Add(files.Key, true);
                                mode = FileMode.Create;
                            }
                            using (var fs = new FileStream(files.Key, mode))
                            using (var sw = new StreamWriter(fs))
                            {
                                foreach (var evt in files.Value)
                                {
                                    sw.WriteLine($"{evt.Key},{evt.Value}");
                                }
                            }
                        }
                    }
                }

                _events.Clear();
            }
        }
    }
}