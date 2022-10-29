// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Coverlet.Core.Abstractions;
using Newtonsoft.Json;

#nullable disable

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
