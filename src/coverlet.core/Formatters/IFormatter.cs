namespace Coverlet.Core.Formatters
{
    public interface IFormatter
    {
        string Format(CoverageResult result);
    }
}