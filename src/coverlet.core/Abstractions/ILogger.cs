// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Coverlet.Core.Abstractions
{
  internal interface ILogger
  {
    void LogVerbose(string message);
    void LogInformation(string message, bool important = false);
    void LogWarning(string message);
    void LogError(string message);
    void LogError(Exception exception);
  }
}
