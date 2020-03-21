namespace Coverlet.Tests.Xunit.Extensions
{
    public interface ITestCondition
    {
        bool IsMet { get; }
        string SkipReason { get; }
    }
}
