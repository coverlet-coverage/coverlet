// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Testing.Platform.Builder;

namespace coverlet.Extension
{
  public static class TestingPlatformBuilderHook
  {
    /// <summary>
    /// Adds crash dump support to the Testing Platform Builder.
    /// </summary>
    /// <param name="testApplicationBuilder">The test application builder.</param>
    /// <param name="_">The command line arguments.</param>
    public static void AddExtensions(ITestApplicationBuilder testApplicationBuilder, string[] _)
    {
      // Ensure AddCoverletCoverageProvider is implemented or accessible
      testApplicationBuilder.AddCoverletExtensionProvider();
    }
  }
}
