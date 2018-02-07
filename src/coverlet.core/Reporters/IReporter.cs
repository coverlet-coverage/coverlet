namespace Coverlet.Core.Reporters
{
    public interface IReporter
    {
        string Format(CoverageResult result);
    }
}