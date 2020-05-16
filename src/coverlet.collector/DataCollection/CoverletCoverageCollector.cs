using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml;
using Coverlet.Collector.Utilities;
using Coverlet.Collector.Utilities.Interfaces;
using Coverlet.Core.Abstractions;
using Coverlet.Core.Helpers;
using Coverlet.Core.Symbols;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;

namespace Coverlet.Collector.DataCollection
{
    /// <summary>
    /// Coverlet coverage out-proc data collector.
    /// </summary>
    [DataCollectorTypeUri(CoverletConstants.DefaultUri)]
    [DataCollectorFriendlyName(CoverletConstants.FriendlyName)]
    public class CoverletCoverageCollector : DataCollector
    {
        private readonly TestPlatformEqtTrace _eqtTrace;
        private readonly ICoverageWrapper _coverageWrapper;
        private readonly ICountDownEventFactory _countDownEventFactory;
        private readonly Func<TestPlatformEqtTrace, TestPlatformLogger, string, IServiceCollection> _serviceCollectionFactory;

        private DataCollectionEvents _events;
        private TestPlatformLogger _logger;
        private XmlElement _configurationElement;
        private DataCollectionSink _dataSink;
        private DataCollectionContext _dataCollectionContext;
        private CoverageManager _coverageManager;
        private IServiceProvider _serviceProvider;

        public CoverletCoverageCollector() : this(new TestPlatformEqtTrace(), new CoverageWrapper(), new CollectorCountdownEventFactory(), GetDefaultServiceCollection)
        {
        }

        internal CoverletCoverageCollector(TestPlatformEqtTrace eqtTrace, ICoverageWrapper coverageWrapper, ICountDownEventFactory countDownEventFactory, Func<TestPlatformEqtTrace, TestPlatformLogger, string, IServiceCollection> serviceCollectionFactory) : base()
        {
            _eqtTrace = eqtTrace;
            _coverageWrapper = coverageWrapper;
            _countDownEventFactory = countDownEventFactory;
            _serviceCollectionFactory = serviceCollectionFactory;
        }

        private void AttachDebugger()
        {
            if (int.TryParse(Environment.GetEnvironmentVariable("COVERLET_DATACOLLECTOR_OUTOFPROC_DEBUG"), out int result) && result == 1)
            {
                Debugger.Launch();
                Debugger.Break();
            }
        }

        /// <summary>
        /// Initializes data collector
        /// </summary>
        /// <param name="configurationElement">Configuration element</param>
        /// <param name="events">Events to register on</param>
        /// <param name="dataSink">Data sink to send attachments to test platform</param>
        /// <param name="logger">Test platform logger</param>
        /// <param name="environmentContext">Environment context</param>
        public override void Initialize(
            XmlElement configurationElement,
            DataCollectionEvents events,
            DataCollectionSink dataSink,
            DataCollectionLogger logger,
            DataCollectionEnvironmentContext environmentContext)
        {

            AttachDebugger();

            if (_eqtTrace.IsInfoEnabled)
            {
                _eqtTrace.Info("Initializing {0} with configuration: '{1}'", CoverletConstants.DataCollectorName, configurationElement?.OuterXml);
            }

            // Store input variables
            _events = events;
            _configurationElement = configurationElement;
            _dataSink = dataSink;
            _dataCollectionContext = environmentContext.SessionDataCollectionContext;
            _logger = new TestPlatformLogger(logger, _dataCollectionContext);

            // Register events
            _events.SessionStart += OnSessionStart;
            _events.SessionEnd += OnSessionEnd;
        }

        /// <summary>
        /// Disposes the data collector
        /// </summary>
        /// <param name="disposing">Disposing flag</param>
        protected override void Dispose(bool disposing)
        {
            _eqtTrace.Verbose("{0}: Disposing", CoverletConstants.DataCollectorName);

            // Unregister events
            if (_events != null)
            {
                _events.SessionStart -= OnSessionStart;
                _events.SessionEnd -= OnSessionEnd;
            }

            // Remove vars
            _events = null;
            _dataSink = null;
            _coverageManager = null;

            base.Dispose(disposing);
        }

        /// <summary>
        /// SessionStart event handler
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="sessionStartEventArgs">Event args</param>
        private void OnSessionStart(object sender, SessionStartEventArgs sessionStartEventArgs)
        {
            _eqtTrace.Verbose("{0}: SessionStart received", CoverletConstants.DataCollectorName);

            try
            {
                // Get coverlet settings
                IEnumerable<string> testModules = this.GetTestModules(sessionStartEventArgs);
                var coverletSettingsParser = new CoverletSettingsParser(_eqtTrace);
                CoverletSettings coverletSettings = coverletSettingsParser.Parse(_configurationElement, testModules);

                // Build services container
                _serviceProvider = _serviceCollectionFactory(_eqtTrace, _logger, coverletSettings.TestModule).BuildServiceProvider();

                // Get coverage and attachment managers
                _coverageManager = new CoverageManager(coverletSettings, _eqtTrace, _logger, _coverageWrapper,
                                                        _serviceProvider.GetRequiredService<IInstrumentationHelper>(), _serviceProvider.GetRequiredService<IFileSystem>(),
                                                        _serviceProvider.GetRequiredService<ISourceRootTranslator>(), _serviceProvider.GetRequiredService<ICecilSymbolHelper>());

                // Instrument modules
                _coverageManager.InstrumentModules();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex.ToString());
                this.Dispose(true);
            }
        }

        /// <summary>
        /// SessionEnd event handler
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event args</param>
        private void OnSessionEnd(object sender, SessionEndEventArgs e)
        {
            try
            {
                _eqtTrace.Verbose("{0}: SessionEnd received", CoverletConstants.DataCollectorName);

                // Get coverage reports
                IEnumerable<(string report, string fileName)> coverageReports = _coverageManager?.GetCoverageReports();

                if (coverageReports != null && coverageReports.Count() > 0)
                {
                    // Send result attachments to test platform.
                    using (var attachmentManager = new AttachmentManager(_dataSink, _dataCollectionContext, _logger, _eqtTrace, _countDownEventFactory.Create(coverageReports.Count(), TimeSpan.FromSeconds(30))))
                    {
                        foreach ((string report, string fileName) in coverageReports)
                        {
                            attachmentManager.SendCoverageReport(report, fileName);
                        }
                    }
                }
                else
                {
                    _eqtTrace.Verbose("{0}: No coverage reports specified", CoverletConstants.DataCollectorName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex.ToString());
                this.Dispose(true);
            }
        }

        /// <summary>
        /// Gets test modules
        /// </summary>
        /// <param name="sessionStartEventArgs">Event args</param>
        /// <returns>Test modules list</returns>
        private IEnumerable<string> GetTestModules(SessionStartEventArgs sessionStartEventArgs)
        {
            try
            {
                IEnumerable<string> testModules = GetPropertyValueWrapper(sessionStartEventArgs);
                if (_eqtTrace.IsInfoEnabled)
                {
                    _eqtTrace.Info("{0}: TestModules: '{1}'",
                        CoverletConstants.DataCollectorName,
                        string.Join(",", testModules ?? Enumerable.Empty<string>()));
                }
                return testModules;
            }
            catch (MissingMethodException ex)
            {
                throw new MissingMethodException("Make sure to use .NET core SDK Version >= 2.2.300", ex);
            }
        }

        /// <summary>
        /// Wraps GetPropertyValue to catch possible MissingMethodException on unsupported runtime
        /// </summary>
        /// <param name="sessionStartEventArgs"></param>
        /// <returns></returns>
        private static IEnumerable<string> GetPropertyValueWrapper(SessionStartEventArgs sessionStartEventArgs)
        {
            return sessionStartEventArgs.GetPropertyValue<IEnumerable<string>>(CoverletConstants.TestSourcesPropertyName);
        }

        private static IServiceCollection GetDefaultServiceCollection(TestPlatformEqtTrace eqtTrace, TestPlatformLogger logger, string testModule)
        {
            IServiceCollection serviceCollection = new ServiceCollection();
            serviceCollection.AddTransient<IRetryHelper, RetryHelper>();
            serviceCollection.AddTransient<IProcessExitHandler, ProcessExitHandler>();
            serviceCollection.AddTransient<IFileSystem, FileSystem>();
            serviceCollection.AddTransient<ILogger, CoverletLogger>(_ => new CoverletLogger(eqtTrace, logger));
            // We need to keep singleton/static semantics
            serviceCollection.AddSingleton<IInstrumentationHelper, InstrumentationHelper>();
            // We cache resolutions
            serviceCollection.AddSingleton<ISourceRootTranslator, SourceRootTranslator>(serviceProvider => new SourceRootTranslator(testModule, serviceProvider.GetRequiredService<ILogger>(), serviceProvider.GetRequiredService<IFileSystem>()));
            serviceCollection.AddSingleton<ICecilSymbolHelper, CecilSymbolHelper>();
            return serviceCollection;
        }
    }
}
