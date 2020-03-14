using System;

namespace Coverlet.Core
{
    internal static class DependencyInjection
    {
        private static IServiceProvider _serviceProvider;

        public static IServiceProvider Current
        {
            get
            {
                return _serviceProvider;
            }
        }

        public static void Set(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
    }
}
