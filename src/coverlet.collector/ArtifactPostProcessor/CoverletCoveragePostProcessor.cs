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
    private CoverageResult _coverageResult;
    private ReportFormatParser _reportFormatParser;
    private IMessageLogger _logger;

    public bool SupportsIncrementalProcessing => true;

    public IEnumerable<Uri> GetExtensionUris() => new[] { new Uri(CoverletConstants.DefaultUri) };

    public Task<ICollection<AttachmentSet>> ProcessAttachmentSetsAsync(XmlElement configurationElement,
      ICollection<AttachmentSet> attachments, IProgress<int> progressReporter,
      IMessageLogger logger, CancellationToken cancellationToken)
    {
      _reportFormatParser ??= new ReportFormatParser();
      _coverageResult ??= new CoverageResult();
      _coverageResult.Modules ??= new Modules();
      _logger = logger;

      if (attachments.Count > 1)
      {
        System.Diagnostics.Debugger.Launch();
      }

      // think about what configuration is mandatory and how to exit if not there
      string[] formats = _reportFormatParser.ParseReportFormats(configurationElement);
      string mergeDirectory = ParseMergeDirectory(configurationElement);
      // validate that json reports where created otherwise we cannot merge them
      IList<IReporter> reporters = CreateReporter(formats).ToList();

      // add more validation logic here, e.g.  for specified json reports
      if (mergeDirectory == null) return Task.FromResult(attachments);

      MergeExistingJsonReports(attachments, mergeDirectory);

      WriteCoverageReports(reporters, mergeDirectory, _coverageResult);

      return Task.FromResult(attachments);
    }

    private void MergeExistingJsonReports(ICollection<AttachmentSet> attachments, string mergeDirectory)
    {
      string jsonFileName = Path.ChangeExtension(CoverletConstants.DefaultFileName, "json");

      foreach (AttachmentSet attachmentSet in attachments)
      {
        foreach (UriDataAttachment uriAttachment in attachmentSet.Attachments)
        {
          MergeWithCoverageResult(uriAttachment.Uri.LocalPath, _coverageResult);
        }
      }

      Directory.CreateDirectory(mergeDirectory);
      string jsonFilePath = Path.Combine(mergeDirectory, jsonFileName);

      MergeIntermediateResultWhenExist(jsonFilePath, _coverageResult);
    }

    private void WriteCoverageReports(IEnumerable<IReporter> reporters, string directory, CoverageResult coverageResult)
    {
      foreach (IReporter reporter in reporters)
      {
        string report = GetCoverageReport(coverageResult, reporter);
        string filePath = Path.Combine(directory, Path.ChangeExtension(CoverletConstants.DefaultFileName, reporter.Extension));
        File.WriteAllText(filePath, report);
      }
    }

    private void MergeIntermediateResultWhenExist(string filePath, CoverageResult coverageResult)
    {
      if (File.Exists(filePath))
      {
        MergeWithCoverageResult(filePath, coverageResult);
      }
    }

    private void MergeWithCoverageResult(string filePath, CoverageResult coverageResult)
    {
      string json = File.ReadAllText(filePath);
      coverageResult.Merge(JsonConvert.DeserializeObject<Modules>(json));
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

    private IEnumerable<IReporter> CreateReporter(IEnumerable<string> formats)
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

      return reporters;
    }

    private static string ParseMergeDirectory(XmlElement configurationElement)
    {
      XmlElement mergeWithElement = configurationElement[CoverletConstants.MergeDirectory];
      return mergeWithElement?.InnerText;
    }
  }
}
