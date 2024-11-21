// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Xunit.v3;

namespace Coverlet.Tests.Xunit.Extensions
{

  public enum SupportedOS
  {
    FreeBSD = 1,
    Linux = 2,
    macOS = 3,
    Windows = 4,
  }
  public sealed class SupportedOSAttribute(params SupportedOS[] supportedOSes) :
       BeforeAfterTestAttribute
  {
    private static readonly Dictionary<SupportedOS, OSPlatform> s_osMappings = new()
    {
        { SupportedOS.FreeBSD, OSPlatform.Create("FreeBSD") },
        { SupportedOS.Linux, OSPlatform.Linux },
        { SupportedOS.macOS, OSPlatform.OSX },
        { SupportedOS.Windows, OSPlatform.Windows },
    };

    public override ValueTask Before(MethodInfo methodUnderTest, IXunitTest test)
    {
      var match = false;

      foreach (var supportedOS in supportedOSes)
      {
        if (!s_osMappings.TryGetValue(supportedOS, out var osPlatform))
          throw new ArgumentException($"Supported OS value '{supportedOS}' is not a known OS", nameof(supportedOSes));

        if (RuntimeInformation.IsOSPlatform(osPlatform))
        {
          match = true;
          break;
        }
      }

      // We use the dynamic skip exception message pattern to turn this into a skipped test
      // when it's not running on one of the targeted OSes
      if (!match)
        throw new Exception($"$XunitDynamicSkip$This test is not supported on {RuntimeInformation.OSDescription}");

      return default;
    }
  }
}
