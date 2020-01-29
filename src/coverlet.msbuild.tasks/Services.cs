using System;
using System.Collections.Generic;
using System.Text;
using Coverlet.Core.Abstracts;
using Coverlet.Core.Helpers;
using Coverlet.MSbuild.Tasks;
using Microsoft.Build.Utilities;
using Microsoft.Extensions.DependencyInjection;

namespace coverlet.msbuild.tasks
{
    internal class Services
    {
        public IServiceProvider GetServiceProvider(TaskLoggingHelper taskLoggingHelper)
        {
            IServiceCollection serviceCollection = new ServiceCollection();
            serviceCollection.AddTransient<IRetryHelper, RetryHelper>();
            serviceCollection.AddTransient<IProcessExitHandler, ProcessExitHandler>();
            serviceCollection.AddTransient<IFileSystem, FileSystem>();
            serviceCollection.AddTransient<IConsole, SystemConsole>();
            serviceCollection.AddTransient<ILogger, MSBuildLogger>(x => new MSBuildLogger(taskLoggingHelper));

            // We need to keep singleton/static semantics
            serviceCollection.AddSingleton<IInstrumentationHelper, InstrumentationHelper>();

            return serviceCollection.BuildServiceProvider();
        }
    }
}
