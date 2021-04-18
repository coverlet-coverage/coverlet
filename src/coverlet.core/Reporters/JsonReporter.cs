using Newtonsoft.Json;

using Coverlet.Core.Abstractions;
using System;

namespace Coverlet.Core.Reporters
{
    internal class JsonReporter : ReporterBase
    {
        public override ReporterOutputType OutputType => ReporterOutputType.File;

        public override string Format => "json";

        public override string Extension => "json";

        public override string Report(CoverageResult result, ISourceRootTranslator sourceRootTranslator)
        {
            return JsonConvert.SerializeObject(result.Modules, Formatting.Indented);
        }
    }
}