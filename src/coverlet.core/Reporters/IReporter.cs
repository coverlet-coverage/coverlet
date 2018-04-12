namespace Coverlet.Core.Reporters
{
    public interface IReporter
    {
        string Report(CoverageResult result);
    }
}