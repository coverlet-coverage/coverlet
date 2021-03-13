namespace Coverlet.Core.Abstractions
{
    internal interface IReporter
    {
        ReporterOutputType OutputType { get; }
        string Format { get; }
        string Extension { get; }
        string Report(CoverageResult result, ISourceRootTranslator sourceRootTranslator);
    }

    internal enum ReporterOutputType
    {
        File,
        Console,
    }
}