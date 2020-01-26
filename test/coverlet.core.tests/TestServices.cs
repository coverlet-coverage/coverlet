using System;
using System.Collections.Generic;
using System.Text;
using Coverlet.Collector.DataCollection;
using Coverlet.Collector.Utilities;
using Coverlet.Core.Abstracts;
using Coverlet.Core.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;

namespace coverlet.core.tests
{
    internal static class TestServices
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

            serviceCollection.AddTransient<TestPlatformEqtTrace>();
            serviceCollection.AddSingleton<TestPlatformLogger>();
            serviceCollection.AddTransient<DataCollectionLogger>();
            serviceCollection.AddTransient<DataCollectionContext>();


            serviceCollection.AddTransient<IRetryHelper, RetryHelper>();
            serviceCollection.AddTransient<IProcessExitHandler, ProcessExitHandler>();
            serviceCollection.AddTransient<IFileSystem, FileSystem>();
            serviceCollection.AddTransient<IConsole, SystemConsole>();
            serviceCollection.AddTransient<ILogger, CoverletLogger>(x =>
                new CoverletLogger(x.GetService<TestPlatformEqtTrace>(), x.GetService<TestPlatformLogger>()));
            // We need to keep singleton/static semantics
            serviceCollection.AddSingleton<IInstrumentationHelper, InstrumentationHelper>();

            return serviceCollection.BuildServiceProvider();
        }
    }
}
