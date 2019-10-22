using System;

namespace Coverlet.Core.Extensions
{
    public static class DependencyInjectionExtensions
    {
        public static T GetService<T>(this IServiceProvider serviceProvider)
        {
            return (T)serviceProvider.GetService(typeof(T));
        }
    }
}
