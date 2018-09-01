using Coverlet.Core.Instrumentation;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace coverlet.core.tests.Instrumentation
{
    public class ModuleTrackerTemplateTestsFixture : IDisposable
    {
        public ModuleTrackerTemplateTestsFixture()
        {
            ModuleTrackerTemplate.HitsFilePath = Path.Combine(Path.GetTempPath(), nameof(ModuleTrackerTemplateTests));
        }

        public void Dispose()
        {
            AppDomain.CurrentDomain.ProcessExit -= ModuleTrackerTemplate.UnloadModule;
            AppDomain.CurrentDomain.DomainUnload -= ModuleTrackerTemplate.UnloadModule;
        }
    }

    public class ModuleTrackerTemplateTests : IClassFixture<ModuleTrackerTemplateTestsFixture>, IDisposable
    {

        public ModuleTrackerTemplateTests()
        {
            File.Delete(ModuleTrackerTemplate.HitsFilePath);
        }

        public void Dispose()
        {
            File.Delete(ModuleTrackerTemplate.HitsFilePath);
        }

        [Fact]
        public void HitsFileCorrectlyWritten()
        {
            ModuleTrackerTemplate.HitsArray = new[] { 1, 2, 0, 3 };
            ModuleTrackerTemplate.UnloadModule(null, null);

            var expectedHitsArray = new[] { 1, 2, 0, 3 };
            Assert.Equal(expectedHitsArray, ReadHitsFile());
        }

        [Fact]
        public void HitsFileWithDifferentNumberOfEntriesCausesExceptionOnUnload()
        {
            WriteHitsFile(new[] { 1, 2, 3 });
            ModuleTrackerTemplate.HitsArray = new[] { 1 };
            Assert.Throws<InvalidDataException>(() => ModuleTrackerTemplate.UnloadModule(null, null));
        }

        [Fact]
        public void HitsOnMultipleThreadsCorrectlyCounted()
        {
            ModuleTrackerTemplate.HitsArray = new[] { 0, 0, 0, 0 };
            for (int i = 0; i < ModuleTrackerTemplate.HitsArray.Length; ++i)
            {
                var t = new Thread(HitIndex);
                t.Start(i);
            }

            ModuleTrackerTemplate.UnloadModule(null, null);
            var expectedHitsArray = new[] { 4, 3, 2, 1 };
            Assert.Equal(expectedHitsArray, ReadHitsFile());

            void HitIndex(object index)
            {
                var hitIndex = (int)index;
                for (int i = 0; i <= hitIndex; ++i)
                {
                    ModuleTrackerTemplate.RecordHit(i);
                }
            }
        }

        [Fact]
        public void MultipleSequentialUnloadsHaveCorrectTotalData()
        {
            ModuleTrackerTemplate.HitsArray = new[] { 0, 3, 2, 1 };
            ModuleTrackerTemplate.UnloadModule(null, null);

            ModuleTrackerTemplate.HitsArray = new[] { 0, 1, 2, 3 };
            ModuleTrackerTemplate.UnloadModule(null, null);

            var expectedHitsArray = new[] { 0, 4, 4, 4 };
            Assert.Equal(expectedHitsArray, ReadHitsFile());
        }
 
        [Fact]
        public async void MutexBlocksMultipleWriters()
        {
            using (var mutex = new Mutex(
                true, Path.GetFileNameWithoutExtension(ModuleTrackerTemplate.HitsFilePath) + "_Mutex", out bool createdNew))
            {
                Assert.True(createdNew);

                ModuleTrackerTemplate.HitsArray = new[] { 0, 1, 2, 3 };
                var unloadTask = Task.Run(() => ModuleTrackerTemplate.UnloadModule(null, null));

                Assert.False(unloadTask.Wait(5));

                WriteHitsFile(new[] { 0, 3, 2, 1 });

                Assert.False(unloadTask.Wait(5));

                mutex.ReleaseMutex();
                await unloadTask;

                var expectedHitsArray = new[] { 0, 4, 4, 4 };
                Assert.Equal(expectedHitsArray, ReadHitsFile());
            }
        }

        private void WriteHitsFile(int[] hitsArray)
        {
            using (var fs = new FileStream(ModuleTrackerTemplate.HitsFilePath, FileMode.Create))
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
            using (var fs = new FileStream(ModuleTrackerTemplate.HitsFilePath, FileMode.Open))
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
