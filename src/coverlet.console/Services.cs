using System;
using System.Collections.Generic;
using System.Text;
using Coverlet.Console.Logging;
using Coverlet.Core.Abstracts;
using Coverlet.Core.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace coverlet.console
{
    internal static class Services
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
            serviceCollection.AddTransient<IFileSystem, FileSystem>();
            serviceCollection.AddTransient<IConsole, SystemConsole>();
            serviceCollection.AddTransient<ILogger, ConsoleLogger>();

            // We need to keep singleton/static semantics
            serviceCollection.AddSingleton<IInstrumentationHelper, InstrumentationHelper>();

            return serviceCollection.BuildServiceProvider();
        }
    }
}
