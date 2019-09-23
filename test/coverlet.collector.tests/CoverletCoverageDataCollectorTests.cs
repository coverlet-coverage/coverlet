using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
using Moq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Coverlet.Core;
using Coverlet.Collector.Utilities.Interfaces;
using Coverlet.Collector.Utilities;
using Xunit;
using Coverlet.Collector.DataCollection;
using Coverlet.Core.Reporters;
using Coverlet.Core.Abstracts;

namespace Coverlet.Collector.Tests
{
    public class CoverletCoverageDataCollectorTests
    {
        private DataCollectionEnvironmentContext _context;
        private CoverletCoverageCollector _coverletCoverageDataCollector;
        private DataCollectionContext _dataCollectionContext;
        private Mock<DataCollectionEvents> _mockDataColectionEvents;
        private Mock<DataCollectionSink> _mockDataCollectionSink;
        private Mock<ICoverageWrapper> _mockCoverageWrapper;
        private Mock<ICountDownEventFactory> _mockCountDownEventFactory;
        private XmlElement _configurationElement;
        private Mock<DataCollectionLogger> _mockLogger;

        public CoverletCoverageDataCollectorTests()
        {
            _mockDataColectionEvents = new Mock<DataCollectionEvents>();
            _mockDataCollectionSink = new Mock<DataCollectionSink>();
            _mockLogger = new Mock<DataCollectionLogger>();
            _configurationElement = null;

            TestCase testcase = new TestCase { Id = Guid.NewGuid() };
            _dataCollectionContext = new DataCollectionContext(testcase);
            _context = new DataCollectionEnvironmentContext(_dataCollectionContext);
            _mockCoverageWrapper = new Mock<ICoverageWrapper>();
            _mockCountDownEventFactory = new Mock<ICountDownEventFactory>();
        }

        [Fact]
        public void OnSessionStartShouldInitializeCoverageWithCorrectCoverletSettings()
        {
            _coverletCoverageDataCollector = new CoverletCoverageCollector(new TestPlatformEqtTrace(), _mockCoverageWrapper.Object, _mockCountDownEventFactory.Object);
            _coverletCoverageDataCollector.Initialize(
                    _configurationElement,
                    _mockDataColectionEvents.Object,
                    _mockDataCollectionSink.Object,
                    _mockLogger.Object,
                    _context);
            IDictionary<string, object> sessionStartProperties = new Dictionary<string, object>();

            sessionStartProperties.Add("TestSources", new List<string> { "abc.dll" });

            _mockDataColectionEvents.Raise(x => x.SessionStart += null, new SessionStartEventArgs(sessionStartProperties));

            _mockCoverageWrapper.Verify(x => x.CreateCoverage(It.Is<CoverletSettings>(y => string.Equals(y.TestModule, "abc.dll")), It.IsAny<ILogger>()), Times.Once);
        }

        [Fact]
        public void OnSessionStartShouldPrepareModulesForCoverage()
        {
            _coverletCoverageDataCollector = new CoverletCoverageCollector(new TestPlatformEqtTrace(), _mockCoverageWrapper.Object, _mockCountDownEventFactory.Object);
            _coverletCoverageDataCollector.Initialize(
                    _configurationElement,
                    _mockDataColectionEvents.Object,
                    _mockDataCollectionSink.Object,
                    null,
                    _context);
            IDictionary<string, object> sessionStartProperties = new Dictionary<string, object>();
            Coverage coverage = new Coverage("abc.dll", null, null, null, null, null, true, true, "abc.json", true, It.IsAny<ILogger>(), (IInstrumentationHelper)DependencyInjection.Current.GetService(typeof(IInstrumentationHelper)), (IFileSystem)DependencyInjection.Current.GetService(typeof(IFileSystem)));

            sessionStartProperties.Add("TestSources", new List<string> { "abc.dll" });
            _mockCoverageWrapper.Setup(x => x.CreateCoverage(It.IsAny<CoverletSettings>(), It.IsAny<ILogger>())).Returns(coverage);

            _mockDataColectionEvents.Raise(x => x.SessionStart += null, new SessionStartEventArgs(sessionStartProperties));

            _mockCoverageWrapper.Verify(x => x.CreateCoverage(It.Is<CoverletSettings>(y => y.TestModule.Contains("abc.dll")), It.IsAny<ILogger>()), Times.Once);
            _mockCoverageWrapper.Verify(x => x.PrepareModules(It.IsAny<Coverage>()), Times.Once);
        }

        [Fact]
        public void OnSessionEndShouldSendGetCoverageReportToTestPlatform()
        {
            _coverletCoverageDataCollector = new CoverletCoverageCollector(new TestPlatformEqtTrace(), new CoverageWrapper(), _mockCountDownEventFactory.Object);
            _coverletCoverageDataCollector.Initialize(
                    _configurationElement,
                    _mockDataColectionEvents.Object,
                    _mockDataCollectionSink.Object,
                    _mockLogger.Object,
                    _context);

            string module = GetType().Assembly.Location;
            string pdb = Path.Combine(Path.GetDirectoryName(module), Path.GetFileNameWithoutExtension(module) + ".pdb");

            var directory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));

            File.Copy(module, Path.Combine(directory.FullName, Path.GetFileName(module)), true);
            File.Copy(pdb, Path.Combine(directory.FullName, Path.GetFileName(pdb)), true);

            IDictionary<string, object> sessionStartProperties = new Dictionary<string, object>();
            sessionStartProperties.Add("TestSources", new List<string> { Path.Combine(directory.FullName, Path.GetFileName(module)) });

            _mockDataColectionEvents.Raise(x => x.SessionStart += null, new SessionStartEventArgs(sessionStartProperties));
            _mockDataColectionEvents.Raise(x => x.SessionEnd += null, new SessionEndEventArgs());

            _mockDataCollectionSink.Verify(x => x.SendFileAsync(It.IsAny<FileTransferInformation>()), Times.Once);

            directory.Delete(true);
        }

        [Theory]
        [InlineData("noValidFormat", 0)]
        [InlineData("json,cobertura", 2)]
        [InlineData("json,cobertura,lcov", 3)]
        public void OnSessionEndShouldSendCoverageReportsForMultipleFormatsToTestPlatform(string formats, int sendReportsCount)
        {
            _coverletCoverageDataCollector = new CoverletCoverageCollector(new TestPlatformEqtTrace(), new CoverageWrapper(), _mockCountDownEventFactory.Object);

            IList<IReporter> reporters = formats.Split(',').Select(f => new ReporterFactory(f).CreateReporter()).Where(x => x != null).ToList();
            Mock<DataCollectionSink> mockDataCollectionSink = new Mock<DataCollectionSink>();
            mockDataCollectionSink.Setup(m => m.SendFileAsync(It.IsAny<FileTransferInformation>())).Callback<FileTransferInformation>(fti =>
            {
                reporters.Remove(reporters.First(x =>
                    Path.GetFileName(fti.Path) == Path.ChangeExtension(CoverletConstants.DefaultFileName, x.Extension))
                );
            });

            var doc = new XmlDocument();
            var root = doc.CreateElement("Configuration");
            var element = doc.CreateElement("Format");
            element.AppendChild(doc.CreateTextNode(formats));
            root.AppendChild(element);

            _configurationElement = root;

            _coverletCoverageDataCollector.Initialize(
                _configurationElement,
                _mockDataColectionEvents.Object,
                mockDataCollectionSink.Object,
                _mockLogger.Object,
                _context);

            var sessionStartProperties = new Dictionary<string, object> { { "TestSources", new List<string> { "Test" } } };

            _mockDataColectionEvents.Raise(x => x.SessionStart += null, new SessionStartEventArgs(sessionStartProperties));
            _mockDataColectionEvents.Raise(x => x.SessionEnd += null, new SessionEndEventArgs());

            mockDataCollectionSink.Verify(x => x.SendFileAsync(It.IsAny<FileTransferInformation>()), Times.Exactly(sendReportsCount));
            Assert.Empty(reporters);
        }

        [Fact]
        public void OnSessionStartShouldLogWarningIfInstrumentationFailed()
        {
            _coverletCoverageDataCollector = new CoverletCoverageCollector(new TestPlatformEqtTrace(), _mockCoverageWrapper.Object, _mockCountDownEventFactory.Object);
            _coverletCoverageDataCollector.Initialize(
                    _configurationElement,
                    _mockDataColectionEvents.Object,
                    _mockDataCollectionSink.Object,
                    _mockLogger.Object,
                    _context);
            IDictionary<string, object> sessionStartProperties = new Dictionary<string, object>();

            sessionStartProperties.Add("TestSources", new List<string> { "abc.dll" });

            _mockCoverageWrapper.Setup(x => x.PrepareModules(It.IsAny<Coverage>())).Throws(new FileNotFoundException());

            _mockDataColectionEvents.Raise(x => x.SessionStart += null, new SessionStartEventArgs(sessionStartProperties));

            _mockLogger.Verify(x => x.LogWarning(_dataCollectionContext,
                It.Is<string>(y => y.Contains("CoverletDataCollectorException"))));
        }
    }
}
