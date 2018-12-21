using Coverlet.Core.Instrumentation;
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
        // Prevent parallel execution of tests using the ModuleTrackerTemplate static class
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);

        public ModuleTrackerTemplateTestsFixture()
        {
            _semaphore.Wait();
            ModuleTrackerTemplate.HitsMemoryMapName = Guid.NewGuid().ToString();
        }

        public void Dispose()
        {
            AppDomain.CurrentDomain.ProcessExit -= ModuleTrackerTemplate.UnloadModule;
            AppDomain.CurrentDomain.DomainUnload -= ModuleTrackerTemplate.UnloadModule;
            _semaphore.Release();
        }
    }

    public class ModuleTrackerTemplateTests : IClassFixture<ModuleTrackerTemplateTestsFixture>, IDisposable
    {
        // Prevent parallel execution of these tests
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);

        private MemoryMappedFile _mmap;

        public ModuleTrackerTemplateTests()
        {
            _semaphore.Wait();
            _mmap = MemoryMappedFile.CreateNew(ModuleTrackerTemplate.HitsMemoryMapName, 100 * sizeof(int));
        }

        public void Dispose()
        {
            _mmap.Dispose();
            _semaphore.Release();
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
            ModuleTrackerTemplate.HitsArraySize = 4;
            for (int i = 0; i < ModuleTrackerTemplate.HitsArraySize; ++i)
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

            ModuleTrackerTemplate.HitsArraySize = hitCounts.Length;

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

            var unloadStarted = mmapAccessor.ReadInt32(Coverage.HitsResultUnloadStarted * sizeof(int));
            var unloadFinished = mmapAccessor.ReadInt32(Coverage.HitsResultUnloadFinished * sizeof(int));

            Assert.Equal(expectedUnloads, unloadStarted);
            Assert.Equal(expectedUnloads, unloadFinished);

            var hits = new List<int>();

            for (int i = 0; i < ModuleTrackerTemplate.HitsArraySize; ++i)
            {
                hits.Add(mmapAccessor.ReadInt32((i + Coverage.HitsResultHeaderSize) * sizeof(int)));
            }

            return hits.ToArray();
        }
    }
}
