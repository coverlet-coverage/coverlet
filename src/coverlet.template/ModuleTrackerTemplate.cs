using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Threading;

namespace Coverlet.Core.Instrumentation
{
    /// <summary>
    /// This static class will be injected on a module being instrumented in order to direct on module hits
    /// to a single location.
    /// </summary>
    /// <remarks>
    /// As this type is going to be customized for each instrumented module it doesn't follow typical practices
    /// regarding visibility of members, etc.
    /// </remarks>
    [ExcludeFromCodeCoverage]
    public static class ModuleTrackerTemplate
    {
        public const int HitsResultHeaderSize = 2;
        public const int HitsResultUnloadStarted = 0;
        public const int HitsResultUnloadFinished = 1;

        public static string HitsFilePath;
        public static string HitsMemoryMapName;
        public static int HitsArraySize;

        // Special case when instrumenting CoreLib, the thread static below prevents infinite loop in CoreLib
        // while allowing the tracker template to call any of its types and functions.
        [ThreadStatic]
        private static bool t_isTracking;

        [ThreadStatic]
        private static int[] t_threadHits;

        private static readonly List<int[]> _threads;

        static ModuleTrackerTemplate()
        {
            t_isTracking = true;
            _threads = new List<int[]>(2 * Environment.ProcessorCount);

            AppDomain.CurrentDomain.ProcessExit += new EventHandler(UnloadModule);
            AppDomain.CurrentDomain.DomainUnload += new EventHandler(UnloadModule);
            t_isTracking = false;
            // At the end of the instrumentation of a module, the instrumenter needs to add code here
            // to initialize the static fields according to the values derived from the instrumentation of
            // the module.
        }

        public static void RecordHit(int hitLocationIndex)
        {
            if (t_isTracking)
                return;

            if (t_threadHits == null)
            {
                t_isTracking = true;

                lock (_threads)
                {
                    if (t_threadHits == null)
                    {
                        t_threadHits = new int[HitsArraySize];
                        _threads.Add(t_threadHits);
                    }
                }

                t_isTracking = false;
            }

            ++t_threadHits[hitLocationIndex];
        }

        public static void UnloadModule(object sender, EventArgs e)
        {
            t_isTracking = true;

            int[][] threads;
            lock (_threads)
            {
                threads = _threads.ToArray();

                // Don't double-count if UnloadModule is called more than once
                _threads.Clear();
            }

            // The same module can be unloaded multiple times in the same process via different app domains.
            // Use a global mutex to ensure no concurrent access.
            using (var mutex = new Mutex(true, HitsMemoryMapName + "_Mutex", out bool createdNew))
            {
                if (!createdNew)
                    mutex.WaitOne();

                MemoryMappedFile memoryMap = null;

                try
                {
                    try
                    {
                        memoryMap = MemoryMappedFile.OpenExisting(HitsMemoryMapName);
                    }
                    catch (PlatformNotSupportedException)
                    {
                        memoryMap = MemoryMappedFile.CreateFromFile(HitsFilePath, FileMode.Open, null, (HitsArraySize + HitsResultHeaderSize) * sizeof(int));
                    }

                    // Tally hit counts from all threads in memory mapped area
                    var accessor = memoryMap.CreateViewAccessor();
                    using (var buffer = accessor.SafeMemoryMappedViewHandle)
                    {
                        unsafe
                        {
                            byte* pointer = null;
                            buffer.AcquirePointer(ref pointer);
                            try
                            {
                                var intPointer = (int*) pointer;

                                // Signal back to coverage analysis that we've started transferring hit counts.
                                // Use interlocked here to ensure a memory barrier before the Coverage class reads
                                // the shared data.
                                Interlocked.Increment(ref *(intPointer + HitsResultUnloadStarted));

                                for (var i = 0; i < HitsArraySize; i++)
                                {
                                    var count = 0;

                                    foreach (var threadHits in threads)
                                    {
                                        count += threadHits[i];
                                    }

                                    if (count > 0)
                                    {
                                        // There's a header of one int before the hit counts
                                        var hitLocationArrayOffset = intPointer + i + HitsResultHeaderSize;

                                        // No need to use Interlocked here since the mutex ensures only one thread updates 
                                        // the shared memory map.
                                        *hitLocationArrayOffset += count;
                                    }
                                }

                                // Signal back to coverage analysis that all hit counts were successfully tallied.
                                Interlocked.Increment(ref *(intPointer + HitsResultUnloadFinished));
                            }
                            finally
                            {
                                buffer.ReleasePointer();
                            }
                        }
                    }
                }
                finally
                {
                    mutex.ReleaseMutex();
                    memoryMap?.Dispose();
                }
            }

            t_isTracking = false;
        }
    }
}
