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
using Coverlet.Core.Abstractions;
using Coverlet.Core.Helpers;
using Coverlet.Core.Symbols;
using Microsoft.Extensions.DependencyInjection;

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
            _mockCountDownEventFactory.Setup(def => def.Create(It.IsAny<int>(), It.IsAny<TimeSpan>())).Returns(new Mock<ICountDownEvent>().Object);
        }

        [Fact]
        public void OnSessionStartShouldInitializeCoverageWithCorrectCoverletSettings()
        {
            Func<TestPlatformEqtTrace, TestPlatformLogger, string, IServiceCollection> serviceCollectionFactory = (TestPlatformEqtTrace eqtTrace, TestPlatformLogger logger, string testModule) =>
            {
                IServiceCollection serviceCollection = new ServiceCollection();
                Mock<IFileSystem> fileSystem = new Mock<IFileSystem>();
                fileSystem.Setup(f => f.Exists(It.IsAny<string>())).Returns((string testLib) => testLib == "abc.dll");
                serviceCollection.AddTransient(_ => fileSystem.Object);

                serviceCollection.AddTransient<IRetryHelper, RetryHelper>();
                serviceCollection.AddTransient<IProcessExitHandler, ProcessExitHandler>();
                serviceCollection.AddTransient<ILogger, CoverletLogger>(_ => new CoverletLogger(eqtTrace, logger));
                serviceCollection.AddSingleton<IInstrumentationHelper, InstrumentationHelper>();
                serviceCollection.AddSingleton<ISourceRootTranslator, SourceRootTranslator>(serviceProvider => new SourceRootTranslator(testModule, serviceProvider.GetRequiredService<ILogger>(), serviceProvider.GetRequiredService<IFileSystem>()));
                serviceCollection.AddSingleton<ICecilSymbolHelper, CecilSymbolHelper>();
                return serviceCollection;
            };
            _coverletCoverageDataCollector = new CoverletCoverageCollector(new TestPlatformEqtTrace(), _mockCoverageWrapper.Object, _mockCountDownEventFactory.Object, serviceCollectionFactory);
            _coverletCoverageDataCollector.Initialize(
                    _configurationElement,
                    _mockDataColectionEvents.Object,
                    _mockDataCollectionSink.Object,
                    _mockLogger.Object,
                    _context);
            IDictionary<string, object> sessionStartProperties = new Dictionary<string, object>();

            sessionStartProperties.Add("TestSources", new List<string> { "abc.dll" });

            _mockDataColectionEvents.Raise(x => x.SessionStart += null, new SessionStartEventArgs(sessionStartProperties));

            _mockCoverageWrapper.Verify(x => x.CreateCoverage(It.Is<CoverletSettings>(y => string.Equals(y.TestModule, "abc.dll")), It.IsAny<ILogger>(), It.IsAny<IInstrumentationHelper>(), It.IsAny<IFileSystem>(), It.IsAny<ISourceRootTranslator>(), It.IsAny<ICecilSymbolHelper>()), Times.Once);
        }

        [Fact]
        public void OnSessionStartShouldPrepareModulesForCoverage()
        {
            Func<TestPlatformEqtTrace, TestPlatformLogger, string, IServiceCollection> serviceCollectionFactory = (TestPlatformEqtTrace eqtTrace, TestPlatformLogger logger, string testModule) =>
            {
                IServiceCollection serviceCollection = new ServiceCollection();
                Mock<IFileSystem> fileSystem = new Mock<IFileSystem>();
                fileSystem.Setup(f => f.Exists(It.IsAny<string>())).Returns((string testLib) => testLib == "abc.dll");
                serviceCollection.AddTransient(_ => fileSystem.Object);

                serviceCollection.AddTransient<IRetryHelper, RetryHelper>();
                serviceCollection.AddTransient<IProcessExitHandler, ProcessExitHandler>();
                serviceCollection.AddTransient<ILogger, CoverletLogger>(_ => new CoverletLogger(eqtTrace, logger));
                serviceCollection.AddSingleton<IInstrumentationHelper, InstrumentationHelper>();
                serviceCollection.AddSingleton<ISourceRootTranslator, SourceRootTranslator>(serviceProvider => new SourceRootTranslator(testModule, serviceProvider.GetRequiredService<ILogger>(), serviceProvider.GetRequiredService<IFileSystem>()));
                serviceCollection.AddSingleton<ICecilSymbolHelper, CecilSymbolHelper>();
                return serviceCollection;
            };
            _coverletCoverageDataCollector = new CoverletCoverageCollector(new TestPlatformEqtTrace(), _mockCoverageWrapper.Object, _mockCountDownEventFactory.Object, serviceCollectionFactory);
            _coverletCoverageDataCollector.Initialize(
                    _configurationElement,
                    _mockDataColectionEvents.Object,
                    _mockDataCollectionSink.Object,
                    null,
                    _context);
            IDictionary<string, object> sessionStartProperties = new Dictionary<string, object>();
            IInstrumentationHelper instrumentationHelper =
                new InstrumentationHelper(new Mock<IProcessExitHandler>().Object,
                                          new Mock<IRetryHelper>().Object,
                                          new Mock<IFileSystem>().Object,
                                          new Mock<ILogger>().Object,
                                          new Mock<ISourceRootTranslator>().Object);

            CoverageParameters parameters = new CoverageParameters
            {
                IncludeFilters = null,
                IncludeDirectories = null,
                ExcludedSourceFiles = null,
                ExcludeAttributes = null,
                IncludeTestAssembly = true,
                SingleHit = true,
                MergeWith = "abc.json",
                UseSourceLink = true
            };

            Coverage coverage = new Coverage("abc.dll", parameters, It.IsAny<ILogger>(), instrumentationHelper, new Mock<IFileSystem>().Object, new Mock<ISourceRootTranslator>().Object, new Mock<ICecilSymbolHelper>().Object);

            sessionStartProperties.Add("TestSources", new List<string> { "abc.dll" });
            _mockCoverageWrapper.Setup(x => x.CreateCoverage(It.IsAny<CoverletSettings>(), It.IsAny<ILogger>(), It.IsAny<IInstrumentationHelper>(), It.IsAny<IFileSystem>(), It.IsAny<ISourceRootTranslator>(), It.IsAny<ICecilSymbolHelper>())).Returns(coverage);

            _mockDataColectionEvents.Raise(x => x.SessionStart += null, new SessionStartEventArgs(sessionStartProperties));

            _mockCoverageWrapper.Verify(x => x.CreateCoverage(It.Is<CoverletSettings>(y => y.TestModule.Contains("abc.dll")), It.IsAny<ILogger>(), It.IsAny<IInstrumentationHelper>(), It.IsAny<IFileSystem>(), It.IsAny<ISourceRootTranslator>(), It.IsAny<ICecilSymbolHelper>()), Times.Once);
            _mockCoverageWrapper.Verify(x => x.PrepareModules(It.IsAny<Coverage>()), Times.Once);
        }

        [Fact]
        public void OnSessionEndShouldSendGetCoverageReportToTestPlatform()
        {
            Func<TestPlatformEqtTrace, TestPlatformLogger, string, IServiceCollection> serviceCollectionFactory = (TestPlatformEqtTrace eqtTrace, TestPlatformLogger logger, string testModule) =>
            {
                IServiceCollection serviceCollection = new ServiceCollection();
                serviceCollection.AddTransient<IFileSystem, FileSystem>();
                serviceCollection.AddTransient<IRetryHelper, RetryHelper>();
                serviceCollection.AddTransient<IProcessExitHandler, ProcessExitHandler>();
                serviceCollection.AddTransient<ILogger, CoverletLogger>(_ => new CoverletLogger(eqtTrace, logger));
                serviceCollection.AddSingleton<IInstrumentationHelper, InstrumentationHelper>();
                serviceCollection.AddSingleton<ISourceRootTranslator, SourceRootTranslator>(serviceProvider => new SourceRootTranslator(testModule, serviceProvider.GetRequiredService<ILogger>(), serviceProvider.GetRequiredService<IFileSystem>()));
                serviceCollection.AddSingleton<ICecilSymbolHelper, CecilSymbolHelper>();
                return serviceCollection;
            };
            _coverletCoverageDataCollector = new CoverletCoverageCollector(new TestPlatformEqtTrace(), new CoverageWrapper(), _mockCountDownEventFactory.Object, serviceCollectionFactory);
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
            Func<TestPlatformEqtTrace, TestPlatformLogger, string, IServiceCollection> serviceCollectionFactory = (TestPlatformEqtTrace eqtTrace, TestPlatformLogger logger, string testModule) =>
            {
                IServiceCollection serviceCollection = new ServiceCollection();
                Mock<IFileSystem> fileSystem = new Mock<IFileSystem>();
                fileSystem.Setup(f => f.Exists(It.IsAny<string>())).Returns((string testLib) => testLib == "Test");
                serviceCollection.AddTransient(_ => fileSystem.Object);

                serviceCollection.AddTransient<IRetryHelper, RetryHelper>();
                serviceCollection.AddTransient<IProcessExitHandler, ProcessExitHandler>();
                serviceCollection.AddTransient<ILogger, CoverletLogger>(_ => new CoverletLogger(eqtTrace, logger));
                serviceCollection.AddSingleton<IInstrumentationHelper, InstrumentationHelper>();
                serviceCollection.AddSingleton<ISourceRootTranslator, SourceRootTranslator>(serviceProvider => new SourceRootTranslator(testModule, serviceProvider.GetRequiredService<ILogger>(), serviceProvider.GetRequiredService<IFileSystem>()));
                serviceCollection.AddSingleton<ICecilSymbolHelper, CecilSymbolHelper>();
                return serviceCollection;
            };
            _coverletCoverageDataCollector = new CoverletCoverageCollector(new TestPlatformEqtTrace(), new CoverageWrapper(), _mockCountDownEventFactory.Object, serviceCollectionFactory);

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
            Func<TestPlatformEqtTrace, TestPlatformLogger, string, IServiceCollection> serviceCollectionFactory = (TestPlatformEqtTrace eqtTrace, TestPlatformLogger logger, string testModule) =>
            {
                IServiceCollection serviceCollection = new ServiceCollection();
                Mock<IFileSystem> fileSystem = new Mock<IFileSystem>();
                fileSystem.Setup(f => f.Exists(It.IsAny<string>())).Returns((string testLib) => testLib == "abc.dll");
                serviceCollection.AddTransient(_ => fileSystem.Object);

                serviceCollection.AddTransient<IRetryHelper, RetryHelper>();
                serviceCollection.AddTransient<IProcessExitHandler, ProcessExitHandler>();
                serviceCollection.AddTransient<ILogger, CoverletLogger>(_ => new CoverletLogger(eqtTrace, logger));
                serviceCollection.AddSingleton<IInstrumentationHelper, InstrumentationHelper>();
                serviceCollection.AddSingleton<ISourceRootTranslator, SourceRootTranslator>(serviceProvider => new SourceRootTranslator(testModule, serviceProvider.GetRequiredService<ILogger>(), serviceProvider.GetRequiredService<IFileSystem>()));
                serviceCollection.AddSingleton<ICecilSymbolHelper, CecilSymbolHelper>();
                return serviceCollection;
            };
            _coverletCoverageDataCollector = new CoverletCoverageCollector(new TestPlatformEqtTrace(), _mockCoverageWrapper.Object, _mockCountDownEventFactory.Object, serviceCollectionFactory);
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
