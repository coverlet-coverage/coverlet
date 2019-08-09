using System.Threading.Tasks;

namespace Coverlet.Core.Samples.Tests
{
    public class Conditions
    {
        public int If(bool condition)
        {
            if (condition)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }
    }
}
