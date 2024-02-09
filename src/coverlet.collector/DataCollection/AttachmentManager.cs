// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel;
using System.IO;
using Coverlet.Collector.Utilities;
using Coverlet.Collector.Utilities.Interfaces;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;

namespace Coverlet.Collector.DataCollection
{
  /// <summary>
  /// Manages coverage report attachments
  /// </summary>
  internal class AttachmentManager : IDisposable
  {
    private readonly DataCollectionSink _dataSink;
    private readonly TestPlatformEqtTrace _eqtTrace;
    private readonly TestPlatformLogger _logger;
    private readonly DataCollectionContext _dataCollectionContext;
    private readonly IFileHelper _fileHelper;
    private readonly IDirectoryHelper _directoryHelper;
    private readonly ICountDownEvent _countDownEvent;
    private readonly string _reportDirectory;

    public AttachmentManager(DataCollectionSink dataSink, DataCollectionContext dataCollectionContext, TestPlatformLogger logger, TestPlatformEqtTrace eqtTrace, ICountDownEvent countDownEvent)
        : this(dataSink,
              dataCollectionContext,
              logger,
              eqtTrace,
              Guid.NewGuid().ToString(),
              new FileHelper(),
              new DirectoryHelper(),
              countDownEvent)
    {
    }

    public AttachmentManager(DataCollectionSink dataSink, DataCollectionContext dataCollectionContext, TestPlatformLogger logger, TestPlatformEqtTrace eqtTrace, string reportDirectoryName, IFileHelper fileHelper, IDirectoryHelper directoryHelper, ICountDownEvent countDownEvent)
    {
      // Store input variables
      _dataSink = dataSink;
      _dataCollectionContext = dataCollectionContext;
      _logger = logger;
      _eqtTrace = eqtTrace;
      _fileHelper = fileHelper;
      _directoryHelper = directoryHelper;
      _countDownEvent = countDownEvent;

      // Report directory to store the coverage reports.
      _reportDirectory = Path.Combine(Path.GetTempPath(), reportDirectoryName);

      // Register events
      _dataSink.SendFileCompleted += OnSendFileCompleted;
    }

    /// <summary>
    /// Sends coverage report to test platform
    /// </summary>
    /// <param name="coverageReport">Coverage report</param>
    /// <param name="coverageReportFileName">Coverage report file name</param>
    public void SendCoverageReport(string coverageReport, string coverageReportFileName)
    {
      // Save coverage report to file
      string coverageReportPath = SaveCoverageReport(coverageReport, coverageReportFileName);

      // Send coverage attachment to test platform.
      SendAttachment(coverageReportPath);
    }

    /// <summary>
    /// Disposes attachment manager
    /// </summary>
    public void Dispose()
    {
      // Unregister events
      try
      {
        _countDownEvent.Wait();
        if (_dataSink != null)
        {
          _dataSink.SendFileCompleted -= OnSendFileCompleted;
        }
        CleanupReportDirectory();
      }
      catch (Exception ex)
      {
        _logger.LogWarning(ex.ToString());
      }
    }

    /// <summary>
    /// Saves coverage report to file system
    /// </summary>
    /// <param name="report">Coverage report</param>
    /// <param name="reportFileName">Coverage report file name</param>
    /// <returns>Coverage report file path</returns>
    private string SaveCoverageReport(string report, string reportFileName)
    {
      try
      {
        _directoryHelper.CreateDirectory(_reportDirectory);
        string filePath = Path.Combine(_reportDirectory, reportFileName);
        _fileHelper.WriteAllText(filePath, report);
        _eqtTrace.Info("{0}: Saved coverage report to path: '{1}'", CoverletConstants.DataCollectorName, filePath);

        return filePath;
      }
      catch (Exception ex)
      {
        throw new CoverletDataCollectorException($"{CoverletConstants.DataCollectorName}: Failed to save coverage report '{reportFileName}' in directory '{_reportDirectory}'", ex);
      }
    }

    /// <summary>
    /// SendFileCompleted event handler
    /// </summary>
    /// <param name="sender">Sender</param>
    /// <param name="e">Event args</param>
    public void OnSendFileCompleted(object sender, AsyncCompletedEventArgs e)
    {
      try
      {
        _eqtTrace.Verbose("{0}: SendFileCompleted received", CoverletConstants.DataCollectorName);
      }
      catch (Exception ex)
      {
        _logger.LogWarning(ex.ToString());
      }
      finally
      {
        _countDownEvent.Signal();
      }
    }

    /// <summary>
    /// Sends attachment file to test platform
    /// </summary>
    /// <param name="attachmentPath">Attachment file path</param>
    private void SendAttachment(string attachmentPath)
    {
      if (_fileHelper.Exists(attachmentPath))
      {
        // Send coverage attachment to test platform.
        _eqtTrace.Verbose("{0}: Sending attachment to test platform", CoverletConstants.DataCollectorName);
        _dataSink.SendFileAsync(_dataCollectionContext, attachmentPath, false);
      }
      else
      {
        _eqtTrace.Warning("{0}: Attachment file does not exist", CoverletConstants.DataCollectorName);
      }
    }

    /// <summary>
    /// Cleans up coverage report directory
    /// </summary>
    private void CleanupReportDirectory()
    {
      try
      {
        if (_directoryHelper.Exists(_reportDirectory))
        {
          _directoryHelper.Delete(_reportDirectory, true);
          _eqtTrace.Verbose("{0}: Deleted report directory: '{1}'", CoverletConstants.DataCollectorName, _reportDirectory);
        }
      }
      catch (Exception ex)
      {
        throw new CoverletDataCollectorException($"{CoverletConstants.DataCollectorName}: Failed to cleanup report directory: '{_reportDirectory}'", ex);
      }
    }
  }
}
