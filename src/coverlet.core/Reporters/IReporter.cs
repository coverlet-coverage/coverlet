using System.IO;

namespace Coverlet.Core.Reporters
{
    public interface IReporter
    {
        string Format { get; }
        string Extension { get; }
        void Report(CoverageResult result, StreamWriter stream);
    }
}