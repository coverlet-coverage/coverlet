// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Coverlet.Core.Instrumentation;
using Xunit;

namespace Coverlet.Core.Tests.Instrumentation
{
  class TrackerContext : IDisposable
  {
    private bool _disposed;

    public TrackerContext()
    {
      ModuleTrackerTemplate.HitsFilePath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
      ModuleTrackerTemplate.FlushHitFile = true;
    }

    protected virtual void Dispose(bool disposing)
    {
      if (!_disposed)
      {
        if (disposing)
        {
          File.Delete(ModuleTrackerTemplate.HitsFilePath);
          File.Delete(ModuleTrackerTemplate.HitsFilePath + ".tmp");
          File.Delete(ModuleTrackerTemplate.HitsFilePath + ".lock");
        }

        // Dispose unmanaged resources
        AppDomain.CurrentDomain.ProcessExit -= ModuleTrackerTemplate.UnloadModule;
        AppDomain.CurrentDomain.DomainUnload -= ModuleTrackerTemplate.UnloadModule;

        _disposed = true;
      }
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }
  }

  public class ModuleTrackerTemplateTests : ExternalProcessExecutionTest
  {
    private static readonly Task<int> s_success = Task.FromResult(0);

    [Fact]
    public void HitsFileCorrectlyWritten()
    {
      FunctionExecutor.Run(() =>
      {
        using var ctx = new TrackerContext();
        ModuleTrackerTemplate.HitsArray = [1, 2, 0, 3];
        ModuleTrackerTemplate.UnloadModule(null, null);

        int[] expectedHitsArray = [1, 2, 0, 3];
        Assert.Equal(expectedHitsArray, ReadHitsFile());

        return s_success;
      });
    }

    [Fact]
    public void HitsFileWithDifferentNumberOfEntriesCausesExceptionOnUnload()
    {
      FunctionExecutor.Run(() =>
      {
        using var ctx = new TrackerContext();
        WriteHitsFile([1, 2, 3]);
        ModuleTrackerTemplate.HitsArray = [1];
        Assert.Throws<InvalidOperationException>(() => ModuleTrackerTemplate.UnloadModule(null, null));
        return s_success;
      });
    }

    [Fact]
    public void HitsOnMultipleThreadsCorrectlyCounted()
    {
      FunctionExecutor.Run(() =>
      {
        var threads = new List<Thread>();
        using var ctx = new TrackerContext();
        ModuleTrackerTemplate.HitsArray = [0, 0, 0, 0];
        for (int i = 0; i < ModuleTrackerTemplate.HitsArray.Length; ++i)
        {
          var t = new Thread(HitIndex);
          threads.Add(t);
          t.Start(i);
        }

        foreach (Thread t in threads)
        {
          t.Join();
        }

        ModuleTrackerTemplate.UnloadModule(null, null);
        int[] expectedHitsArray = [4, 3, 2, 1];
        Assert.Equal(expectedHitsArray, ReadHitsFile());

        static void HitIndex(object index)
        {
          int hitIndex = (int)index;
          for (int i = 0; i <= hitIndex; ++i)
          {
            ModuleTrackerTemplate.RecordHit(i);
          }
        }

        return s_success;
      });
    }

    [Fact]
    public void MultipleSequentialUnloadsHaveCorrectTotalData()
    {
      FunctionExecutor.Run(() =>
      {
        using var ctx = new TrackerContext();
        ModuleTrackerTemplate.HitsArray = [0, 3, 2, 1];
        ModuleTrackerTemplate.UnloadModule(null, null);

        // Each AppDomain has its own copy of ModuleTrackerTemplate with FlushHitFile = true.
        // Reset the flag to simulate a second AppDomain's unload.
        ModuleTrackerTemplate.FlushHitFile = true;
        ModuleTrackerTemplate.HitsArray = [0, 1, 2, 3];
        ModuleTrackerTemplate.UnloadModule(null, null);

        int[] expectedHitsArray = [0, 4, 4, 4];
        Assert.Equal(expectedHitsArray, ReadHitsFile());

        return s_success;
      });
    }

    [Fact]
    public void RegisterUnloadEventsPopulatesRegistry()
    {
      // Regression test for Fix 1 in issue #1983: RegisterUnloadEvents must record the module's
      // UnloadModule delegate so the in-proc collector can flush all hit files.
      FunctionExecutor.Run(() =>
      {
        using var ctx = new TrackerContext();
        ModuleTrackerTemplate.HitsArray = [3, 1, 4];

        // Simulate Initialize pre-creating the bag, then module load calling RegisterUnloadEvents.
        var bag = new ConcurrentBag<EventHandler>();
        AppDomain.CurrentDomain.SetData(ModuleTrackerTemplate.ModuleTrackerRegistryKey, bag);
        ModuleTrackerTemplate.RegisterUnloadEvents();

        EventHandler handler = Assert.Single(bag);
        handler.Invoke(null, EventArgs.Empty);
        int[] expectedHitsArray = [3, 1, 4];
        Assert.Equal(expectedHitsArray, ReadHitsFile());

        return s_success;
      });
    }

    [Fact]
    public void FlushHitFileClearedInsideMutexPreventsDoubleWrite()
    {
      // Regression test for Fix 3 in issue #1983: FlushHitFile must be cleared inside the mutex so a
      // concurrent ProcessExit caller that was waiting for the lock sees false and skips the
      // write.
      FunctionExecutor.Run(() =>
      {
        using var ctx = new TrackerContext();
        ModuleTrackerTemplate.HitsArray = [1, 2, 3];

        ModuleTrackerTemplate.UnloadModule(null, null);

        // FlushHitFile must be false inside the mutex, not after it is released.
        Assert.False(ModuleTrackerTemplate.FlushHitFile);

        // Second call simulates ProcessExit in the old TOCTOU window; must be a no-op.
        ModuleTrackerTemplate.UnloadModule(null, null);

        int[] expectedHitsArray = [1, 2, 3];
        Assert.Equal(expectedHitsArray, ReadHitsFile());

        return s_success;
      });
    }

    [Fact]
    public void HitsFileWrittenAtomicallyLeavesNoTempFile()
    {
      // Regression test for Fix 2 in issue #1983: UnloadModule must write via a temp file and rename
      // so the hit file appears at HitsFilePath only once it is complete. No .tmp residual should
      // remain after a successful flush.
      FunctionExecutor.Run(() =>
      {
        using var ctx = new TrackerContext();
        ModuleTrackerTemplate.HitsArray = [1, 2, 3];
        ModuleTrackerTemplate.UnloadModule(null, null);

        Assert.True(File.Exists(ModuleTrackerTemplate.HitsFilePath));
        Assert.False(File.Exists(ModuleTrackerTemplate.HitsFilePath + ".tmp"));
        int[] expectedHitsArray = [1, 2, 3];
        Assert.Equal(expectedHitsArray, ReadHitsFile());

        return s_success;
      });
    }

    [Fact]
    public void MutexBlocksMultipleWriters()
    {
      FunctionExecutor.Run(async () =>
      {
        using var ctx = new TrackerContext();
        using var mutex = new Mutex(
              true, Path.GetFileNameWithoutExtension(ModuleTrackerTemplate.HitsFilePath) + "_Mutex", out bool createdNew);
        Assert.True(createdNew);

        ModuleTrackerTemplate.HitsArray = [0, 1, 2, 3];
        var unloadTask = Task.Run(() => ModuleTrackerTemplate.UnloadModule(null, null));

#pragma warning disable xUnit1031 // Do not use blocking task operations in test method
        Assert.False(unloadTask.Wait(5));
#pragma warning restore xUnit1031 // Do not use blocking task operations in test method

        WriteHitsFile([0, 3, 2, 1]);

#pragma warning disable xUnit1031 // Do not use blocking task operations in test method
        Assert.False(unloadTask.Wait(5));
#pragma warning restore xUnit1031 // Do not use blocking task operations in test method

        mutex.ReleaseMutex();
        await unloadTask;

        int[] expectedHitsArray = [0, 4, 4, 4];
        Assert.Equal(expectedHitsArray, ReadHitsFile());

        return 0;
      });

    }

    [Fact]
    public void LockFileHeldDuringWriteAndReleasedAfter()
    {
      FunctionExecutor.Run(() =>
      {
        using var ctx = new TrackerContext();
        string lockPath = ModuleTrackerTemplate.HitsFilePath + ".lock";

        ModuleTrackerTemplate.HitsArray = [1, 2, 3];
        ModuleTrackerTemplate.UnloadModule(null, null);

        // After a completed write the lock file must have been released.
        // A successful exclusive open (FileShare.None) confirms no one holds it.
        using var probe = new FileStream(lockPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        Assert.True(probe.CanRead);

        return s_success;
      });
    }

    private static void WriteHitsFile(int[] hitsArray)
    {
      using var fs = new FileStream(ModuleTrackerTemplate.HitsFilePath, FileMode.Create);
      using var bw = new BinaryWriter(fs);
      bw.Write(hitsArray.Length);
      foreach (int hitCount in hitsArray)
      {
        bw.Write(hitCount);
      }
    }

    private static int[] ReadHitsFile()
    {
      using var fs = new FileStream(ModuleTrackerTemplate.HitsFilePath, FileMode.Open);
      using var br = new BinaryReader(fs);
      int[] hitsArray = new int[br.ReadInt32()];
      for (int i = 0; i < hitsArray.Length; ++i)
      {
        hitsArray[i] = br.ReadInt32();
      }

      return hitsArray;
    }
  }
}
