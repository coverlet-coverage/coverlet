using System;
using System.Runtime.InteropServices;

namespace Coverlet.Tests.Xunit.Extensions
{
    [Flags]
    public enum OS
    {
        Linux = 1,
        MacOS = 2,
        Windows = 4
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true)]
    public class SkipOnOSAttribute : Attribute, ITestCondition
    {
        private readonly OS _os;

        public SkipOnOSAttribute(OS os) => _os = os;

        public bool IsMet => _os switch
        {
            OS.Linux => !RuntimeInformation.IsOSPlatform(OSPlatform.Linux),
            OS.MacOS => !RuntimeInformation.IsOSPlatform(OSPlatform.OSX),
            OS.Windows => !RuntimeInformation.IsOSPlatform(OSPlatform.Windows),
            _ => throw new NotSupportedException($"Not supported OS {_os}")
        };

        public string SkipReason => "OS not supported";
    }
}
