namespace Coverlet.Core.Reporters
{
    public interface IReporter
    {
        ReporterOutputType OutputType { get; }
        string Format { get; }
        string Extension { get; }
        string Report(CoverageResult result);
    }

    public enum ReporterOutputType
    {
        File,
        Console,
    }
}