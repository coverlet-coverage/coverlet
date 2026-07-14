// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using Coverlet.Collector.DataCollection;
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
  }
}
