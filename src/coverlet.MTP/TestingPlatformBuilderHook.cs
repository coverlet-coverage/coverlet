// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Testing.Platform.Builder;

namespace coverlet.Extension
{
  public static class TestingPlatformBuilderHook
  {
    /// <summary>
    /// Adds Coverlet code coverage extension support to the Testing Platform Builder.
    /// </summary>
    /// <param name="testApplicationBuilder">The test application builder.</param>
    /// <param name="_">The command line arguments (unused).</param>
    public static void AddExtensions(ITestApplicationBuilder testApplicationBuilder, string[] _)
    {
      testApplicationBuilder.AddCoverletExtensionProvider();
    }
  }
}
