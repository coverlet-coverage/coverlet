using Newtonsoft.Json;

using Coverlet.Core.Abstractions;
using System;

namespace Coverlet.Core.Reporters
{
    internal class JsonReporter : IReporter
    {
        public ReporterOutputType OutputType => ReporterOutputType.File;

        public string Format => "json";

        public string Extension => "json";

        public string Report(CoverageResult result, ISourceRootTranslator _)
        {
            return JsonConvert.SerializeObject(result.Modules, Formatting.Indented);
        }
    }
}