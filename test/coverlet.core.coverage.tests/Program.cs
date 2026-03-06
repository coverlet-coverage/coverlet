// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using coverlet.core.coverage.tests;
using Coverlet.Core.Tests.Infrastructure;

// Handle ProcessExecutor invocations when running as a child process
if (ProcessExecutor.TryExecute(args))
{
  return Environment.ExitCode;
}

// Normal test execution via xunit.v3.mtp-v2
// Check for automated/inline arguments (used by xunit console runner)
if (args.Any(arg => arg == "-automated" || arg == "@@"))
{
  return await Xunit.Runner.InProc.SystemConsole.ConsoleRunner.Run(args);
}
else
{
  return await Xunit.MicrosoftTestingPlatform.TestPlatformTestFramework.RunAsync(args, SelfRegisteredExtensions.AddSelfRegisteredExtensions);
}
