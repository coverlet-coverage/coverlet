// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
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
    public TrackerContext()
    {
      ModuleTrackerTemplate.HitsFilePath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
      ModuleTrackerTemplate.FlushHitFile = true;
    }

    public void Dispose()
    {
      File.Delete(ModuleTrackerTemplate.HitsFilePath);
      AppDomain.CurrentDomain.ProcessExit -= ModuleTrackerTemplate.UnloadModule;
      AppDomain.CurrentDomain.DomainUnload -= ModuleTrackerTemplate.UnloadModule;
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

        ModuleTrackerTemplate.HitsArray = [0, 1, 2, 3];
        ModuleTrackerTemplate.UnloadModule(null, null);

        int[] expectedHitsArray = [0, 4, 4, 4];
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
