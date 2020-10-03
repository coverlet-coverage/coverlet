using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Coverlet.Core.Attributes;
using Microsoft.VisualStudio.TestPlatform;

namespace Coverlet.Core.Samples.Tests
{
    class ConstructorNotDeclaredClass
    {
    }
    class DeclaredConstructorClass
    {
        DeclaredConstructorClass() { }

        public bool HasSingleDecision(string input)
        {
            if (input.Contains("test")) return true;
            return false;
        }

        public bool HasTwoDecisions(string input)
        {
            if (input.Contains("test")) return true;
            if (input.Contains("xxx")) return true;
            return false;
        }

        public bool HasCompleteIf(string input)
        {
            if (input.Contains("test"))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool HasSwitch(int input)
        {
            switch (input)
            {
                case 0:
                    return true;
                case 1:
                    return false;
                case 2:
                    return true;
            }
            return false;
        }

        public bool HasSwitchWithDefault(int input)
        {
            switch (input)
            {
                case 1:
                    return true;
                case 2:
                    return false;
                case 3:
                    return true;
                default:
                    return false;
            }
        }

        public bool HasSwitchWithBreaks(int input)
        {
            bool ret = false;
            switch (input)
            {
                case 1:
                    ret = true;
                    break;
                case 2:
                    ret = false;
                    break;
                case 3:
                    ret = true;
                    break;
            }

            return ret;
        }

        public int HasSwitchWithMultipleCases(int input)
        {
            switch (input)
            {
                case 1:
                    return -1;
                case 2:
                    return 2001;
                case 3:
                    return -5001;
                default:
                    return 7;
            }
        }

        public string HasSimpleUsingStatement()
        {
            string value;
            try
            {

            }
            finally
            {
                using (var stream = new MemoryStream())
                {
                    var x = stream.Length;
                    value = x > 1000 ? "yes" : "no";
                }
            }
            return value;
        }

        /// <summary>
        /// This method is used by a unit test that verifies the behavior of the instrumentation process on an assembly
        /// which is not instrumented. Excluding this method from code coverage prevents the bytecode for this reference
        /// method from getting modified prior to test execution so it retains its original form for the test. This is
        /// not a problem for the test because the instrumentation process only runs on assemblies which have not
        /// already been instrumented.
        /// </summary>
        [ExcludeFromCodeCoverage]
        public void HasSimpleTaskWithLambda()
        {
            var t = new Task(() => { });
        }

        public string UsingWithException_Issue243()
        {
            using (var ms = new MemoryStream()) // IL generates a finally block for using to dispose the stream
            {
                throw new Exception();
            }
        }
    }

    public class LinqIssue
    {
        public void Method()
        {
            var s = new ObservableCollection<string>();
            var x = (from a in s select new { a });
        }

        public object Property
        {
            get
            {
                var s = new ObservableCollection<string>();
                var x = (from a in s select new { a });
                return x;
            }
        }
    }

    public class Iterator
    {
        public IEnumerable<string> Fetch()
        {
            yield return "one";
            yield return "two";
        }
    }

    public class SingletonIterator
    {
        public IEnumerable<string> Fetch()
        {
            yield return "one";
        }
    }

    public class AsyncAwaitStateMachine
    {
        async public Task AsyncAwait()
        {
            await Task.CompletedTask;
        }
    }

    public class AsyncAwaitValueTaskStateMachine
    {
        async public ValueTask AsyncAwait()
        {
            await default(ValueTask);
        }
    }

    [ExcludeFromCoverage]
    public class ClassExcludedByCoverletCodeCoverageAttr
    {

        public string Method(string input)
        {
            if (string.IsNullOrEmpty(input))
                throw new ArgumentException("Cannot be empty", nameof(input));

            return input;
        }
    }

    [ExcludeFromCodeCoverage]
    public class ClassExcludedByCodeAnalysisCodeCoverageAttr
    {
        public string Method(string input)
        {
            if (string.IsNullOrEmpty(input))
                throw new ArgumentException("Cannot be empty", nameof(input));

            return input;
        }
    }

    [TestSDKAutoGeneratedCode]
    public class ClassExcludedByAttrWithoutAttributeNameSuffix
    {
        public string Method(string input)
        {
            if (string.IsNullOrEmpty(input))
                throw new ArgumentException("Cannot be empty", nameof(input));

            return input;
        }
    }

    [Obsolete]
    public class ClassExcludedByObsoleteAttr
    {
        public string Method(string input)
        {
            if (string.IsNullOrEmpty(input))
                throw new ArgumentException("Cannot be empty", nameof(input));

            return input;
        }
    }

    public class ClassWithMethodExcludedByObsoleteAttr
    {
        [Obsolete]
        public string Method(string input)
        {
            if (string.IsNullOrEmpty(input))
                throw new ArgumentException("Cannot be empty", nameof(input));

            return input;
        }
    }

    public class ClassWithPropertyExcludedByObsoleteAttr
    {
        [Obsolete]
        public string Property { get; set; }
    }

    public class ClassWithSetterOnlyPropertyExcludedByObsoleteAttr
    {
        [Obsolete]
        public string Property
        {
            set => _ = string.Empty;
        }
    }

    public abstract class ClassWithEmptyMethod
    {
        public abstract void EmptyMethod();
    }

    public class ExceptionFilter
    {
        public void Test()
        {
            try
            {
                int a = 0;
                int b = 1;
                int c = b / a;
            }
            catch (Exception ex) when (True() && False())
            {
                Console.WriteLine(ex.Message);
            }
        }

        public bool True()
        {
            return true;
        }

        public bool False()
        {
            return false;
        }
    }
}