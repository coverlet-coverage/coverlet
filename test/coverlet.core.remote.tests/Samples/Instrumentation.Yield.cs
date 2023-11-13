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

        public System.Collections.Generic.IEnumerable<int> OneWithSwitch(int n)
        {
            int result;
            switch (n)
            {
                case 0:
                    result = 10;
                    break;
                case 1:
                    result = 11;
                    break;
                case 2:
                    result = 12;
                    break;
                default:
                    result = -1;
                    break;
            }

            yield return result;
        }

        public System.Collections.Generic.IEnumerable<int> Three()
        {
            yield return 1;
            yield return 2;
            yield return 3;
        }

        public System.Collections.Generic.IEnumerable<string> Enumerable(System.Collections.Generic.IList<string> ls)
        {
            foreach (
                    var item
                    in
                    ls
                    )
            {
                yield return item;
            }
        }
    }
}
