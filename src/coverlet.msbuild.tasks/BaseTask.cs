using System;
using Microsoft.Build.Utilities;

namespace Coverlet.MSbuild.Tasks
{
    public abstract class BaseTask : Task
    {
        protected static IServiceProvider ServiceProvider { get; set; }
    }
}
