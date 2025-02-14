// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using Coverlet.Collector.DataCollection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
using Moq;
using Xunit;

namespace Coverlet.Collector.Tests.DataCollection
{
  public class CoverletInProcDataCollectorTests
  {
    private readonly CoverletInProcDataCollector _dataCollector;

    public CoverletInProcDataCollectorTests()
    {
      _dataCollector = new CoverletInProcDataCollector();
    }

    [Fact]
    public void GetInstrumentationClass_ShouldReturnNullForNonMatchingType_EnabledLogging()
    {
      // Arrange
      var dataCollectionSink = new Mock<IDataCollectionSink>();
      var mockAssembly = new Mock<Assembly>();
      var mockType = new Mock<Type>();
      mockType.Setup(t => t.Namespace).Returns("Coverlet.Core.Instrumentation.Tracker");
      mockType.Setup(t => t.Name).Returns("MockAssembly_Tracker");
      mockAssembly.Setup(a => a.GetTypes()).Returns(new[] { mockType.Object });
      Environment.SetEnvironmentVariable("COVERLET_DATACOLLECTOR_INPROC_EXCEPTIONLOG_ENABLED", "1");
      _dataCollector.Initialize(dataCollectionSink.Object);

      // Act & Assert
      var results = _dataCollector.GetInstrumentationClass(mockAssembly.Object);

      // Assert
      Assert.Null(results);
    }

    [Fact]
    public void GetInstrumentationClass_ShouldReturnNullForNonMatchingType()
    {
      // Arrange
      var mockAssembly = new Mock<Assembly>();
      var mockType = new Mock<Type>();
      mockType.Setup(t => t.Namespace).Returns("NonMatchingNamespace");
      mockType.Setup(t => t.Name).Returns("NonMatchingName");
      mockAssembly.Setup(a => a.GetTypes()).Returns(new[] { mockType.Object });

      // Act
      var result = _dataCollector.GetInstrumentationClass(mockAssembly.Object);

      // Assert
      Assert.Null(result);
    }
  }
}
