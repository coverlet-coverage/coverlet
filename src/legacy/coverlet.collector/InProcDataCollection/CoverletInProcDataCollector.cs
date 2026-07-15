// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using Coverlet.Collector.Utilities;
using Coverlet.Core.Instrumentation;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollector.InProcDataCollector;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.InProcDataCollector;

namespace Coverlet.Collector.DataCollection
{
  public class CoverletInProcDataCollector : InProcDataCollection
  {
    private TestPlatformEqtTrace _eqtTrace;
    private bool _enableExceptionLog;
    private bool _disableInprocFlush;

    private void AttachDebugger()
    {
      if (int.TryParse(Environment.GetEnvironmentVariable("COVERLET_DATACOLLECTOR_INPROC_DEBUG"), out int result) && result == 1)
      {
        Debugger.Launch();
        Debugger.Break();
      }
    }

    private void EnableExceptionLog()
    {
      if (int.TryParse(Environment.GetEnvironmentVariable("COVERLET_DATACOLLECTOR_INPROC_EXCEPTIONLOG_ENABLED"), out int result) && result == 1)
      {
        _enableExceptionLog = true;
      }
    }

    private void DisableInprocFlush()
    {
      if (int.TryParse(Environment.GetEnvironmentVariable("COVERLET_DATACOLLECTOR_INPROC_FLUSH_DISABLED"), out int result) && result == 1)
      {
        _disableInprocFlush = true;
      }
    }

    public void Initialize(IDataCollectionSink dataCollectionSink)
    {
      AttachDebugger();
      EnableExceptionLog();
      DisableInprocFlush();

      _eqtTrace = new TestPlatformEqtTrace();
      _eqtTrace.Verbose("Initialize CoverletInProcDataCollector");

      // Pre-create the registry bag before any instrumented assembly is loaded.
      AppDomain.CurrentDomain.SetData(
          ModuleTrackerTemplate.ModuleTrackerRegistryKey,
          new ConcurrentBag<EventHandler>());
    }

    public void TestCaseEnd(TestCaseEndArgs testCaseEndArgs)
    {
    }

    public void TestCaseStart(TestCaseStartArgs testCaseStartArgs)
    {
    }

    public void TestSessionEnd(TestSessionEndArgs testSessionEndArgs)
    {
      if (_disableInprocFlush)
      {
        _eqtTrace.Verbose("COVERLET_DATACOLLECTOR_INPROC_FLUSH_DISABLED is set; skipping in-proc flush, hits will be written by process-exit handlers");
        return;
      }

      // Use the AppDomain registry populated by RegisterUnloadEvents at module-load time.
      var registeredHandlers = (ConcurrentBag<EventHandler>)AppDomain.CurrentDomain.GetData(ModuleTrackerTemplate.ModuleTrackerRegistryKey);
      if (registeredHandlers is null)
        return;

      foreach (EventHandler handler in registeredHandlers)
      {
        string assemblyName = handler.Method?.DeclaringType?.Assembly?.FullName ?? "(unknown)";
        try
        {
          _eqtTrace.Verbose($"Calling ModuleTrackerTemplate.UnloadModule for '{assemblyName}'");
          handler.Invoke(this, EventArgs.Empty);
          _eqtTrace.Verbose($"Called ModuleTrackerTemplate.UnloadModule for '{assemblyName}'");
        }
        catch (Exception ex)
        {
          _eqtTrace.Error("{0}: Failed to unload module '{1}' with error: {2}", CoverletConstants.InProcDataCollectorName, assemblyName, ex);
          if (_enableExceptionLog)
            throw new CoverletDataCollectorException($"{CoverletConstants.InProcDataCollectorName}: Failed to unload module", ex);
        }
      }
    }

    public void TestSessionStart(TestSessionStartArgs testSessionStartArgs)
    {
    }

  }
}
