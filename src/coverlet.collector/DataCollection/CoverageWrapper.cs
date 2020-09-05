using Coverlet.Collector.Utilities.Interfaces;
using Coverlet.Core;
using Coverlet.Core.Abstractions;

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
        public Coverage CreateCoverage(CoverletSettings settings, ILogger coverletLogger, IInstrumentationHelper instrumentationHelper, IFileSystem fileSystem, ISourceRootTranslator sourceRootTranslator, ICecilSymbolHelper cecilSymbolHelper)
        {
            CoverageParameters parameters = new CoverageParameters
            {
                IncludeFilters = settings.IncludeFilters,
                IncludeDirectories = settings.IncludeDirectories,
                ExcludeFilters = settings.ExcludeFilters,
                ExcludedSourceFiles = settings.ExcludeSourceFiles,
                ExcludeAttributes = settings.ExcludeAttributes,
                IncludeTestAssembly = settings.IncludeTestAssembly,
                SingleHit = settings.SingleHit,
                MergeWith = settings.MergeWith,
                UseSourceLink = settings.UseSourceLink,
                SkipAutoProps = settings.SkipAutoProps,
                DoesNotReturnAttributes = settings.DoesNotReturnAttributes
            };

            return new Coverage(
                settings.TestModule,
                parameters,
                coverletLogger,
                instrumentationHelper,
                fileSystem,
                sourceRootTranslator,
                cecilSymbolHelper);
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
