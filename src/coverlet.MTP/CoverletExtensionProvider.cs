// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using coverlet.Extension.Collector;
using Coverlet.MTP.CommandLine;
using Microsoft.Testing.Platform.Builder;
using Microsoft.Testing.Platform.Configurations;
using Microsoft.Testing.Platform.Extensions.TestHostControllers;
using Microsoft.Testing.Platform.Services;
using Microsoft.Testing.Extensions.Diagnostics;

namespace coverlet.Extension
{
  public static class CoverletExtensionProvider
  {
    public static void AddCoverletExtensionProvider(this ITestApplicationBuilder builder, bool ignoreIfNotSupported = false)
    {
      CoverletExtension _extension = new();

      builder.TestHostControllers.AddEnvironmentVariableProvider(serviceProvider
          => new CoverletExtensionEnvironmentVariableProvider(
              serviceProvider.GetConfiguration(),
              serviceProvider.GetCommandLineOptions(),
              serviceProvider.GetLoggerFactory()));

      builder.TestHostControllers.AddProcessLifetimeHandler(static serviceProvider
          =>
          {
            // Configuration may be null when coverlet-coverage is not enabled.
            // The collector checks for --coverlet-coverage flag before using configuration.
            IConfiguration configuration = serviceProvider.GetConfiguration();
            return new CoverletExtensionCollector(
                serviceProvider.GetLoggerFactory(),
                serviceProvider.GetCommandLineOptions(),
                configuration) as ITestHostProcessLifetimeHandler;
          });

      builder.CommandLine.AddProvider(() => new CoverletExtensionCommandLineProvider(_extension));
    }
  }
}
