using System;
using System.ComponentModel;
using System.IO;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
using Xunit;
using Moq;
using Coverlet.Collector.DataCollector;
using Coverlet.Collector.Utilities;
using Coverlet.Collector.Utilities.Interfaces;

namespace Coverlet.Collector.Tests
{
    public class AttachmentManagerTests
    {
        private AttachmentManager attachmentManager;
        private Mock<DataCollectionSink> mockDataCollectionSink;
        private DataCollectionContext dataCollectionContext;
        private TestPlatformLogger testPlatformLogger;
        private TestPlatformEqtTrace eqtTrace;
        private Mock<IFileHelper> mockFileHelper;
        private Mock<IDirectoryHelper> mockDirectoryHelper;
        private Mock<DataCollectionLogger> mockDataCollectionLogger;

        public AttachmentManagerTests()
        {
            this.mockDataCollectionSink = new Mock<DataCollectionSink>();
            this.mockDataCollectionLogger = new Mock<DataCollectionLogger>();
            TestCase testcase = new TestCase { Id = Guid.NewGuid() };
            this.dataCollectionContext = new DataCollectionContext(testcase);
            this.testPlatformLogger = new TestPlatformLogger(this.mockDataCollectionLogger.Object, this.dataCollectionContext);
            this.eqtTrace = new TestPlatformEqtTrace();
            this.mockFileHelper = new Mock<IFileHelper>();
            this.mockDirectoryHelper = new Mock<IDirectoryHelper>();

            this.attachmentManager = new AttachmentManager(this.mockDataCollectionSink.Object, this.dataCollectionContext, this.testPlatformLogger,
                this.eqtTrace, "report.cobertura.xml", @"E:\temp", this.mockFileHelper.Object, this.mockDirectoryHelper.Object);
        }

        [Fact]
        public void SendCoverageReportShouldSaveReportToFile()
        {
            string coverageReport = "<?xml version=\"1.0\" encoding=\"utf-8\"?>"
                                    + "<coverage line-rate=\"1\" branch-rate=\"1\" version=\"1.9\" timestamp=\"1556263787\" lines-covered=\"0\" lines-valid=\"0\" branches-covered=\"0\" branches-valid=\"0\">"
                                    + "<sources/>"
                                    + "<packages/>"
                                    + "</coverage>";

            this.attachmentManager.SendCoverageReport(coverageReport);
            this.mockFileHelper.Verify(x => x.WriteAllText(@"E:\temp\report.cobertura.xml", coverageReport), Times.Once);
        }

        [Fact]
        public void SendCoverageReportShouldThrowExceptionWhenFailedToSaveReportToFile()
        {
            this.attachmentManager = new AttachmentManager(this.mockDataCollectionSink.Object, this.dataCollectionContext, this.testPlatformLogger,
               this.eqtTrace, null, @"E:\temp", this.mockFileHelper.Object, this.mockDirectoryHelper.Object);

            string coverageReport = "<?xml version=\"1.0\" encoding=\"utf-8\"?>"
                                    + "<coverage line-rate=\"1\" branch-rate=\"1\" version=\"1.9\" timestamp=\"1556263787\" lines-covered=\"0\" lines-valid=\"0\" branches-covered=\"0\" branches-valid=\"0\">"
                                    + "<sources/>"
                                    + "<packages/>"
                                    + "</coverage>";

            var message = Assert.Throws<CoverletDataCollectorException>(() => this.attachmentManager.SendCoverageReport(coverageReport)).Message;
            Assert.Equal("CoverletCoverageDataCollector: Failed to save coverage report '' in directory 'E:\\temp'", message);
        }

        [Fact]
        public void SendCoverageReportShouldSendAttachmentToTestPlatform()
        {
            var directory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));
            this.attachmentManager = new AttachmentManager(this.mockDataCollectionSink.Object, this.dataCollectionContext, this.testPlatformLogger,
               this.eqtTrace, "report.cobertura.xml", directory.ToString(), new FileHelper(), this.mockDirectoryHelper.Object);

            string coverageReport = "<?xml version=\"1.0\" encoding=\"utf-8\"?>"
                                    + "<coverage line-rate=\"1\" branch-rate=\"1\" version=\"1.9\" timestamp=\"1556263787\" lines-covered=\"0\" lines-valid=\"0\" branches-covered=\"0\" branches-valid=\"0\">"
                                    + "<sources/>"
                                    + "<packages/>"
                                    + "</coverage>";

            this.attachmentManager.SendCoverageReport(coverageReport);

            this.mockDataCollectionSink.Verify(x => x.SendFileAsync(It.IsAny<FileTransferInformation>()));

            directory.Delete(true);
        }

        [Fact]
        public void OnSendFileCompletedShouldCleanUpReportDirectory()
        {
            this.mockDirectoryHelper.Setup(x => x.Exists(@"E:\temp")).Returns(true);

            this.mockDataCollectionSink.Raise(x => x.SendFileCompleted += null, new AsyncCompletedEventArgs(null, false, null));

            this.mockDirectoryHelper.Verify(x => x.Delete(@"E:\temp", true), Times.Once);
        }

        [Fact]
        public void OnSendFileCompletedShouldThrowCoverletDataCollectorExceptionIfUnableToCleanUpReportDirectory()
        {
            this.mockDirectoryHelper.Setup(x => x.Exists(@"E:\temp")).Returns(true);
            this.mockDirectoryHelper.Setup(x => x.Delete(@"E:\temp", true)).Throws(new FileNotFoundException());

            this.mockDataCollectionSink.Raise(x => x.SendFileCompleted += null, new AsyncCompletedEventArgs(null, false, null));
            this.mockDataCollectionLogger.Verify(x => x.LogWarning(this.dataCollectionContext,
                It.Is<string>(y => y.Contains("CoverletDataCollectorException: CoverletCoverageDataCollector: Failed to cleanup report directory"))), Times.Once);
        }
    }
}
