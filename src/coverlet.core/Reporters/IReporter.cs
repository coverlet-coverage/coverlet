namespace Coverlet.Core.Reporters
{
    internal interface IReporter
    {
        ReporterOutputType OutputType { get; }
        string Format { get; }
        string Extension { get; }
        string Report(CoverageResult result);
    }

    internal enum ReporterOutputType
    {
        File,
        Console,
    }
}