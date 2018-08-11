using System.IO;
using Newtonsoft.Json;

namespace Coverlet.Core.Reporters
{
    public class JsonReporter : IReporter
    {
        private static readonly JsonSerializer _jsonSerializer = JsonSerializer.CreateDefault(new JsonSerializerSettings { Formatting = Formatting.Indented });

        public string Format => "json";

        public string Extension => "json";

        public void Report(CoverageResult result, StreamWriter streamWriter)
        {
            _jsonSerializer.Serialize(streamWriter, result.Modules);
        }
    }
}