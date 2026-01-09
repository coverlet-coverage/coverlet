// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Coverlet.Core.Abstractions;

namespace Coverlet.MTP
{
  /// <summary>
  /// Implementation of IProcessExitHandler for Microsoft Testing Platform.
  /// Supports both in-process and out-of-process execution modes.
  /// </summary>
  internal sealed class MtpProcessExitHandler : IProcessExitHandler
  {
    private readonly bool _isOutOfProcess;

    /// <summary>
    /// Creates a new instance of MtpProcessExitHandler.
    /// </summary>
    /// <param name="isOutOfProcess">
    /// True if running in out-of-process mode (controller manages lifecycle).
    /// False if running in-process (needs AppDomain.ProcessExit).
    /// </param>
    public MtpProcessExitHandler(bool isOutOfProcess = true)
    {
      _isOutOfProcess = isOutOfProcess;
    }

    public void Add(EventHandler handler)
    {
      if (_isOutOfProcess)
      {
        // Out-of-process: MTP handles cleanup via OnTestHostProcessExitedAsync
        // No need to register ProcessExit handler
        return;
      }

      // In-process: Register the handler for AppDomain.ProcessExit
      AppDomain.CurrentDomain.ProcessExit += handler;
    }
  }
}
