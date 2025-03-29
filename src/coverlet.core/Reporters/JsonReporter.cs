// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Encodings.Web;
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
      var options = new JsonSerializerOptions
      {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        IncludeFields = true,
        WriteIndented = true,
      };
      return JsonSerializer.Serialize(result.Modules, options);
    }
  }
}
