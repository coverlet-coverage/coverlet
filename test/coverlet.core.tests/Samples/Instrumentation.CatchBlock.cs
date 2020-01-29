// Remember to use full name because adding new using directives change line numbers

using System.Threading.Tasks;

namespace Coverlet.Core.Samples.Tests
{
    public class CatchBlock
    {
        public int Parse(string str)
        {
            try
            {
                return int.Parse(str);
            }
            catch
            {
                throw;
            }
        }

        public async Task<int> ParseAsync(string str)
        {
            try
            {
                return int.Parse(str);
            }
            catch
            {
                await Task.Delay(0);

                throw;
            }
        }

        public void Test()
        {
            Parse(nameof(Test).Length.ToString());
            try
            {
                Parse(nameof(Test));
            }
            catch { }
        }

        public async Task TestAsync()
        {
            await ParseAsync(nameof(Test).Length.ToString());
            try
            {
                await ParseAsync(nameof(Test));
            }
            catch { }
        }
    }
}
