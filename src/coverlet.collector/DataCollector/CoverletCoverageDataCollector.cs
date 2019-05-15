namespace Coverlet.Collector.DataCollector
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml;
    using coverlet.collector.DataCollector;
    using Coverlet.Collector.Utilities;
    using Coverlet.Collector.Utilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;

    /// <summary>
    /// Coverlet coverage out-proc data collector.
    /// </summary>
    [DataCollectorTypeUri(CoverletConstants.DefaultUri)]
    [DataCollectorFriendlyName(CoverletConstants.FriendlyName)]
    public class CoverletCoverageCollector : DataCollector
    {
        private readonly TestPlatformEqtTrace eqtTrace;
        private DataCollectionEvents events;
        private TestPlatformLogger logger;
        private XmlElement configurationElement;
        private DataCollectionSink dataSink;
        private DataCollectionContext dataCollectionContext;
        private CoverageManager coverageManager;
        private ICoverageWrapper coverageWrapper;

        public CoverletCoverageCollector() : this(new TestPlatformEqtTrace(), new CoverageWrapper())
        {
        }

        internal CoverletCoverageCollector(TestPlatformEqtTrace eqtTrace, ICoverageWrapper coverageWrapper) : base()
        {
            this.eqtTrace = eqtTrace;
            this.coverageWrapper = coverageWrapper;
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
            if (this.eqtTrace.IsInfoEnabled)
            {
                this.eqtTrace.Info("Initializing {0} with configuration: '{1}'", CoverletConstants.DataCollectorName, configurationElement?.OuterXml);
            }

            // Store input variables
            this.events = events;
            this.configurationElement = configurationElement;
            this.dataSink = dataSink;
            this.dataCollectionContext = environmentContext.SessionDataCollectionContext;
            this.logger = new TestPlatformLogger(logger, this.dataCollectionContext);

            // Register events
            this.events.SessionStart += this.OnSessionStart;
            this.events.SessionEnd += this.OnSessionEnd;
        }

        /// <summary>
        /// Disposes the data collector
        /// </summary>
        /// <param name="disposing">Disposing flag</param>
        protected override void Dispose(bool disposing)
        {
            this.eqtTrace.Verbose("{0}: Disposing", CoverletConstants.DataCollectorName);

            // Unregister events
            if (this.events != null)
            {
                this.events.SessionStart -= this.OnSessionStart;
                this.events.SessionEnd -= this.OnSessionEnd;
            }

            // Remove vars
            this.events = null;
            this.dataSink = null;
            this.coverageManager = null;

            base.Dispose(disposing);
        }

        /// <summary>
        /// SessionStart event handler
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="sessionStartEventArgs">Event args</param>
        private void OnSessionStart(object sender, SessionStartEventArgs sessionStartEventArgs)
        {
            this.eqtTrace.Verbose("{0}: SessionStart received", CoverletConstants.DataCollectorName);

            try
            {
                // Get coverlet settings
                IEnumerable<string> testModules = this.GetTestModules(sessionStartEventArgs);
                var coverletSettingsParser = new CoverletSettingsParser(this.eqtTrace);
                var coverletSettings = coverletSettingsParser.Parse(this.configurationElement, testModules);

                // Get coverage and attachment managers
                this.coverageManager = new CoverageManager(coverletSettings, this.eqtTrace, this.logger, this.coverageWrapper);

                // Instrument modules
                this.coverageManager.InstrumentModules();
            }
            catch (Exception ex)
            {
                this.logger.LogWarning(ex.ToString());
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
                this.eqtTrace.Verbose("{0}: SessionEnd received", CoverletConstants.DataCollectorName);

                // Get coverage reports
                var coverageReport = this.coverageManager?.GetCoverageReport();

                // Send result attachments to test platform.
                using (var attachmentManager = new AttachmentManager(dataSink, this.dataCollectionContext, this.logger, this.eqtTrace, this.GetReportFileName()))
                {
                    attachmentManager?.SendCoverageReport(coverageReport);
                }
            }
            catch (Exception ex)
            {
                this.logger.LogWarning(ex.ToString());
                this.Dispose(true);
            }
        }

        /// <summary>
        /// Gets coverage report file name
        /// </summary>
        /// <returns>Coverage report file name</returns>
        private string GetReportFileName()
        {
            var fileName = CoverletConstants.DefaultFileName;
            var extension = this.coverageManager?.Reporter.Extension;

            return extension == null ? fileName : $"{fileName}.{extension}";
        }

        /// <summary>
        /// Gets test modules
        /// </summary>
        /// <param name="sessionStartEventArgs">Event args</param>
        /// <returns>Test modules list</returns>
        private IEnumerable<string> GetTestModules(SessionStartEventArgs sessionStartEventArgs)
        {
            var testModules = sessionStartEventArgs.GetPropertyValue<IEnumerable<string>>(CoverletConstants.TestSourcesPropertyName);
            if (this.eqtTrace.IsInfoEnabled)
            {
                this.eqtTrace.Info("{0}: TestModules: '{1}'",
                    CoverletConstants.DataCollectorName,
                    string.Join(",", testModules ?? Enumerable.Empty<string>()));
            }

            return testModules;
        }
    }
}
