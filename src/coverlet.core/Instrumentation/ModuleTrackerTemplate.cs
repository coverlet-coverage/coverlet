using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Coverlet.Core.Instrumentation
{
    /// <summary>
    /// This static class will be injected on a module being instrumented in order to direct on module hits
    /// 'to a single location.
    /// </summary>
    /// <remarks>
    /// As this type is going to be customized on each module it doesn't follow typical practices regarding
    /// visibility of members, etc.
    /// </remarks>
    public static class ModuleTrackerTemplate
    {
        public static string HitsFilePath;
        public static int[] HitsArray;

        [ThreadStatic]
        private static int[] threadHits;

        private static List<int[]> threads;

        static ModuleTrackerTemplate()
        {
            threads = new List<int[]>(2 * Environment.ProcessorCount);

            AppDomain.CurrentDomain.ProcessExit += new EventHandler(UnloadModule);
            AppDomain.CurrentDomain.DomainUnload += new EventHandler(UnloadModule);
            // At the end of the instrumentation of a module, the instrumenter needs to add code here
            // to initialize the static fields according to the values derived from the instrumentation of
            // the module.
        }

        public static void RecordHit(int hitLocationIndex)
        {
            if (threadHits == null)
            {
                lock (threads)
                {
                    threadHits = new int[HitsArray.Length];
                    threads.Add(threadHits);
                }
            }

            ++threadHits[hitLocationIndex];
        }

        public static void UnloadModule(object sender, EventArgs e)
        {
            // Update the global hits array from data from all the threads
            lock (threads)
            {
                foreach (var threadHits in threads)
                {
                    for (int i = 0; i < HitsArray.Length; ++i)
                        HitsArray[i] += threadHits[i];
                }
            }

            // TODO: same module can be unloaded multiple times in the same process. Need to check and handle this case.
            // TODO: perhaps some kind of global mutex based on the name of the modules hits file, and after the first
            // TODO: they update the hit count from the file already created. Something like this, (minus the read stuff):
            using (var mutex = new Mutex(true, Path.GetFileNameWithoutExtension(HitsFilePath) + "_Mutex", out bool createdNew))
            {
                if (!createdNew)
                    mutex.WaitOne();

                if (!File.Exists(HitsFilePath))
                {
                    // File not created yet, just write it
                    using (var fs = new FileStream(HitsFilePath, FileMode.Create))
                    using (var bw = new BinaryWriter(fs))
                    {
                        bw.Write(HitsArray.Length);
                        foreach (int hitCount in HitsArray)
                        {
                            bw.Write(hitCount);
                        }
                    }
                }
                else
                {
                    using (var fs = File.Open(HitsFilePath, FileMode.Open))
                    using (var br = new BinaryReader(fs))
                    using (var bw = new BinaryWriter(fs))
                    {
                        int hitsLength = br.ReadInt32();
                        // TODO: check hits length match

                        for (int i = 0; i < hitsLength; ++i)
                        {
                            int oldHitCount = br.ReadInt32();
                            bw.Seek(-sizeof(int), SeekOrigin.Current);
                            bw.Write(HitsArray[i] + oldHitCount);
                        }
                    }
                }
            }
        }
    }
}
