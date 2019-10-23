using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;

namespace Coverlet.Collector.Utilities
{
    /// <summary>
    /// Test platform logger
    /// </summary>
    internal class TestPlatformLogger
    {
        private readonly DataCollectionLogger _logger;
        private readonly DataCollectionContext _dataCollectionContext;

        public TestPlatformLogger(DataCollectionLogger logger, DataCollectionContext dataCollectionContext)
        {
            _logger = logger;
            _dataCollectionContext = dataCollectionContext;
        }

        /// <summary>
        /// Log warning
        /// </summary>
        /// <param name="warning">Warning message</param>
        public void LogWarning(string warning)
        {
            _logger.LogWarning(_dataCollectionContext, $"[coverlet]{warning}");
        }
    }
}
