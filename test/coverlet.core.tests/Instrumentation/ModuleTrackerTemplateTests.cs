using System;
using System.IO;
using System.Threading;
using Xunit;

namespace Coverlet.Core.Instrumentation.Tests
{
    public class ModuleTrackerTemplateTests : IDisposable
    {
        readonly string tempFileName = Path.Combine(Path.GetTempPath(), nameof(ModuleTrackerTemplateTests));

        public ModuleTrackerTemplateTests()
        {
            Dispose();
            ModuleTrackerTemplate.hitsFilePath = tempFileName;
            ModuleTrackerTemplate.hitsArraySize = 4;
        }

        public void Dispose()
        {
            File.Delete(tempFileName);
        }

        [Fact]
        public void HitsFileCorrectlyWritten()
        {
            ModuleTrackerTemplate.Setup();
            try
            {
                ModuleTrackerTemplate.RecordHit(3);
                ModuleTrackerTemplate.RecordHit(3);
                ModuleTrackerTemplate.RecordHit(1);
                ModuleTrackerTemplate.RecordHit(1);
                ModuleTrackerTemplate.RecordHit(0);
                ModuleTrackerTemplate.RecordHit(3);
            }
            finally
            {
                ModuleTrackerTemplate.Dispose();
            }

            var expectedHitsArray = new[] { 1, 2, 0, 3 };
            Assert.Equal(expectedHitsArray, ReadHitsFile());
        }

        [Fact]
        public void HitsOnMultipleThreadsCorrectlyCounted()
        {
            using (var semaphore = new SemaphoreSlim(1))
            {
                semaphore.Wait();
                int joinCount = 0;
                ModuleTrackerTemplate.Setup();
                try
                {
                    void HitIndex(object index)
                    {
                        var hitIndex = (int)index;
                        for (int i = 0; i <= hitIndex; ++i)
                            ModuleTrackerTemplate.RecordHit(i);

                        if (Interlocked.Increment(ref joinCount) == 4)
                            semaphore.Release();
                    }

                    for (int i = 0; i < 4; ++i)
                    {
                        var t = new Thread(HitIndex);
                        t.Start(i);
                    }
                }
                finally
                {
                    ModuleTrackerTemplate.Dispose();
                }

                semaphore.Wait();
            }

            var expectedHitsArray = new[] { 4, 3, 2, 1 };
            Assert.Equal(expectedHitsArray, ReadHitsFile());
        }

        [Fact]
        public void MultipleRecordingsHaveCorrectTotalData()
        {
            ModuleTrackerTemplate.Setup();
            try
            {
                ModuleTrackerTemplate.RecordHit(1);
                ModuleTrackerTemplate.RecordHit(3);
                ModuleTrackerTemplate.RecordHit(2);
                ModuleTrackerTemplate.RecordHit(2);
                ModuleTrackerTemplate.RecordHit(3);
            }
            finally
            {
                ModuleTrackerTemplate.Dispose();
            }
            ModuleTrackerTemplate.Setup();
            try
            {
                ModuleTrackerTemplate.RecordHit(1);
                ModuleTrackerTemplate.RecordHit(1);
                ModuleTrackerTemplate.RecordHit(3);
                ModuleTrackerTemplate.RecordHit(2);
                ModuleTrackerTemplate.RecordHit(2);
                ModuleTrackerTemplate.RecordHit(1);
                ModuleTrackerTemplate.RecordHit(3);
            }
            finally
            {
                ModuleTrackerTemplate.Dispose();
            }

            var expectedHitsArray = new[] { 0, 4, 4, 4 };
            Assert.Equal(expectedHitsArray, ReadHitsFile());
        }

        private void WriteHitsFile(int[] hitsArray)
        {
            using (var fs = new FileStream(tempFileName, FileMode.Create))
            using (var bw = new BinaryWriter(fs))
            {
                bw.Write(hitsArray.Length);
                foreach (int hitCount in hitsArray)
                {
                    bw.Write(hitCount);
                }
            }
        }

        private int[] ReadHitsFile()
        {
            using (var fs = new FileStream(tempFileName, FileMode.Open))
            using (var br = new BinaryReader(fs))
            {
                var hitsArray = new int[br.ReadInt32()];
                for (int i = 0; i < hitsArray.Length; ++i)
                {
                    hitsArray[i] = br.ReadInt32();
                }

                return hitsArray;
            }
        }
    }
}
