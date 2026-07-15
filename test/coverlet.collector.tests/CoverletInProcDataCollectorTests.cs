// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using Coverlet.Collector.DataCollection;
using Coverlet.Collector.Utilities;
using Coverlet.Core.Instrumentation;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollector.InProcDataCollector;
using Moq;
using Xunit;

namespace Coverlet.Collector.Tests.DataCollection
{
  public class CoverletInProcDataCollectorTests : IDisposable
  {
    private readonly CoverletInProcDataCollector _dataCollector;

    public CoverletInProcDataCollectorTests()
    {
      _dataCollector = new CoverletInProcDataCollector();
      _dataCollector.Initialize(new Mock<IDataCollectionSink>().Object);
    }

    public void Dispose()
    {
      // Remove any registry entries written during the test so other tests are not affected.
      AppDomain.CurrentDomain.SetData(ModuleTrackerTemplate.ModuleTrackerRegistryKey, null);
    }

    [Fact]
    public void TestSessionEnd_UsesRegistryWhenAvailable()
    {
      // Regression test for Fix 1 in issue #1983: TestSessionEnd must invoke handlers from the AppDomain registry.
      bool handlerCalled = false;

      var bag = (ConcurrentBag<EventHandler>)AppDomain.CurrentDomain.GetData(ModuleTrackerTemplate.ModuleTrackerRegistryKey);
      bag?.Add(new((_, _) => { handlerCalled = true; }));

      _dataCollector.TestSessionEnd(new TestSessionEndArgs());

      Assert.True(handlerCalled);
    }

    [Fact]
    public void TestSessionEnd_DoesNotRethrowWhenHandlerThrows()
    {
      // Regression test for Fix 4 in issue #1983: a failing handler must be logged but must not
      // propagate by default, so coverage failures do not abort the test run.
      var bag = (ConcurrentBag<EventHandler>)AppDomain.CurrentDomain.GetData(ModuleTrackerTemplate.ModuleTrackerRegistryKey);
      bag?.Add(new((_, _) => throw new InvalidOperationException("simulated flush failure")));

      _dataCollector.TestSessionEnd(new TestSessionEndArgs());
    }

    [Fact]
    public void TestSessionEnd_SkipsFlushWhenInprocFlushDisabled()
    {
      // Regression test: COVERLET_DATACOLLECTOR_INPROC_FLUSH_DISABLED=1 must cause TestSessionEnd to skip
      // the in-proc flush entirely, leaving coverage data to be written by the process-exit handlers.
      try
      {
        Environment.SetEnvironmentVariable("COVERLET_DATACOLLECTOR_INPROC_FLUSH_DISABLED", "1");
        var collector = new CoverletInProcDataCollector();
        collector.Initialize(new Mock<IDataCollectionSink>().Object);

        bool handlerCalled = false;
        var bag = (ConcurrentBag<EventHandler>)AppDomain.CurrentDomain.GetData(ModuleTrackerTemplate.ModuleTrackerRegistryKey);
        bag?.Add(new((_, _) => { handlerCalled = true; }));

        collector.TestSessionEnd(new TestSessionEndArgs());

        Assert.False(handlerCalled);
      }
      finally
      {
        Environment.SetEnvironmentVariable("COVERLET_DATACOLLECTOR_INPROC_FLUSH_DISABLED", null);
      }
    }

    [Fact]
    public void TestSessionEnd_RethrowsWhenHandlerThrowsAndExceptionLogEnabled()
    {
      // Regression test for Fix 4 in issue #1983: with COVERLET_DATACOLLECTOR_INPROC_EXCEPTIONLOG_ENABLED=1
      // a failing handler must be rethrown so the test run can surface coverage failures explicitly.
      try
      {
        Environment.SetEnvironmentVariable("COVERLET_DATACOLLECTOR_INPROC_EXCEPTIONLOG_ENABLED", "1");
        var collector = new CoverletInProcDataCollector();
        collector.Initialize(new Mock<IDataCollectionSink>().Object);

        var bag = (ConcurrentBag<EventHandler>)AppDomain.CurrentDomain.GetData(ModuleTrackerTemplate.ModuleTrackerRegistryKey);
        bag?.Add(new((_, _) => throw new InvalidOperationException("simulated flush failure")));

        Assert.Throws<CoverletDataCollectorException>(() => collector.TestSessionEnd(new TestSessionEndArgs()));
      }
      finally
      {
        Environment.SetEnvironmentVariable("COVERLET_DATACOLLECTOR_INPROC_EXCEPTIONLOG_ENABLED", null);
      }
    }
  }
}
