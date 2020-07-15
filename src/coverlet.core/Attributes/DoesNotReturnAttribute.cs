using System;

namespace Coverlet.Core.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Class)]
    public class DoesNotReturnAttribute : Attribute { }
}
