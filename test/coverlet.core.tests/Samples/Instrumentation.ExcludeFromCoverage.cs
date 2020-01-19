// Remember to use full name because adding new using directives change line numbers

namespace Coverlet.Core.Samples.Tests
{
    public class MethodsWithExcludeFromCodeCoverageAttr
    {
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        public int TestLambda(string input)
        {
            System.Func<string, int> lambdaFunc = s => s.Length;
            return lambdaFunc(input);
        }

        public int TestLambda(string input, int value)
        {
            System.Func<string, int> lambdaFunc = s => s.Length;
            return lambdaFunc(input) + value;
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        public System.Collections.Generic.IEnumerable<int> TestYield(string input)
        {
            foreach (char c in input)
            {
                yield return c;
            }
        }

        public System.Collections.Generic.IEnumerable<int> TestYield(string input, int value)
        {
            foreach (char c in input)
            {
                yield return c + value;
            }
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        public async System.Threading.Tasks.Task TestAsyncAwait()
        {
            await System.Threading.Tasks.Task.Delay(50);
        }

        public async System.Threading.Tasks.Task TestAsyncAwait(int value)
        {
            await System.Threading.Tasks.Task.Delay(System.Math.Min(value, 50)); // Avoid infinite delay
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        public int TestLocalFunction(string input)
        {
            return LocalFunction(input);

            static int LocalFunction(string input)
            {
                return input.Length;
            }
        }

        public int TestLocalFunction(string input, int value)
        {
            return LocalFunction(input) + value;

            static int LocalFunction(string input)
            {
                return input.Length;
            }
        }

        public async System.Threading.Tasks.Task<int> Test(string input)
        {
            await TestAsyncAwait(1);
            return TestLambda(input, 1) + System.Linq.Enumerable.Sum(TestYield(input, 1)) + TestLocalFunction(input, 1);
        }
    }

    public class MethodsWithExcludeFromCodeCoverageAttr2
    {
        public int TestLambda(string input)
        {
            System.Func<string, int> lambdaFunc = s => s.Length;
            return lambdaFunc(input);
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        public int TestLambda(string input, int value)
        {
            System.Func<string, int> lambdaFunc = s => s.Length;
            return lambdaFunc(input) + value;
        }

        public System.Collections.Generic.IEnumerable<int> TestYield(string input)
        {
            foreach (char c in input)
            {
                yield return c;
            }
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        public System.Collections.Generic.IEnumerable<int> TestYield(string input, int value)
        {
            foreach (char c in input)
            {
                yield return c + value;
            }
        }

        public async System.Threading.Tasks.Task TestAsyncAwait()
        {
            await System.Threading.Tasks.Task.Delay(50);
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        public async System.Threading.Tasks.Task TestAsyncAwait(int value)
        {
            await System.Threading.Tasks.Task.Delay(50);
        }

        public int TestLocalFunction(string input)
        {
            return LocalFunction(input);

            static int LocalFunction(string input)
            {
                return input.Length;
            }
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        public int TestLocalFunction(string input, int value)
        {
            return LocalFunction(input) + value;

            static int LocalFunction(string input)
            {
                return input.Length;
            }
        }
    }

    public class ExcludeFromCoverageAttrFilterClass1
    {
        public int Run() => 10 + new ExcludeFromCoverageAttrFilterClass2().Run();

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        public class ExcludeFromCoverageAttrFilterClass2
        {
            public int Run() => 10 + new ExcludeFromCoverageAttrFilterClass3().Run();

            public class ExcludeFromCoverageAttrFilterClass3
            {
                public int Run() => 10 + new ExcludeFromCoverageAttrFilterClass4().Run();

                public class ExcludeFromCoverageAttrFilterClass4
                {
                    public int Run() => 12;
                }
            }
        }
    }
}