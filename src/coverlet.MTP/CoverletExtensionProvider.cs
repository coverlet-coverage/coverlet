// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using coverlet.Extension.Collector;
using Microsoft.Testing.Extensions.Diagnostics;
using Microsoft.Testing.Platform.Builder;
using Microsoft.Testing.Platform.Extensions.TestHostControllers;
using Microsoft.Testing.Platform.Services;

namespace coverlet.Extension
{
  public static class CoverletExtensionProvider
  {
    public static void AddCoverletExtensionProvider(this ITestApplicationBuilder builder, bool ignoreIfNotSupported = false)
    {
      CoverletExtension _extension = new();
      CoverletExtensionConfiguration coverletExtensionConfiguration = new();
      if (ignoreIfNotSupported)
      {
#if !NETCOREAPP
              coverletExtensionConfiguration.Enable = false;
#endif
      }

      builder.TestHostControllers.AddEnvironmentVariableProvider(serviceProvider
          => new CoverletExtensionEnvironmentVariableProvider(
              serviceProvider.GetConfiguration(),
              serviceProvider.GetCommandLineOptions(),
              serviceProvider.GetLoggerFactory()));

      // Fix for CS0029 and CS1662:
      // Ensure that CoverletExtensionCollector implements ITestHostProcessLifetimeHandler
      builder.TestHostControllers.AddProcessLifetimeHandler(static serviceProvider
          => new CoverletExtensionCollector(
              serviceProvider.GetLoggerFactory(),
              serviceProvider.GetCommandLineOptions()) as ITestHostProcessLifetimeHandler);

      builder.CommandLine.AddProvider(() => new CoverletExtensionCommandLineProvider(_extension));

    }
  }
}
