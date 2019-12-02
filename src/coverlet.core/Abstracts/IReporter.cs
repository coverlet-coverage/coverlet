using Coverlet.Core.ObjectModel;

namespace Coverlet.Core.Abstracts
{
    public static class Reporters
    {
        public readonly static string Cobertura = "cobertura";
        public readonly static string Json = "json";
        public readonly static string Lcov = "lcov";
        public readonly static string OpenCover = "opencover";
        public readonly static string TeamCity = "teamcity";
    }

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