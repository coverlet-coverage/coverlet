using System;

namespace Coverlet.Core.Attributes
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor)]
    public class ExcludeFromCoverageAttribute : Attribute { }
}