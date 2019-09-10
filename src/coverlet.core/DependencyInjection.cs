using System;

using Coverlet.Core.Abstracts;
using Coverlet.Core.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace Coverlet.Core
{
    public static class DependencyInjection
    {
        private static Lazy<IServiceProvider> _serviceProvider = new Lazy<IServiceProvider>(() => InitDefaultServices(), true);

        public static IServiceProvider Current
        {
            get
            {
                return _serviceProvider.Value;
            }
        }

        public static void Set(IServiceProvider serviceProvider)
        {
            _serviceProvider = new Lazy<IServiceProvider>(() => serviceProvider);
        }

        private static IServiceProvider InitDefaultServices()
        {
            IServiceCollection serviceCollection = new ServiceCollection();
            serviceCollection.AddTransient<IRetryHelper, RetryHelper>();
            serviceCollection.AddTransient<IProcessExitHandler, ProcessExitHandler>();

            // We need to keep singleton/static semantics
            serviceCollection.AddSingleton<IInstrumentationHelper, InstrumentationHelper>();

            return serviceCollection.BuildServiceProvider();
        }

    }
}
