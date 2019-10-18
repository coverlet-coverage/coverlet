using System;
using Coverlet.Core.Attributes;

namespace coverlet.core.Extensions
{
    public static class DependencyInjectionExtensions
    {
        [ExcludeFromCoverage]
        public static T GetService<T>(this IServiceProvider serviceProvider)
        {
            return (T)serviceProvider.GetService(typeof(T));
        }
    }
}
