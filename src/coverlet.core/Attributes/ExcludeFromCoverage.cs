using System;

namespace Coverlet.Core.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Constructor)]
    public sealed class ExcludeFromCoverageAttribute : Attribute { }
}