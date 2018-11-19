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
        }

        public void Dispose()
        {
            File.Delete(tempFileName);
        }

        [Fact]
        public void HitsFileCorrectlyWritten()
        {
            using (var tracker = new ModuleTrackerTemplate(tempFileName, 4))
            {
                tracker.InstanceRecordHit(3);
                tracker.InstanceRecordHit(3);
                tracker.InstanceRecordHit(1);
                tracker.InstanceRecordHit(1);
                tracker.InstanceRecordHit(0);
                tracker.InstanceRecordHit(3);
            }

            var expectedHitsArray = new[] { 1, 2, 0, 3 };
            Assert.Equal(expectedHitsArray, ReadHitsFile());
        }

        [Fact]
        public void HitsOnMultipleThreadsCorrectlyCounted()
        {
            using (var semaphore = new SemaphoreSlim(1, 1))
            using (var tracker = new ModuleTrackerTemplate(tempFileName, 4))
            {
                int joinCount = 0;
                void HitIndex(object index)
                {
                    var hitIndex = (int)index;
                    for (int i = 0; i <= hitIndex; ++i)
                        tracker.InstanceRecordHit(i);

                    if (Interlocked.Increment(ref joinCount) == 4)
                        semaphore.Release();
                }

                for (int i = 0; i < 4; ++i)
                {
                    var t = new Thread(HitIndex);
                    t.Start(i);
                }

                semaphore.Wait();
            }

            var expectedHitsArray = new[] { 4, 3, 2, 1 };
            Assert.Equal(expectedHitsArray, ReadHitsFile());
        }

        [Fact]
        public void MultipleParallelRecordingsHaveCorrectTotalData()
        {
            using (var tracker1 = new ModuleTrackerTemplate(tempFileName, 4))
            using (var tracker2 = new ModuleTrackerTemplate(tempFileName, 4))
            {
                tracker1.InstanceRecordHit(1);
                tracker2.InstanceRecordHit(3);
                tracker1.InstanceRecordHit(2);
                tracker2.InstanceRecordHit(2);
                tracker1.InstanceRecordHit(3);
                tracker2.InstanceRecordHit(1);
                tracker1.InstanceRecordHit(1);
                tracker2.InstanceRecordHit(3);
                tracker1.InstanceRecordHit(2);
                tracker2.InstanceRecordHit(2);
                tracker1.InstanceRecordHit(1);
                tracker2.InstanceRecordHit(3);
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
