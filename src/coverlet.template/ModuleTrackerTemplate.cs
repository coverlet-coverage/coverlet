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

        static ModuleTrackerTemplate()
        {
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(UnloadModule);
            AppDomain.CurrentDomain.DomainUnload += new EventHandler(UnloadModule);

            // At the end of the instrumentation of a module, the instrumenter needs to add code here
            // to initialize the static fields according to the values derived from the instrumentation of
            // the module.
        }

        public static void RecordHit(int hitLocationIndex)
        {
            // Make sure to avoid recording if this is a call to RecordHit within the AppDomain setup code in an
            // instrumented build of System.Private.CoreLib.
            if (HitsArray is null)
                return;

            Interlocked.Increment(ref HitsArray[hitLocationIndex]);
        }

        public static void UnloadModule(object sender, EventArgs e)
        {
            // Claim the current hits array and reset it to prevent double-counting scenarios.
            var hitsArray = Interlocked.Exchange(ref HitsArray, new int[HitsArray.Length]);

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
                        bw.Write(hitsArray.Length);
                        foreach (int hitCount in hitsArray)
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
                        if (hitsLength != hitsArray.Length)
                        {
                            throw new InvalidOperationException(
                                $"{HitsFilePath} has {hitsLength} entries but on memory {nameof(HitsArray)} has {hitsArray.Length}");
                        }

                        for (int i = 0; i < hitsLength; ++i)
                        {
                            int oldHitCount = br.ReadInt32();
                            bw.Seek(-sizeof(int), SeekOrigin.Current);
                            bw.Write(hitsArray[i] + oldHitCount);
                        }
                    }
                }

                // On purpose this is not under a try-finally: it is better to have an exception if there was any error writing the hits file
                // this case is relevant when instrumenting corelib since multiple processes can be running against the same instrumented dll.
                mutex.ReleaseMutex();
            }
        }
    }
}
