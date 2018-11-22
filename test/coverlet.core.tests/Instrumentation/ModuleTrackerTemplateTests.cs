using System;
using System.IO;
using System.Threading;
using Xunit;

namespace Coverlet.Core.Instrumentation.Tests
{
    public class ModuleTrackerTemplateTests : IDisposable
    {
        readonly string tempDirName = Path.Combine(Path.GetTempPath(), nameof(ModuleTrackerTemplateTests));

        public ModuleTrackerTemplateTests()
        {
            Dispose();
            Directory.CreateDirectory(tempDirName);
            ModuleTrackerTemplate.hitsDirectoryPath = tempDirName;
            ModuleTrackerTemplate.hitsArraySize = 4;
        }

        public void Dispose()
        {
            if (Directory.Exists(tempDirName))
                Directory.Delete(tempDirName, true);
        }

        [Fact]
        public void HitsFileCorrectlyWritten()
        {
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
                CloseMemoryMapping();
            }

            var expectedHitsArray = new[] { 1, 2, 0, 3 };
            Assert.Equal(expectedHitsArray, ReadHitsFiles());
        }

        [Fact]
        public void HitsOnMultipleThreadsCorrectlyCounted()
        {
            using (var semaphore = new SemaphoreSlim(1))
            {
                semaphore.Wait();
                int joinCount = 0;
                void HitIndex(object index)
                {
                    try
                    {
                        var hitIndex = (int)index;
                        for (int i = 0; i <= hitIndex; ++i)
                            ModuleTrackerTemplate.RecordHit(i);
                    }
                    finally
                    {
                        CloseMemoryMapping();
                    }
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
            Assert.Equal(expectedHitsArray, ReadHitsFiles());
        }

        [Fact]
        public void MultipleRecordingsHaveCorrectTotalData()
        {
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
                CloseMemoryMapping();
            }

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
                CloseMemoryMapping();
            }

            var expectedHitsArray = new[] { 0, 4, 4, 4 };
            Assert.Equal(expectedHitsArray, ReadHitsFiles());
        }

        private void WriteHitsFile(int[] hitsArray)
        {
            using (var fs = new FileStream(Path.Combine(tempDirName, 1.ToString()), FileMode.Create))
            using (var bw = new BinaryWriter(fs))
            {
                bw.Write(hitsArray.Length);
                foreach (int hitCount in hitsArray)
                {
                    bw.Write(hitCount);
                }
            }
        }

        private int[] ReadHitsFiles()
        {
            int[] hitsArray = null;
            foreach (var file in Directory.EnumerateFiles(tempDirName))
                using (var fs = new FileStream(Path.Combine(tempDirName, file), FileMode.Open))
                using (var br = new BinaryReader(fs))
                {
                    var expectedHitsArraySize = br.ReadInt32();
                    if (hitsArray == null)
                        hitsArray = new int[expectedHitsArraySize];
                    else
                        Assert.Equal(hitsArray.Length, expectedHitsArraySize);

                    Assert.Equal(fs.Length, sizeof(int) * (expectedHitsArraySize + 1));

                    for (int i = 0; i < hitsArray.Length; ++i)
                    {
                        hitsArray[i] += br.ReadInt32();
                    }
                }

            return hitsArray;
        }

        private void CloseMemoryMapping()
        {
            ModuleTrackerTemplate.memoryMappedViewAccessor?.Dispose();
            ModuleTrackerTemplate.memoryMappedViewAccessor = null;
        }
    }
}
