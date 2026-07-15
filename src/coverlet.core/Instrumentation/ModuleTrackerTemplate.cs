// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Coverlet.Core.Instrumentation
{
  /// <summary>
  /// This static class will be injected on a module being instrumented in order to direct on module hits
  /// to a single location.
  /// </summary>
  /// <remarks>
  /// As this type is going to be customized for each instrumented module it doesn't follow typical practices
  /// regarding visibility of members, etc.
  /// </remarks>
  [CompilerGenerated]
  [ExcludeFromCodeCoverage]
  internal static class ModuleTrackerTemplate
  {
    /// <summary>
    /// AppDomain data key under which a <see cref="ConcurrentBag{T}"/> of <see cref="EventHandler"/>
    /// delegates, one per loaded instrumented module, is stored for use by the in-proc collector.
    /// </summary>
    internal const string ModuleTrackerRegistryKey = "Coverlet_RegisteredModuleTrackers";

    public static string HitsFilePath;
    public static int[] HitsArray;
    public static bool SingleHit;
    public static bool FlushHitFile;
    private static readonly bool s_enableLog = int.TryParse(Environment.GetEnvironmentVariable("COVERLET_ENABLETRACKERLOG"), out int result) && result == 1;
    private static readonly string s_sessionId = Guid.NewGuid().ToString();

    static ModuleTrackerTemplate()
    {
      // At the end of the instrumentation of a module, the instrumenter needs to add code here
      // to initialize the static fields according to the values derived from the instrumentation of
      // the module.
    }

    // A call to this method will be injected in the static constructor above for most cases. However, if the
    // current assembly is System.Private.CoreLib (or more specifically, defines System.AppDomain), a call directly
    // to UnloadModule will be injected in System.AppContext.OnProcessExit.
    public static void RegisterUnloadEvents()
    {
      // HitsFilePath is already set before this call (the injected static constructor initialises
      // all fields before calling RegisterUnloadEvents). If no in-proc collector created the
      // registry (MSBuild / global-tool integration), GetData returns null and the Add is a
      // deliberate no-op; the ProcessExit handler covers the flush in those paths.
      var registry = (ConcurrentBag<EventHandler>)AppDomain.CurrentDomain.GetData(ModuleTrackerRegistryKey);
      registry?.Add(new EventHandler(UnloadModule));

      AppDomain.CurrentDomain.ProcessExit += new EventHandler(UnloadModule);
      AppDomain.CurrentDomain.DomainUnload += new EventHandler(UnloadModule);
    }

    public static void RecordHitInCoreLibrary(int hitLocationIndex)
    {
      // Make sure to avoid recording if this is a call to RecordHit within the AppDomain setup code in an
      // instrumented build of System.Private.CoreLib.
      if (HitsArray is null)
        return;

      Interlocked.Increment(ref HitsArray[hitLocationIndex]);
    }

    public static void RecordHit(int hitLocationIndex)
    {
      Interlocked.Increment(ref HitsArray[hitLocationIndex]);
    }

    public static void RecordSingleHitInCoreLibrary(int hitLocationIndex)
    {
      // Make sure to avoid recording if this is a call to RecordHit within the AppDomain setup code in an
      // instrumented build of System.Private.CoreLib.
      if (HitsArray is null)
        return;

      ref int location = ref HitsArray[hitLocationIndex];
      if (location == 0)
        location = 1;
    }

    public static void RecordSingleHit(int hitLocationIndex)
    {
      ref int location = ref HitsArray[hitLocationIndex];
      if (location == 0)
        location = 1;
    }

    public static void UnloadModule(object sender, EventArgs e)
    {
      // The same module can be unloaded concurrently (different AppDomains, or ProcessExit racing an
      // in-proc-collector call). A global mutex serialises access; FlushHitFile is cleared inside the
      // lock so a waiting caller sees the updated value and skips a redundant write.
      using var mutex = new Mutex(true, Path.GetFileNameWithoutExtension(HitsFilePath) + "_Mutex", out bool createdNew);
      if (!createdNew)
      {
        mutex.WaitOne();
      }

      // Hold the lock file exclusively for the entire write so the out-of-proc reader can detect
      // a write in progress and wait, or detect a crashed writer (the OS releases all file handles
      // on process death, making the lock visible to readers immediately).
      // A reader may briefly hold the file shared; retry a handful of times before giving up.
      using (FileStream lockFile = TryAcquireExclusiveLockOnFile(HitsFilePath + ".lock"))
      {
        if (lockFile == null)
          throw new InvalidOperationException($"Failed to acquire lock file for '{HitsFilePath}' after retries.");

        if (FlushHitFile)
        {
          try
          {
            // Claim the current hits array and reset it to prevent double-counting scenarios.
            int[] hitsArray = Interlocked.Exchange(ref HitsArray, new int[HitsArray.Length]);

            WriteLog($"Unload called for '{Assembly.GetExecutingAssembly().Location}' by '{sender ?? "null"}'");
            WriteLog($"Flushing hit file '{HitsFilePath}'");

            string tmpFilePath = HitsFilePath + ".tmp";
            if (File.Exists(HitsFilePath))
            {
              // Another AppDomain already wrote HitsFilePath (only possible on .NET Framework).
              // Read it, compute the merged result, then replace atomically.
              int[] mergedHits = MergeHitsWithExistingFile(hitsArray);
              WriteHitsToFile(tmpFilePath, mergedHits);
              File.Replace(tmpFilePath, HitsFilePath, null);
            }
            else
            {
              // First write: stage in .tmp so HitsFilePath only appears once the data is complete.
              WriteHitsToFile(tmpFilePath, hitsArray);
              File.Move(tmpFilePath, HitsFilePath);
            }

            WriteHits(sender);

            WriteLog($"Hit file '{HitsFilePath}' flushed, size {new FileInfo(HitsFilePath).Length}");
            WriteLog("--------------------------------");
          }
          catch (Exception ex)
          {
            WriteLog(ex.ToString());
            throw;
          }

          // Clear the flag inside the mutex so that any concurrent caller (e.g. ProcessExit)
          // that is waiting for the mutex will see FlushHitFile == false and skip a redundant write.
          FlushHitFile = false;
        }
      }

      // On purpose this is not under a try-finally: it is better to have an exception if there was any error writing the hits file
      // this case is relevant when instrumenting corelib since multiple processes can be running against the same instrumented dll.
      mutex.ReleaseMutex();
    }

    private static FileStream TryAcquireExclusiveLockOnFile(string lockFilePath)
    {
      for (int i = 0; i < 25; i++)
      {
        try
        {
          return new FileStream(lockFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        }
        catch (IOException)
        {
          Thread.Sleep(20);
        }
      }
      return null;
    }

    private static void WriteHitsToFile(string filePath, int[] hitsArray)
    {
      using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
      using var bw = new BinaryWriter(fs);
      bw.Write(hitsArray.Length);
      foreach (int hitCount in hitsArray)
      {
        bw.Write(hitCount);
      }
    }

    private static int[] MergeHitsWithExistingFile(int[] inMemoryHits)
    {
      using var fs = new FileStream(HitsFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
      using var br = new BinaryReader(fs);
      int hitsLength = br.ReadInt32();
      WriteLog($"Current hits found '{hitsLength}'");

      if (hitsLength != inMemoryHits.Length)
        throw new InvalidOperationException($"{HitsFilePath} has {hitsLength} entries but in-memory {nameof(HitsArray)} has {inMemoryHits.Length}");

      int[] merged = new int[hitsLength];
      for (int i = 0; i < hitsLength; ++i)
      {
        int existing = br.ReadInt32();
        merged[i] = SingleHit
          ? inMemoryHits[i] + existing > 0 ? 1 : 0
          : inMemoryHits[i] + existing;
      }
      return merged;
    }

    private static void WriteHits(object sender)
    {
      if (s_enableLog)
      {
        var currentAssembly = Assembly.GetExecutingAssembly();
        var location = new DirectoryInfo(Path.Combine(Path.GetDirectoryName(currentAssembly.Location), "TrackersHitsLog"));
        location.Create();
        string logFile = Path.Combine(location.FullName, $"{Path.GetFileName(currentAssembly.Location)}_{DateTime.UtcNow.Ticks}_{s_sessionId}.txt");
        using (var fs = new FileStream(HitsFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        using (var log = new FileStream(logFile, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
        using (var logWriter = new StreamWriter(log))
        using (var br = new BinaryReader(fs))
        {
          int hitsLength = br.ReadInt32();
          for (int i = 0; i < hitsLength; ++i)
          {
            logWriter.WriteLine($"{i},{br.ReadInt32()}");
          }
        }

        File.AppendAllText(logFile, $"Hits flushed file path {HitsFilePath} location '{Assembly.GetExecutingAssembly().Location}' by '{sender ?? "null"}'");
      }
    }

    private static void WriteLog(string logText)
    {
      if (s_enableLog)
      {
        // We don't set path as global var to keep benign possible errors inside try/catch
        // I'm not sure that location will be ok in every scenario
        string location = Assembly.GetExecutingAssembly().Location;
        File.AppendAllText(Path.Combine(Path.GetDirectoryName(location), Path.GetFileName(location) + "_tracker.txt"), $"[{DateTime.UtcNow} S:{s_sessionId} T:{Thread.CurrentThread.ManagedThreadId}]{logText}{Environment.NewLine}");
      }
    }
  }
}
