// This code is an adapted porting of CoreFx RemoteExecutor 
// https://github.com/dotnet/arcade/tree/master/src/Microsoft.DotNet.RemoteExecutor/src

using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.ExceptionServices;
using System.Runtime.Loader;

namespace Coverlet.Tests.RemoteExecutor
{
    /// <summary>
    /// Provides an entry point in a new process that will load a specified method and invoke it.
    /// </summary>
    internal static class Program
    {
        private static int Main(string[] args)
        {
            string assemlbyFilePath = args[0];
            string typeName = args[1];
            string methodName = args[2];
            string exceptionFile = args[3];

            string[] additionalArgs = args.Length > 4 ?
                args.Subarray(4, args.Length - 4) :
                Array.Empty<string>();

            Assembly a = null;
            Type t = null;
            MethodInfo mi = null;
            object instance = null;
            int exitCode = 0;
            try
            {
                // Create the test class if necessary
                a = AssemblyLoadContext.Default.LoadFromAssemblyPath(assemlbyFilePath);
                t = a.GetType(typeName);
                mi = t.GetTypeInfo().GetDeclaredMethod(methodName);
                if (!mi.IsStatic)
                {
                    instance = Activator.CreateInstance(t);
                }

                // Invoke the test
                object result = mi.Invoke(instance, additionalArgs);

                if (result is Task<int> task)
                {
                    exitCode = task.GetAwaiter().GetResult();
                }
                else if (result is int exit)
                {
                    exitCode = exit;
                }
            }
            catch (Exception exc)
            {
                if (exc is TargetInvocationException && exc.InnerException != null)
                    exc = exc.InnerException;

                var output = new StringBuilder();
                output.AppendLine();
                output.AppendLine("Child exception:");
                output.AppendLine("  " + exc);
                output.AppendLine();
                output.AppendLine("Child process:");
                output.AppendLine(string.Format("  {0} {1} {2}", a, t, mi));
                output.AppendLine();

                if (additionalArgs.Length > 0)
                {
                    output.AppendLine("Child arguments:");
                    output.AppendLine("  " + string.Join(", ", additionalArgs));
                }

                File.WriteAllText(exceptionFile, output.ToString());

                ExceptionDispatchInfo.Capture(exc).Throw();
            }
            finally
            {
                (instance as IDisposable)?.Dispose();
            }

            return exitCode;
        }

        private static T[] Subarray<T>(this T[] arr, int offset, int count)
        {
            var newArr = new T[count];
            Array.Copy(arr, offset, newArr, 0, count);
            return newArr;
        }
    }
}
