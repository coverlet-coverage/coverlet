// Remember to use full name because adding new using directives change line numbers

namespace Coverlet.Core.Samples.Tests
{
    public class Yield
    {
        public System.Collections.Generic.IEnumerable<int> One()
        {
            yield return 1;
        }

        public System.Collections.Generic.IEnumerable<int> Two()
        {
            yield return 1;
            yield return 2;
        }
    }
}
