// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Coverlet.Core.Abstractions;


namespace Coverlet.Core.Reporters
{
    internal class JsonReporter : IReporter
    {
        public ReporterOutputType OutputType => ReporterOutputType.File;

        public string Format => "json";

        public string Extension => "json";

        public string Report(CoverageResult result, ISourceRootTranslator _)
        {
            JsonSerializerOptions options = new JsonSerializerOptions();
            options.WriteIndented = true;
            return JsonSerializer.Serialize(result.Modules, options);
        }
    }
}
