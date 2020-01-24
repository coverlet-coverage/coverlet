// Remember to use full name because adding new using directives change line numbers

using System.Threading.Tasks;

namespace Coverlet.Core.Samples.Tests
{
    public class Lambda_Issue343
    {
        protected T WriteToStream<T>(System.Func<System.IO.Stream, bool, T> getResultFunction)
        {
            using (var stream = new System.IO.MemoryStream())
            {
                var result = getResultFunction(stream, false);
                return result;
            }
        }

        public bool InvokeAnonymous()
        {
            return WriteToStream((stream, condition) =>
            {
                if (condition)
                    stream.WriteByte(1);
                else
                    stream.WriteByte(0);
                return condition;
            }
            );
        }

        public bool InvokeAnonymous_Test()
        {
            Lambda_Issue343 demoClass = new Lambda_Issue343();
            return demoClass.InvokeAnonymous();
        }

        protected async Task<T> WriteToStreamAsync<T>(System.Func<System.IO.Stream, bool, Task<T>> getResultFunction)
        {
            using (var stream = new System.IO.MemoryStream())
            {
                var result = await getResultFunction(stream, false);
                return result;
            }
        }

        async public Task<bool> InvokeAnonymousAsync()
        {
            return await WriteToStreamAsync(async (stream, condition) =>
            {
                if (condition)
                    stream.WriteByte(1);
                else
                    stream.WriteByte(0);
                return await Task.FromResult(condition);
            });
        }

        async public Task<bool> InvokeAnonymousAsync_Test()
        {
            Lambda_Issue343 demoClass = new Lambda_Issue343();
            return await demoClass.InvokeAnonymousAsync();
        }

        public bool InvokeAnonymous_WithBranch()
        {
            if (WriteToStream((stream, condition) =>
            {
                if (condition)
                    stream.WriteByte(1);
                else
                    stream.WriteByte(0);
                return condition;
            }))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool InvokeAnonymous_NoLamda()
        {
            return WriteToStream_NoLambda(Lambda_Issue343_Nested.GetCondition());
        }

        public bool InvokeAnonymous_NoLambda_WithBranch()
        {
            if (WriteToStream_NoLambda(Lambda_Issue343_Nested.GetCondition()))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        protected bool WriteToStream_NoLambda(bool condition)
        {
            using (var stream = new System.IO.MemoryStream())
            {
                var result = GetResult(stream, condition);
                return result;
            }
        }

        public bool GetResult(System.IO.Stream stream, bool condition)
        {
            if (condition)
                stream.WriteByte(1);
            else
                stream.WriteByte(0);
            return condition;
        }

        public bool InvokeAnonymous_MoreTests()
        {
            Lambda_Issue343 demoClass = new Lambda_Issue343();
            return demoClass.InvokeAnonymous_WithBranch() &
                   demoClass.InvokeAnonymous_NoLamda() &
                   demoClass.InvokeAnonymous_NoLambda_WithBranch();
        }

        public static class Lambda_Issue343_Nested
        {
            public static bool GetCondition()
            {
                return false;
            }
        }
    }
}
