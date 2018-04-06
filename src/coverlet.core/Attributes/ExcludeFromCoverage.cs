using System;

namespace Coverlet.Core.Attributes
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Class)]
    public class ExcludeFromCoverageAttribute : Attribute { }
}