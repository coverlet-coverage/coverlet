// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using coverlet.MTP.Collector;
using Coverlet.MTP.InProcess;
using coverlet.MTP.EnvironmentVariables;
using Microsoft.Testing.Platform.Builder;
using Microsoft.Testing.Platform.Configurations;
using Microsoft.Testing.Platform.Extensions.TestHostControllers;
using Microsoft.Testing.Platform.Services;

namespace coverlet.MTP;

public static class CoverletExtensionProvider
{
  public static void AddCoverletExtensionProvider(this ITestApplicationBuilder builder, bool ignoreIfNotSupported = false)
  {
    // ============================================================
    // OUT-OF-PROCESS EXTENSIONS (run in controller process)
    // These observe the test host from outside
    // ============================================================

    // Environment variable provider - passes configuration to test host
    builder.TestHostControllers.AddEnvironmentVariableProvider(serviceProvider
        => new CoverletExtensionEnvironmentVariableProvider(
            serviceProvider.GetConfiguration(),
            serviceProvider.GetCommandLineOptions(),
            serviceProvider.GetLoggerFactory()));

    // Process lifetime handler - instruments before tests, collects after
    builder.TestHostControllers.AddProcessLifetimeHandler(static serviceProvider
        =>
        {
          IConfiguration configuration = serviceProvider.GetConfiguration();
          return new CoverletExtensionCollector(
              serviceProvider.GetLoggerFactory(),
              serviceProvider.GetCommandLineOptions(),
              configuration) as ITestHostProcessLifetimeHandler;
        });

    // ============================================================
    // IN-PROCESS EXTENSIONS (run inside test host process)
    // These run in the same process as the tests
    // ============================================================

    // Test session lifetime handler - flushes coverage data when session ends
    builder.TestHost.AddTestSessionLifetimeHandle(static serviceProvider
        => new CoverletInProcessHandler(serviceProvider.GetLoggerFactory()));

    // ============================================================
    // COMMAND LINE
    // ============================================================

    builder.CommandLine.AddProvider(() => new CommandLine.CoverletExtensionCommandLineProvider(new CoverletExtension()));
  }
}
