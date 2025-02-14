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
using System.Diagnostics;

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

        // does merge only work for json extensions, how are they created now if isn't specified in runsettings
        MergeExistingJsonReports(attachments);

        RemoveObsoleteReports(fileAttachments);

        var mergedFileAttachment = WriteCoverageReports(reporters, mergeFilePath, _coverageResult);
        // check if we can remove more than just the json extension files
        // maybe don't remove the merged json file as it is printed to the console

        attachments = new List<AttachmentSet> { mergedFileAttachment };
        // create new attachment set with only the merged file (add new parameter to pass in the output directory; if not specified use first one)
      }

      return Task.FromResult(attachments);
    }

    // proper documentation for the whole feature
    // integration tests? maybe in coverlet.integration.tests.Collectors?
    // double check that new parameter is only useable for collectors
    private void RemoveObsoleteReports(List<UriDataAttachment> fileAttachments)
    {
      fileAttachments.ForEach(x => File.Delete(x.Uri.LocalPath));
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
        //throws exceptions -- check what the problem is
        string filePath = Path.Combine(directory, Path.ChangeExtension(CoverletConstants.DefaultFileName, reporter.Extension));
        File.WriteAllText(filePath, report);
        attachment.Attachments.Add(new UriDataAttachment(new Uri(filePath),string.Empty));
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
        // check if we need the sourceRootTranslator here 
        return reporter.Report(coverageResult, new DummySourceRootTranslator());
      }
      catch (Exception ex)
      {
        throw new CoverletDataCollectorException(
          $"{CoverletConstants.DataCollectorName}: Failed to get coverage report", ex);
      }
    }

    private void AttachDebugger()
    {
      if (int.TryParse(Environment.GetEnvironmentVariable("COVERLET_DATACOLLECTOR_ATTACHMENT_DEBUG"), out int result) && result == 1)
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
