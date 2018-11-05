namespace Coverlet.Core.Reporters
{
    public interface IReporter
    {
        bool UseConsoleOutput { get; }
        string Format { get; }
        string Extension { get; }
        string Report(CoverageResult result);
    }
}