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
        }

        public void Test_Catch()
        {
            try
            {
                Parse(nameof(Test));
            }
            catch { }
        }

        public async Task TestAsync()
        {
            await ParseAsync(nameof(Test).Length.ToString());
        }

        public async Task TestAsync_Catch()
        {
            try
            {
                await ParseAsync(nameof(Test));
            }
            catch { }
        }

        public int Parse(string str, bool condition)
        {
            try
            {
                return int.Parse(str);
            }
            catch
            {
                if (condition)
                {
                    throw;
                }
                else
                {
                    throw new System.Exception();
                }
            }
        }

        public async Task<int> ParseAsync(string str, bool condition)
        {
            try
            {
                return int.Parse(str);
            }
            catch
            {
                await Task.Delay(0);

                if (condition)
                {
                    throw;
                }
                else
                {
                    throw new System.Exception();
                }
            }
        }

        public void Test(bool condition)
        {
            Parse(nameof(Test).Length.ToString(), condition);
        }

        public void Test_Catch(bool condition)
        {
            try
            {
                Parse(nameof(Test), condition);
            }
            catch { }
        }

        public async Task TestAsync(bool condition)
        {
            await ParseAsync(nameof(Test).Length.ToString(), condition);
        }

        public async Task TestAsync_Catch(bool condition)
        {
            try
            {
                await ParseAsync(nameof(Test), condition);
            }
            catch { }
        }
    }
}
