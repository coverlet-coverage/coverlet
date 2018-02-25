using System;

namespace Coverlet.Core.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class ExcludeFromCoverageAttribute : Attribute { }
}