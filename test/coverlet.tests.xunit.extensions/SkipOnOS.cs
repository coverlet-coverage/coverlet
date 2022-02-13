using System;
using System.Runtime.InteropServices;

namespace Coverlet.Tests.Xunit.Extensions
{
    public enum OS
    {
        Linux,
        MacOS,
        Windows
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true)]
    public class SkipOnOSAttribute : Attribute, ITestCondition
    {
        private readonly OS _os;
        private readonly string _reason;

        public SkipOnOSAttribute(OS os, string reason = "") => (_os, _reason) = (os, reason);

        public bool IsMet => _os switch
        {
            OS.Linux => !RuntimeInformation.IsOSPlatform(OSPlatform.Linux),
            OS.MacOS => !RuntimeInformation.IsOSPlatform(OSPlatform.OSX),
            OS.Windows => !RuntimeInformation.IsOSPlatform(OSPlatform.Windows),
            _ => throw new NotSupportedException($"Not supported OS {_os}")
        };

        public string SkipReason => $"OS not supported{(string.IsNullOrEmpty(_reason) ? "" : $", {_reason}")}";
    }
}
