// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Testing.Platform.Logging;

namespace Coverlet.Extension.Logging
{
  internal class CoverletLoggerAdapter : Coverlet.Core.Abstractions.ILogger
  {
    private readonly Microsoft.Testing.Platform.Logging.ILogger _logger;

    public CoverletLoggerAdapter(ILoggerFactory loggerFactory)
    {
      _logger = loggerFactory.CreateLogger("Coverlet");
    }

    public void LogVerbose(string message)
    {
      _logger.LogTrace(message);
    }

    public void LogInformation(string message, bool important = false)
    {
      if (important)
      {
        _logger.LogInformation($"[Important] {message}");
      }
      else
      {
        _logger.LogInformation(message);
      }
    }

    public void LogWarning(string message)
    {
      _logger.LogWarning(message);
    }

    public void LogError(string message)
    {
      _logger.LogError(message);
    }

    public void LogError(Exception exception)
    {
      _logger.LogError(exception.ToString());
    }
  }
}
