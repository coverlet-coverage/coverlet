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
using Coverlet.Core.Reporters;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Newtonsoft.Json;
using System.Diagnostics;
using Coverlet.Core.Helpers;

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

      string[] formats = _reportFormatParser.ParseReportFormats(configurationElement);
      bool deterministic = _reportFormatParser.ParseDeterministicReport(configurationElement);
      bool useSourceLink = _reportFormatParser.ParseUseSourceLink(configurationElement);
      bool reportMerging = _reportFormatParser.ParseReportMerging(configurationElement);

      AttachDebugger();

      if (!reportMerging) return Task.FromResult(attachments);

      IList<IReporter> reporters = CreateReporters(formats).ToList();

      if (attachments.Count > 1)
      {
        _coverageResult.Parameters = new CoverageParameters() {DeterministicReport = deterministic, UseSourceLink = useSourceLink };
        
        var fileAttachments = attachments.SelectMany(x => x.Attachments.Where(IsFileAttachment)).ToList();
        string mergeFilePath = Path.GetDirectoryName(fileAttachments.First().Uri.LocalPath);

        MergeExistingJsonReports(attachments);

        RemoveObsoleteReports(fileAttachments);

        AttachmentSet mergedFileAttachment = WriteCoverageReports(reporters, mergeFilePath, _coverageResult);

        attachments = new List<AttachmentSet> { mergedFileAttachment };
      }

      return Task.FromResult(attachments);
    }

    private static void RemoveObsoleteReports(List<UriDataAttachment> fileAttachments)
    {
      fileAttachments.ForEach(x =>
      {
        string directory = Path.GetDirectoryName(x.Uri.LocalPath);
        if (! string.IsNullOrEmpty(directory) && Directory.Exists(directory))
          Directory.Delete(directory, true);
      });
    }

    private void MergeExistingJsonReports(IEnumerable<AttachmentSet> attachments)
    {
      foreach (AttachmentSet attachmentSet in attachments)
      {
        attachmentSet.Attachments.Where(IsFileWithJsonExt).ToList().ForEach(x =>
          MergeWithCoverageResult(x.Uri.LocalPath, _coverageResult)
        );
      }
    }

    private AttachmentSet WriteCoverageReports(IEnumerable<IReporter> reporters, string directory, CoverageResult coverageResult)
    {
      var attachment = new AttachmentSet(new Uri(CoverletConstants.DefaultUri), string.Empty);
      foreach (IReporter reporter in reporters)
      {
        string report = GetCoverageReport(coverageResult, reporter);
        var file = new FileInfo(Path.Combine(directory, Path.ChangeExtension(CoverletConstants.DefaultFileName, reporter.Extension)));
        file.Directory?.Create();
        File.WriteAllText(file.FullName, report);
        attachment.Attachments.Add(new UriDataAttachment(new Uri(file.FullName),string.Empty));
      }
      return attachment;
    }

    private static bool IsFileWithJsonExt(UriDataAttachment x)
    {
      return IsFileAttachment(x) && Path.GetExtension(x.Uri.AbsolutePath).Equals(".json");
    }

    private static bool IsFileAttachment(UriDataAttachment x)
    {
      return x.Uri.IsFile;
    }

    private void MergeWithCoverageResult(string filePath, CoverageResult coverageResult)
    {
      string json = File.ReadAllText(filePath);
      coverageResult.Merge(JsonConvert.DeserializeObject<Modules>(json));
    }

    private string GetCoverageReport(CoverageResult coverageResult, IReporter reporter)
    {
      try
      {
        // empty source root translator returns the original path for deterministic report
        return reporter.Report(coverageResult, new SourceRootTranslator());
      }
      catch (Exception ex)
      {
        throw new CoverletDataCollectorException(
          $"{CoverletConstants.DataCollectorName}: Failed to get coverage report", ex);
      }
    }

    private void AttachDebugger()
    {
      if (int.TryParse(Environment.GetEnvironmentVariable("COVERLET_DATACOLLECTOR_POSTPROCESSOR_DEBUG"), out int result) && result == 1)
      {
        Debugger.Launch();
        Debugger.Break();
      }
    }

    private IEnumerable<IReporter> CreateReporters(IEnumerable<string> formats)
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
  }
}
