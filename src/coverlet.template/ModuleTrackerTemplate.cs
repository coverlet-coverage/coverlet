using System;
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
        public static int[] HitsArray;

        static ModuleTrackerTemplate()
        {
            // At the end of the instrumentation of a module, the instrumenter needs to add code here
            // to initialize the static fields according to the values derived from the instrumentation of
            // the module.
        }

        // A call to this method will be injected in the static constructor above for most cases. However, if the
        // current assembly is System.Private.CoreLib (or more specifically, defines System.AppDomain), a call directly
        // to UnloadModule will be injected in System.AppContext.OnProcessExit.
        public static void RegisterUnloadEvents()
        {
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(UnloadModule);
            AppDomain.CurrentDomain.DomainUnload += new EventHandler(UnloadModule);
        }

        public static void RecordHitInCoreLibrary(int hitLocationIndex)
        {
            // Make sure to avoid recording if this is a call to RecordHit within the AppDomain setup code in an
            // instrumented build of System.Private.CoreLib.
            if (HitsArray is null)
                return;

            Interlocked.Increment(ref HitsArray[hitLocationIndex]);
        }

        public static void RecordHit(int hitLocationIndex)
        {
            Interlocked.Increment(ref HitsArray[hitLocationIndex]);
        }

        public static void UnloadModule(object sender, EventArgs e)
        {
            // Claim the current hits array and reset it to prevent double-counting scenarios.
            var hitsArray = Interlocked.Exchange(ref HitsArray, new int[HitsArray.Length]);

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
                        memoryMap = MemoryMappedFile.CreateFromFile(HitsFilePath, FileMode.Open, null, (HitsArray.Length + HitsResultHeaderSize) * sizeof(int));
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

                                for (var i = 0; i < hitsArray.Length; i++)
                                {
                                    var count = hitsArray[i];

                                    // By only modifying the memory map pages where there have been hits
                                    // unnecessary allocation of all-zero pages is avoided.
                                    if (count > 0)
                                    {
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
        }
    }
}
