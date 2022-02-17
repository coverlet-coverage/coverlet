// Remember to use full name because adding new using directives change line numbers

namespace Coverlet.Core.Samples.Tests
{
    public class MethodsWithExcludeFromCodeCoverageAttr_NestedStateMachines
    {
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        public async System.Threading.Tasks.Task NestedStateMachines()
        {
            await System.Threading.Tasks.Task.Run(async () => await System.Threading.Tasks.Task.Delay(50));
        }

        public int Test()
        {
            return 0;
        }
    }  
}