// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

namespace coverlet.collector.ArtifactPostProcessor
{
  public class CoverletCoveragePostProcessor : IDataCollectorAttachmentProcessor
  {
    private readonly CoverageResult _coverageResult = new();
    private ReportFormatParser _reportFormatParser;
    private IMessageLogger _logger;

    public CoverletCoveragePostProcessor()
    {
      _coverageResult.Modules = new Modules();
    }

    public bool SupportsIncrementalProcessing => true;

    public IEnumerable<Uri> GetExtensionUris() => new[] { new Uri(CoverletConstants.DefaultUri) };

    public Task<ICollection<AttachmentSet>> ProcessAttachmentSetsAsync(XmlElement configurationElement,
      ICollection<AttachmentSet> attachments, IProgress<int> progressReporter,
      IMessageLogger logger, CancellationToken cancellationToken)
    {
      _reportFormatParser ??= new ReportFormatParser();
      _logger = logger;

      if (attachments.Count > 1)
      {
        System.Diagnostics.Debugger.Launch();
      }

      // think about what configuration is mandatory and how to exit if not there
      string[] formats = _reportFormatParser.ParseReportFormats(configurationElement);
      string mergeDirectory = ParseMergeDirectory(configurationElement);
      IReporter reporter = CreateReporter(formats);
      string fileName = Path.ChangeExtension(CoverletConstants.DefaultFileName, reporter.Extension);

      if (mergeDirectory == null) return Task.FromResult(attachments);

      foreach (AttachmentSet attachmentSet in attachments)
      {
        foreach (UriDataAttachment uriAttachment in attachmentSet.Attachments)
        {
          MergeWithCoverageResult(uriAttachment.Uri.LocalPath);
        }
      }

      Directory.CreateDirectory(mergeDirectory);
      string filePath = Path.Combine(mergeDirectory, fileName);

      MergeIntermediateResultWhenExist(filePath);

      string report = GetCoverageReport(_coverageResult, reporter);

      File.WriteAllText(filePath, report);

      return Task.FromResult(attachments);
    }

    private void MergeIntermediateResultWhenExist(string filePath)
    {
      if (File.Exists(filePath))
      {
        MergeWithCoverageResult(filePath);
      }
    }

    private void MergeWithCoverageResult(string filePath)
    {
      string json = File.ReadAllText(filePath);
      _coverageResult.Merge(JsonConvert.DeserializeObject<Modules>(json));
// think about merging reports with different format -> e.g. reportgenerator core
// or maybe we can deserialize different reports into Modules???
// or always create additionally the specified format and overwrite intermediate results
    }

    private string GetCoverageReport(CoverageResult coverageResult, IReporter reporter)
    {
      try
      {
        // check if we need the sourceRootTranslator here 
        return reporter.Report(coverageResult, new DummySourceRootTranslator());
      }
      catch (Exception ex)
      {
        throw new CoverletDataCollectorException(
          $"{CoverletConstants.DataCollectorName}: Failed to get coverage report", ex);
      }
    }

    private IReporter CreateReporter(IEnumerable<string> formats)
    {
      IEnumerable<IReporter> reporters = formats.Select(format =>
      {
        var reporterFactory = new ReporterFactory(format);
        if (!reporterFactory.IsValidFormat())
        {
          _logger.SendMessage(TestMessageLevel.Warning, $"Invalid report format '{format}'");
          return null;
        }
        return reporterFactory.CreateReporter();
      }).Where(r => r != null);

      // if we want to consider the case where multiple reporters are specified, the first one should be taken
      IReporter reporter = reporters.FirstOrDefault();

      // current prototype only with json format
      var reporterFactory = new ReporterFactory("json");
      reporter = reporterFactory.CreateReporter();

      return reporter;
    }

    private static string ParseMergeDirectory(XmlElement configurationElement)
    {
      XmlElement mergeWithElement = configurationElement[CoverletConstants.MergeDirectory];
      return mergeWithElement?.InnerText;
    }
  }
}
