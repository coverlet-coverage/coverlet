// Remember to use full name because adding new using directives change line numbers

namespace Coverlet.Core.Samples.Tests
{
    public class MethodsWithExcludeFromCodeCoverageAttr_Issue670
    {
        public void Test(string input)
        {
            MethodsWithExcludeFromCodeCoverageAttr_Issue670_Startup obj = new MethodsWithExcludeFromCodeCoverageAttr_Issue670_Startup();
            obj.ObjectExtension(input);
        }
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class MethodsWithExcludeFromCodeCoverageAttr_Issue670_Startup
    {
        public void UseExceptionHandler(System.Action<MethodsWithExcludeFromCodeCoverageAttr_Issue670_Startup> action)
        {
            action(this);
        }

        public async void Run(System.Func<MethodsWithExcludeFromCodeCoverageAttr_Issue670_Context, System.Threading.Tasks.Task> func)
        {
            await func(new MethodsWithExcludeFromCodeCoverageAttr_Issue670_Context());
        }
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class MethodsWithExcludeFromCodeCoverageAttr_Issue670_Context
    {
        public System.Threading.Tasks.Task SimulateAsyncWork(int val)
        {
            return System.Threading.Tasks.Task.Delay(System.Math.Min(val, 50));
        }
    }

    public static class MethodsWithExcludeFromCodeCoverageAttr_Issue670_Ext
    {
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        public static void ObjectExtension(this Coverlet.Core.Samples.Tests.MethodsWithExcludeFromCodeCoverageAttr_Issue670_Startup obj, string input)
        {
            obj.UseExceptionHandler(o =>
            {
                o.Run(async context =>
                {
                    if (context != null)
                    {
                        await context.SimulateAsyncWork(input.Length);
                    }
                });
            });
        }
    }
}