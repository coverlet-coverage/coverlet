// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Testing.Platform.Builder;
using Coverlet.MTP.InProcDataCollection;

namespace Coverlet.MTP.Configuration;

/// <summary>
/// Extension methods to register coverlet.MTP with Microsoft Testing Platform
/// </summary>
public static class CoverletConfigurationExtensions
{
  /// <summary>
  /// Adds Coverlet code coverage to the test application
  /// </summary>
  public static void AddCoverletCodeCoverage(this ITestApplicationBuilder builder)
  {
    // Register the test session lifetime handler
    builder.TestHost.AddTestSessionLifetimeHandler(serviceProvider =>
        new CoverletTestSessionHandler());
  }
}
