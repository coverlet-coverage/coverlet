using Coverlet.Core.Instrumentation;
using Coverlet.Core.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Coverlet.Core.Tests.Instrumentation
{
    public class ModuleTrackerTemplateTestsFixture : IDisposable
    {
        public void Dispose()
        {
            AppDomain.CurrentDomain.ProcessExit -= ModuleTrackerTemplate.UnloadModule;
            AppDomain.CurrentDomain.DomainUnload -= ModuleTrackerTemplate.UnloadModule;
        }
    }

    [CollectionDefinition(nameof(ModuleTrackerTemplate))]
    public class ModuleTrackerTemplateCollection : ICollectionFixture<ModuleTrackerTemplateTestsFixture>
    {

    }

    [Collection(nameof(ModuleTrackerTemplate))]
    public class ModuleTrackerTemplateTests : IDisposable
    {
        private readonly MemoryMappedFile _mmap;

        public ModuleTrackerTemplateTests()
        {
            ModuleTrackerTemplate.HitsArray = new int[4 + ModuleTrackerTemplate.HitsResultHeaderSize];
            ModuleTrackerTemplate.HitsMemoryMapName = Guid.NewGuid().ToString();
            ModuleTrackerTemplate.HitsFilePath = Path.Combine(Path.GetTempPath(), $"coverlet.test_{ModuleTrackerTemplate.HitsMemoryMapName}");

            var size = (ModuleTrackerTemplate.HitsArray.Length + ModuleTrackerTemplate.HitsResultHeaderSize) * sizeof(int);

            try
            {
                _mmap = MemoryMappedFile.CreateNew(ModuleTrackerTemplate.HitsMemoryMapName, size);
            } 
            catch (PlatformNotSupportedException)
            {
                _mmap = MemoryMappedFile.CreateFromFile(ModuleTrackerTemplate.HitsFilePath, FileMode.CreateNew, null, size);
            }
        }

        public void Dispose()
        {
            var hitsFilePath = ModuleTrackerTemplate.HitsFilePath;
            _mmap.Dispose();
            InstrumentationHelper.DeleteHitsFile(hitsFilePath);
        }

        [Fact]
        public void HitsFileCorrectlyWritten()
        {
            RecordHits(1, 2, 0, 3);
            ModuleTrackerTemplate.UnloadModule(null, null);

            var expectedHitsArray = new[] { 1, 2, 0, 3 };
            Assert.Equal(expectedHitsArray, ReadHits());
        }

        [Fact]
        public void HitsOnMultipleThreadsCorrectlyCounted()
        {
            for (int i = 0; i < 4; ++i)
            {
                var t = new Thread(HitIndex);
                t.Start(i);
                t.Join();
            }

            ModuleTrackerTemplate.UnloadModule(null, null);
            var expectedHitsArray = new[] { 4, 3, 2, 1 };
            Assert.Equal(expectedHitsArray, ReadHits());

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
            RecordHits(0, 3, 2, 1);
            ModuleTrackerTemplate.UnloadModule(null, null);

            RecordHits(0, 1, 2, 3);
            ModuleTrackerTemplate.UnloadModule(null, null);

            var expectedHitsArray = new[] { 0, 4, 4, 4 };
            Assert.Equal(expectedHitsArray, ReadHits(2));
        }
 
        [Fact]
        public async void MutexBlocksMultipleWriters()
        {
            using (var mutex = new Mutex(
                true, Path.GetFileNameWithoutExtension(ModuleTrackerTemplate.HitsMemoryMapName) + "_Mutex", out bool createdNew))
            {
                Assert.True(createdNew);

                RecordHits(0, 1, 2, 3);
                var unloadTask = Task.Run(() => ModuleTrackerTemplate.UnloadModule(null, null));

                Assert.False(unloadTask.Wait(5));

                var expectedHitsArray = new[] { 0, 0, 0, 0 };
                Assert.Equal(expectedHitsArray, ReadHits(0));

                mutex.ReleaseMutex();
                await unloadTask;

                expectedHitsArray = new[] { 0, 1, 2, 3 };
                Assert.Equal(expectedHitsArray, ReadHits());
            }
        }

        private void RecordHits(params int[] hitCounts)
        {
            // Since the hit array is held in a thread local member that is
            // then dropped by UnloadModule the hit counting must be done
            // in a new thread for each test

            Assert.Equal(ModuleTrackerTemplate.HitsArray.Length, hitCounts.Length + ModuleTrackerTemplate.HitsResultHeaderSize);

            var thread = new Thread(() =>
            {
                for (var i = 0; i < hitCounts.Length; i++)
                {
                    var count = hitCounts[i];
                    while (count-- > 0)
                    {
                        ModuleTrackerTemplate.RecordHit(i);
                    }
                }
            });
            thread.Start();
            thread.Join();
        }

        private int[] ReadHits(int expectedUnloads = 1)
        {
            var mmapAccessor = _mmap.CreateViewAccessor();

            var unloadStarted = mmapAccessor.ReadInt32(ModuleTrackerTemplate.HitsResultUnloadStarted * sizeof(int));
            var unloadFinished = mmapAccessor.ReadInt32(ModuleTrackerTemplate.HitsResultUnloadFinished * sizeof(int));

            Assert.Equal(expectedUnloads, unloadStarted);
            Assert.Equal(expectedUnloads, unloadFinished);

            var hits = new List<int>();

            for (int i = ModuleTrackerTemplate.HitsResultHeaderSize; i < ModuleTrackerTemplate.HitsArray.Length; ++i)
            {
                hits.Add(mmapAccessor.ReadInt32(i * sizeof(int)));
            }

            return hits.ToArray();
        }
    }
}
