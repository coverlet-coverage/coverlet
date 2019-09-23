using Newtonsoft.Json;

namespace Coverlet.Core.Reporters
{
    internal class JsonReporter : IReporter
    {
        public ReporterOutputType OutputType => ReporterOutputType.File;

        public string Format => "json";

        public string Extension => "json";

        public string Report(CoverageResult result)
        {
            return JsonConvert.SerializeObject(result.Modules, Formatting.Indented);
        }
    }
}