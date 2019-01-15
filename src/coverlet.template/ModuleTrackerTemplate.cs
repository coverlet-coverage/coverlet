using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;

namespace Coverlet.Core.Instrumentation
{
    /// <summary>
    /// This static class will be injected on a module being instrumented in order to direct on module hits
    /// to a single location.
    /// </summary>
    /// <remarks>
    /// As this type is going to be customized for each instrumeted module it doesn't follow typical practices
    /// regarding visibility of members, etc.
    /// </remarks>
    [ExcludeFromCodeCoverage]
    public static class ModuleTrackerTemplate
    {
        public static string HitsFilePath;
        public static int[] HitsArray;

        // Special case when instrumenting CoreLib, the static below prevents infinite loop in CoreLib
        // while allowing the tracker template to call any of its types and functions.
        private static bool s_isTracking;

        static ModuleTrackerTemplate()
        {
            s_isTracking = true;
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(UnloadModule);
            AppDomain.CurrentDomain.DomainUnload += new EventHandler(UnloadModule);
            s_isTracking = false;

            // At the end of the instrumentation of a module, the instrumenter needs to add code here
            // to initialize the static fields according to the values derived from the instrumentation of
            // the module.
        }

        public static void RecordHit(int hitLocationIndex)
        {
            if (s_isTracking)
                return;

            Interlocked.Increment(ref HitsArray[hitLocationIndex]);
        }

        public static void UnloadModule(object sender, EventArgs e)
        {
            s_isTracking = true;

            // The same module can be unloaded multiple times in the same process via different app domains.
            // Use a global mutex to ensure no concurrent access.
            using (var mutex = new Mutex(true, Path.GetFileNameWithoutExtension(HitsFilePath) + "_Mutex", out bool createdNew))
            {
                if (!createdNew)
                    mutex.WaitOne();

                bool failedToCreateNewHitsFile = false;
                try
                {
                    using (var fs = new FileStream(HitsFilePath, FileMode.CreateNew))
                    using (var bw = new BinaryWriter(fs))
                    {
                        bw.Write(HitsArray.Length);
                        foreach (int hitCount in HitsArray)
                        {
                            bw.Write(hitCount);
                        }
                    }
                }
                catch
                {
                    failedToCreateNewHitsFile = true;
                }

                if (failedToCreateNewHitsFile)
                {
                    // Update the number of hits by adding value on disk with the ones on memory.
                    // This path should be triggered only in the case of multiple AppDomain unloads.
                    using (var fs = new FileStream(HitsFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                    using (var br = new BinaryReader(fs))
                    using (var bw = new BinaryWriter(fs))
                    {
                        int hitsLength = br.ReadInt32();
                        if (hitsLength != HitsArray.Length)
                        {
                            throw new InvalidOperationException(
                                $"{HitsFilePath} has {hitsLength} entries but on memory {nameof(HitsArray)} has {HitsArray.Length}");
                        }

                        for (int i = 0; i < hitsLength; ++i)
                        {
                            int oldHitCount = br.ReadInt32();
                            bw.Seek(-sizeof(int), SeekOrigin.Current);
                            bw.Write(HitsArray[i] + oldHitCount);
                        }
                    }
                }

                // Prevent any double counting scenario, i.e.: UnloadModule called twice (not sure if this can happen in practice ...)
                // Only an issue if DomainUnload and ProcessExit can both happens, perhaps can be removed...
                Array.Clear(HitsArray, 0, HitsArray.Length);

                // On purpose this is not under a try-finally: it is better to have an exception if there was any error writing the hits file
                // this case is relevant when instrumenting corelib since multiple processes can be running against the same instrumented dll.
                mutex.ReleaseMutex();
            }
        }
    }
}
