namespace Coverlet.Core.Reporters
{
    public interface IReporter
    {
        string Format { get; }
        string Extension { get; }
        string Report(CoverageResult result);
    }
}