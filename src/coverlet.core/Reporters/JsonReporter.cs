using Newtonsoft.Json;

namespace Coverlet.Core.Reporters
{
    internal class JsonReporter : ReporterBase
    {
        public override ReporterOutputType OutputType => ReporterOutputType.File;

        public override string Format => "json";

        public override string Extension => "json";

        public override string Report(CoverageResult result)
        {
            return JsonConvert.SerializeObject(result.Modules, Formatting.Indented);
        }
    }
}