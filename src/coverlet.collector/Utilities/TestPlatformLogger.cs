using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;

namespace Coverlet.Collector.Utilities
{
    /// <summary>
    /// Test platform logger
    /// </summary>
    internal class TestPlatformLogger
    {
        private readonly DataCollectionLogger logger;
        private readonly DataCollectionContext dataCollectionContext;

        public TestPlatformLogger(DataCollectionLogger logger, DataCollectionContext dataCollectionContext)
        {
            this.logger = logger;
            this.dataCollectionContext = dataCollectionContext;
        }

        /// <summary>
        /// Log warning
        /// </summary>
        /// <param name="warning">Warning message</param>
        public void LogWarning(string warning)
        {
            this.logger.LogWarning(this.dataCollectionContext, warning);
        }
    }
}
