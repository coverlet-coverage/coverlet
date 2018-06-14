using Newtonsoft.Json;

namespace Coverlet.Core.Reporters
{
    public class JsonReporter : IReporter
    {
        public string Format => "json";

        public string Extension => "json";

        public string Report(CoverageResult result)
        {
            return JsonConvert.SerializeObject(result.Modules, Formatting.Indented);
        }
    }
}