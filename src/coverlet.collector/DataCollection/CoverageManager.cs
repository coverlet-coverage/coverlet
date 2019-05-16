using System;
using coverlet.collector.Resources;
using Coverlet.Collector.Utilities;
using Coverlet.Collector.Utilities.Interfaces;
using Coverlet.Core;
using Coverlet.Core.Logging;
using Coverlet.Core.Reporters;

namespace Coverlet.Collector.DataCollection
{
    /// <summary>
    /// Manages coverlet coverage
    /// </summary>
    internal class CoverageManager
    {
        private readonly Coverage _coverage;

        private ICoverageWrapper _coverageWrapper;

        public IReporter Reporter { get; }

        public CoverageManager(CoverletSettings settings, TestPlatformEqtTrace eqtTrace, TestPlatformLogger logger, ICoverageWrapper coverageWrapper)
            : this(settings,
                  new ReporterFactory(settings.ReportFormat).CreateReporter(),
                  new CoverletLogger(eqtTrace, logger),
                  coverageWrapper)
        {
        }

        public CoverageManager(CoverletSettings settings, IReporter reporter, ILogger logger, ICoverageWrapper coverageWrapper)
        {
            // Store input vars
            Reporter = reporter;
            _coverageWrapper = coverageWrapper;

            // Coverage object
            _coverage = _coverageWrapper.CreateCoverage(settings, logger);
        }

        /// <summary>
        /// Instrument modules
        /// </summary>
        public void InstrumentModules()
        {
            try
            {
                // Instrument modules
                _coverageWrapper.PrepareModules(_coverage);
            }
            catch (Exception ex)
            {
                string errorMessage = string.Format(Resources.InstrumentationException, CoverletConstants.DataCollectorName);
                throw new CoverletDataCollectorException(errorMessage, ex);
            }
        }

        /// <summary>
        /// Gets coverlet coverage report
        /// </summary>
        /// <returns>Coverage report</returns>
        public string GetCoverageReport()
        {
            // Get coverage result
            CoverageResult coverageResult = this.GetCoverageResult();

            // Get coverage report in default format
            string coverageReport = this.GetCoverageReport(coverageResult);
            return coverageReport;
        }

        /// <summary>
        /// Gets coverlet coverage result
        /// </summary>
        /// <returns>Coverage result</returns>
        private CoverageResult GetCoverageResult()
        {
            try
            {
                return _coverageWrapper.GetCoverageResult(_coverage);
            }
            catch (Exception ex)
            {
                string errorMessage = string.Format(Resources.CoverageResultException, CoverletConstants.DataCollectorName);
                throw new CoverletDataCollectorException(errorMessage, ex);
            }
        }

        /// <summary>
        /// Gets coverage report from coverage result
        /// </summary>
        /// <param name="coverageResult">Coverage result</param>
        /// <returns>Coverage report</returns>
        private string GetCoverageReport(CoverageResult coverageResult)
        {
            try
            {
                return Reporter.Report(coverageResult);
            }
            catch (Exception ex)
            {
                string errorMessage = string.Format(Resources.CoverageReportException, CoverletConstants.DataCollectorName);
                throw new CoverletDataCollectorException(errorMessage, ex);
            }
        }
    }
}
