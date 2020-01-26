using System;
using Coverlet.Collector.DataCollection;
using Coverlet.Collector.Utilities;
using Coverlet.Core.Abstracts;
using Coverlet.Core.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;

namespace coverlet.collector
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

            serviceCollection.AddTransient<DataCollectionLogger>();
            serviceCollection.AddTransient<DataCollectionContext>();
            serviceCollection.AddTransient<DataCollectionEnvironmentContext>();
            serviceCollection.AddSingleton<TestPlatformLogger>();
            serviceCollection.AddTransient<TestPlatformEqtTrace>();
            serviceCollection.AddTransient<IRetryHelper, RetryHelper>();
            serviceCollection.AddTransient<IProcessExitHandler, ProcessExitHandler>();
            serviceCollection.AddTransient<IFileSystem, FileSystem>();
            serviceCollection.AddTransient<ILogger, CoverletLogger>();
            // We need to keep singleton/static semantics
            serviceCollection.AddSingleton<IInstrumentationHelper, InstrumentationHelper>();

            return serviceCollection.BuildServiceProvider();
        }
    }
}
