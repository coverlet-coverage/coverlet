using Coverlet.Core.ObjectModel;

namespace Coverlet.Core.Abstracts
{
    internal interface IReporterFactory
    {
        IReporter Create(string format);
    }

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
        Console
    }
}