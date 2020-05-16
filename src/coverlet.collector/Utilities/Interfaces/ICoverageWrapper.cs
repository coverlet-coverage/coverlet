using Coverlet.Collector.DataCollection;
using Coverlet.Core;
using Coverlet.Core.Abstractions;

namespace Coverlet.Collector.Utilities.Interfaces
{
    /// <summary>
    /// Wrapper interface for Coverage class in coverlet.core
    /// Since the class is not testable, this interface is used to abstract methods for mocking in unit tests.
    /// </summary>
    internal interface ICoverageWrapper
    {
        /// <summary>
        /// Creates a coverage object from given coverlet settings
        /// </summary>
        /// <param name="settings">Coverlet settings</param>
        /// <param name="logger">Coverlet logger</param>
        /// <param name="instrumentationHelper">Coverlet instrumentationHelper</param>
        /// <param name="fileSystem">Coverlet fileSystem</param>
        /// <param name="sourceRootTranslator">Coverlet sourceRootTranslator</param>
        /// <param name="cecilSymbolHelper"></param>
        /// <returns>Coverage object</returns>
        Coverage CreateCoverage(CoverletSettings settings, ILogger logger, IInstrumentationHelper instrumentationHelper, IFileSystem fileSystem, ISourceRootTranslator sourceRootTranslator, ICecilSymbolHelper cecilSymbolHelper);

        /// <summary>
        /// Gets the coverage result from provided coverage object
        /// </summary>
        /// <param name="coverage">Coverage</param>
        /// <returns>The coverage result</returns>
        CoverageResult GetCoverageResult(Coverage coverage);

        /// <summary>
        /// Prepares modules for getting coverage.
        /// Wrapper over coverage.PrepareModules
        /// </summary>
        /// <param name="coverage"></param>
        void PrepareModules(Coverage coverage);

    }
}
