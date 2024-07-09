// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Coverlet.Collector.Utilities;
using Coverlet.Core;
using Coverlet.Core.Abstractions;
using coverlet.core.Helpers;
using Coverlet.Core.Reporters;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Newtonsoft.Json;
using System.Linq;

namespace coverlet.collector.ArtifactPostProcessor
{
  public class CoverletCoveragePostProcessor : IDataCollectorAttachmentProcessor
  {
    private readonly CoverageResult _coverageResult = new();

    public CoverletCoveragePostProcessor()
    {
      _coverageResult.Modules = new Modules();
    }

#pragma warning disable CS8632
    public IEnumerable<Uri>? GetExtensionUris()
#pragma warning restore CS8632
    {
      var uri = new Uri(CoverletConstants.DefaultUri);
      return new[] { uri };
    }

    public Task<ICollection<AttachmentSet>> ProcessAttachmentSetsAsync(XmlElement configurationElement, ICollection<AttachmentSet> attachments, IProgress<int> progressReporter,
      IMessageLogger logger, CancellationToken cancellationToken)
    {
      if(attachments.Count <= 1)
      {
        return Task.FromResult(attachments);
      }

      System.Diagnostics.Debugger.Launch();

      foreach (AttachmentSet attachmentSet in attachments)
      {
        foreach (UriDataAttachment uriAttachment in attachmentSet.Attachments)
        {
          string json = File.ReadAllText(uriAttachment.Uri.LocalPath);

          _coverageResult.Merge(JsonConvert.DeserializeObject<Modules>(json));
        }
      }

      (string report, string fileName) report = GetCoverageReport(_coverageResult);
      string reportDirectory = Path.Combine(Path.GetTempPath(), new Guid().ToString());

      Directory.CreateDirectory(reportDirectory);
      string filePath = Path.Combine(reportDirectory, report.fileName);
      File.WriteAllText(filePath, report.report);

      attachments = new[] { new AttachmentSet(new Uri(filePath), report.fileName) }.Concat(attachments).ToList();
      return Task.FromResult(attachments);
    }

    public bool SupportsIncrementalProcessing => true;

    private (string report, string fileName) GetCoverageReport(CoverageResult coverageResult)
    {
      var reporterFactory = new ReporterFactory("json");
      IReporter reporter = reporterFactory.CreateReporter();

      try
      {
        return (reporter.Report(coverageResult, new DummySourceRootTranslator()), Path.ChangeExtension(CoverletConstants.DefaultFileName, reporter.Extension));
      }
      catch (Exception ex)
      {
        throw new CoverletDataCollectorException($"{CoverletConstants.DataCollectorName}: Failed to get coverage report", ex);
      }
    }
  }
}
