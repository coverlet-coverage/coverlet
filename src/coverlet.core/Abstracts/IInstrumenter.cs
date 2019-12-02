#nullable enable

using System.IO;
using System.Reflection;

using Coverlet.Core.ObjectModel;

namespace Coverlet.Core.Abstracts
{
    public class InstrumentationOptions
    {
        public string? Module { get; set; }
        public string[]? IncludeFilters { get; set; }
        public string[]? ExcludeFilters { get; set; }
        public string[]? ExcludeSourceFiles { get; set; }
        public string[]? ExcludeAttributes { get; set; }
        public string[]? IncludeDirectories { get; set; }
        public bool IncludeTestAssembly { get; set; }
        public bool SingleHit { get; set; }
        public bool UseSourceLink { get; set; }
        public string? MergeWith { get; set; }
    }

    public interface ICoverageEngineFactory
    {
        ICoverageEngine CreateEngine(InstrumentationOptions options);
        IReporter CreateReporter(string format);
        IInProcessCoverageEngine CreateInProcessEngine(Stream instrumentationResultStream);
    }

    public interface ICoverageEngine
    {
        Stream PrepareModules();
        CoverageResult GetCoverageResult(Stream instrumentationResultStream);
    }

    public interface IInProcessCoverageEngine
    {
        Assembly[] GetInstrumentedAssemblies();
        CoverageResult? ReadCurrentCoverage();
    }
}
