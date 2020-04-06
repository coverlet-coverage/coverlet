using Coverlet.Collector.Utilities.Interfaces;
using Coverlet.Core;
using Coverlet.Core.Abstracts;
using Coverlet.Core.Extensions;

namespace Coverlet.Collector.DataCollection
{
    /// <summary>
    /// Implementation for wrapping over Coverage class in coverlet.core
    /// </summary>
    internal class CoverageWrapper : ICoverageWrapper
    {
        /// <summary>
        /// Creates a coverage object from given coverlet settings
        /// </summary>
        /// <param name="settings">Coverlet settings</param>
        /// <param name="coverletLogger">Coverlet logger</param>
        /// <returns>Coverage object</returns>
        public Coverage CreateCoverage(CoverletSettings settings, ILogger coverletLogger, IInstrumentationHelper instrumentationHelper, IFileSystem fileSystem, ISourceRootTranslator sourceRootTranslator)
        {
            return new Coverage(
                settings.TestModule,
                settings.IncludeFilters,
                settings.IncludeDirectories,
                settings.ExcludeFilters,
                settings.ExcludeSourceFiles,
                settings.ExcludeAttributes,
                settings.IncludeTestAssembly,
                settings.SingleHit,
                settings.MergeWith,
                settings.UseSourceLink,
                coverletLogger,
                instrumentationHelper,
                fileSystem,
                sourceRootTranslator);
        }

        /// <summary>
        /// Gets the coverage result from provided coverage object
        /// </summary>
        /// <param name="coverage">Coverage</param>
        /// <returns>The coverage result</returns>
        public CoverageResult GetCoverageResult(Coverage coverage)
        {
            return coverage.GetCoverageResult();
        }

        /// <summary>
        /// Prepares modules for getting coverage.
        /// Wrapper over coverage.PrepareModules
        /// </summary>
        /// <param name="coverage"></param>
        public void PrepareModules(Coverage coverage)
        {
            coverage.PrepareModules();
        }
    }
}
