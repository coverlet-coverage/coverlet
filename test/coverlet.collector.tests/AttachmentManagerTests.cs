using System;
using System.ComponentModel;
using System.IO;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
using Xunit;
using Moq;
using Coverlet.Collector.Utilities;
using Coverlet.Collector.Utilities.Interfaces;
using Coverlet.Collector.DataCollection;

namespace Coverlet.Collector.Tests
{
    public class AttachmentManagerTests
    {
        private AttachmentManager _attachmentManager;
        private Mock<DataCollectionSink> _mockDataCollectionSink;
        private DataCollectionContext _dataCollectionContext;
        private TestPlatformLogger _testPlatformLogger;
        private TestPlatformEqtTrace _eqtTrace;
        private Mock<IFileHelper> _mockFileHelper;
        private Mock<IDirectoryHelper> _mockDirectoryHelper;
        private Mock<ICountDownEvent> _mockCountDownEvent;
        private Mock<DataCollectionLogger> _mockDataCollectionLogger;

        public AttachmentManagerTests()
        {
            _mockDataCollectionSink = new Mock<DataCollectionSink>();
            _mockDataCollectionLogger = new Mock<DataCollectionLogger>();
            var testcase = new TestCase { Id = Guid.NewGuid() };
            _dataCollectionContext = new DataCollectionContext(testcase);
            _testPlatformLogger = new TestPlatformLogger(_mockDataCollectionLogger.Object, _dataCollectionContext);
            _eqtTrace = new TestPlatformEqtTrace();
            _mockFileHelper = new Mock<IFileHelper>();
            _mockDirectoryHelper = new Mock<IDirectoryHelper>();
            _mockCountDownEvent = new Mock<ICountDownEvent>();

            _attachmentManager = new AttachmentManager(_mockDataCollectionSink.Object, _dataCollectionContext, _testPlatformLogger,
                _eqtTrace, @"E:\temp", _mockFileHelper.Object, _mockDirectoryHelper.Object, _mockCountDownEvent.Object);
        }

        [Fact]
        public void SendCoverageReportShouldSaveReportToFile()
        {
            string coverageReport = "<?xml version=\"1.0\" encoding=\"utf-8\"?>"
                                    + "<coverage line-rate=\"1\" branch-rate=\"1\" version=\"1.9\" timestamp=\"1556263787\" lines-covered=\"0\" lines-valid=\"0\" branches-covered=\"0\" branches-valid=\"0\">"
                                    + "<sources/>"
                                    + "<packages/>"
                                    + "</coverage>";

            _attachmentManager.SendCoverageReport(coverageReport, "report.cobertura.xml");
            _mockFileHelper.Verify(x => x.WriteAllText(It.Is<string>(y => y.Contains(@"report.cobertura.xml")), coverageReport), Times.Once);
        }

        [Fact]
        public void SendCoverageReportShouldThrowExceptionWhenFailedToSaveReportToFile()
        {
            _attachmentManager = new AttachmentManager(_mockDataCollectionSink.Object, _dataCollectionContext, _testPlatformLogger,
               _eqtTrace, @"E:\temp", _mockFileHelper.Object, _mockDirectoryHelper.Object, _mockCountDownEvent.Object);

            string coverageReport = "<?xml version=\"1.0\" encoding=\"utf-8\"?>"
                                    + "<coverage line-rate=\"1\" branch-rate=\"1\" version=\"1.9\" timestamp=\"1556263787\" lines-covered=\"0\" lines-valid=\"0\" branches-covered=\"0\" branches-valid=\"0\">"
                                    + "<sources/>"
                                    + "<packages/>"
                                    + "</coverage>";

            string message = Assert.Throws<CoverletDataCollectorException>(() => _attachmentManager.SendCoverageReport(coverageReport, null)).Message;
            Assert.Contains("CoverletCoverageDataCollector: Failed to save coverage report", message);
        }

        [Fact]
        public void SendCoverageReportShouldSendAttachmentToTestPlatform()
        {
            var directory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));
            _attachmentManager = new AttachmentManager(_mockDataCollectionSink.Object, _dataCollectionContext, _testPlatformLogger,
               _eqtTrace, directory.ToString(), new FileHelper(), _mockDirectoryHelper.Object, _mockCountDownEvent.Object);

            string coverageReport = "<?xml version=\"1.0\" encoding=\"utf-8\"?>"
                                    + "<coverage line-rate=\"1\" branch-rate=\"1\" version=\"1.9\" timestamp=\"1556263787\" lines-covered=\"0\" lines-valid=\"0\" branches-covered=\"0\" branches-valid=\"0\">"
                                    + "<sources/>"
                                    + "<packages/>"
                                    + "</coverage>";

            _attachmentManager.SendCoverageReport(coverageReport, "report.cobertura.xml");

            _mockDataCollectionSink.Verify(x => x.SendFileAsync(It.IsAny<FileTransferInformation>()));

            directory.Delete(true);
        }

        [Fact]
        public void OnDisposeAttachmentManagerShouldCleanUpReportDirectory()
        {
            var mockDirectoryHelper = new Mock<IDirectoryHelper>();
            mockDirectoryHelper.Setup(x => x.Exists(It.Is<string>(y => y.Contains(@"E:\temp")))).Returns(true);
            using (var attachmentManager = new AttachmentManager(_mockDataCollectionSink.Object, _dataCollectionContext, _testPlatformLogger, _eqtTrace, @"E:\temp", _mockFileHelper.Object, mockDirectoryHelper.Object, _mockCountDownEvent.Object))
            {
                _mockDataCollectionSink.Raise(x => x.SendFileCompleted += null, new AsyncCompletedEventArgs(null, false, null));
            }

            mockDirectoryHelper.Verify(x => x.Delete(It.Is<string>(y => y.Contains(@"E:\temp")), true), Times.Once);
        }

        [Fact]
        public void OnDisposeAttachmentManagerShouldThrowCoverletDataCollectorExceptionIfUnableToCleanUpReportDirectory()
        {
            var mockDirectoryHelper = new Mock<IDirectoryHelper>();
            mockDirectoryHelper.Setup(x => x.Exists(It.Is<string>(y => y.Contains(@"E:\temp")))).Returns(true);
            mockDirectoryHelper.Setup(x => x.Delete(It.Is<string>(y => y.Contains(@"E:\temp")), true)).Throws(new FileNotFoundException());
            using (var attachmentManager = new AttachmentManager(_mockDataCollectionSink.Object, _dataCollectionContext, _testPlatformLogger, _eqtTrace, @"E:\temp", _mockFileHelper.Object, mockDirectoryHelper.Object, _mockCountDownEvent.Object))
            {
                _mockDataCollectionSink.Raise(x => x.SendFileCompleted += null, new AsyncCompletedEventArgs(null, false, null));
            }
            _mockDataCollectionLogger.Verify(x => x.LogWarning(_dataCollectionContext,
                It.Is<string>(y => y.Contains("CoverletDataCollectorException: CoverletCoverageDataCollector: Failed to cleanup report directory"))), Times.AtLeastOnce);
        }
    }
}
