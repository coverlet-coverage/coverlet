// This code is an adapted porting of CoreFx RemoteExecutor 
// https://github.com/dotnet/arcade/tree/master/src/Microsoft.DotNet.RemoteExecutor/src

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Sdk;

namespace Coverlet.Tests.RemoteExecutor
{
    public static partial class RemoteExecutor
    {
        // If you want to debug instrumentation you could
        // 1) Add a Debug.Launch() inside lambda and attach(slow)
        // 2) Temporary pass true to invoke local, it will throw because code try to replace locked files,
        //    but if you temporary "comment" offensive code(RestoreOriginalModule/s) you can debug all procedure and it's very very very useful
        public static IRemoteInvokeHandle Invoke(Func<string, Task<int>> method, string arg = "", bool invokeInProcess = false)
        {
            if (invokeInProcess)
            {
                return new LocalInvoker(method.Invoke(arg).GetAwaiter().GetResult());
            }
            else
            {
                return Invoke(GetMethodInfo(method), new[] { arg });
            }
        }

        private static IRemoteInvokeHandle Invoke(MethodInfo method, string[] args, bool pasteArguments = true)
        {
            Type t = method.DeclaringType;
            Assembly a = t.GetTypeInfo().Assembly;
            string exceptionFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            string resultFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            string metadataArgs = PasteArguments.Paste(new string[] { a.Location, t.FullName, method.Name, exceptionFile }, pasteFirstArgumentUsingArgV0Rules: false);
            string hostRunner = Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName);
            string passedArgs = pasteArguments ? PasteArguments.Paste(args, pasteFirstArgumentUsingArgV0Rules: false) : string.Join(" ", args);
            string testConsoleAppArgs = Assembly.GetExecutingAssembly().Location + " " + metadataArgs + " " + passedArgs;

            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = hostRunner;
            psi.Arguments = testConsoleAppArgs;
            psi.UseShellExecute = false;
            return new RemoteInvokeHandle(Process.Start(psi), exceptionFile, resultFile);
        }

        private static MethodInfo GetMethodInfo(Delegate d)
        {
            // RemoteInvoke doesn't support marshaling state on classes associated with
            // the delegate supplied (often a display class of a lambda).  If such fields
            // are used, odd errors result, e.g. NullReferenceExceptions during the remote
            // execution.  Try to ward off the common cases by proactively failing early
            // if it looks like such fields are needed.
            if (d.Target != null)
            {
                // The only fields on the type should be compiler-defined (any fields of the compiler's own
                // making generally include '<' and '>', as those are invalid in C# source).  Note that this logic
                // may need to be revised in the future as the compiler changes, as this relies on the specifics of
                // actually how the compiler handles lifted fields for lambdas.
                Type targetType = d.Target.GetType();
                Assert.All(
                    targetType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                    fi => Assert.True(fi.Name.IndexOf('<') != -1, $"Field marshaling is not supported by {nameof(Invoke)}: {fi.Name}"));
            }

            return d.GetMethodInfo();
        }
    }

    public interface IRemoteInvokeHandle : IDisposable
    {
        int ExitCode { get; }
    }

    public struct LocalInvoker : IRemoteInvokeHandle
    {
        public readonly int ExitCode { get; }

        public LocalInvoker(int exitCode) => ExitCode = exitCode;

        public void Dispose()
        {
            if (ExitCode != 0)
            {
                throw new RemoteExecutionException($"Result '{ExitCode}'");
            }
        }
    }

    public sealed class RemoteInvokeHandle : IRemoteInvokeHandle
    {
        const int FailWaitTimeoutMilliseconds = 60 * 1000;
        private readonly Process _process;
        private readonly string _exceptionFile;
        private readonly string _resultFile;

        public RemoteInvokeHandle(Process process, string exceptionFile, string resultFile) => (_process, _exceptionFile, _resultFile) = (process, exceptionFile, resultFile);

        public int ExitCode
        {
            get
            {
                _process.WaitForExit(FailWaitTimeoutMilliseconds);
                return _process.ExitCode;
            }
        }

        public void Dispose()
        {
            Assert.True(_process.WaitForExit(FailWaitTimeoutMilliseconds), $"Timed out after {FailWaitTimeoutMilliseconds}ms waiting for remote process {_process.Id}");

            FileInfo exceptionFileInfo = new FileInfo(_exceptionFile);
            if (exceptionFileInfo.Exists && exceptionFileInfo.Length != 0)
            {
                string exception = File.ReadAllText(_exceptionFile);
                File.Delete(_exceptionFile);
                throw new RemoteExecutionException(exception);
            }

            Assert.True(0 == _process.ExitCode, $"Exit code {_process.ExitCode}");
        }
    }

    public sealed class RemoteExecutionException : XunitException
    {
        public RemoteExecutionException(string stackTrace)
            : base("Remote process failed with an unhandled exception.", stackTrace)
        {
        }
    }

    internal static class PasteArguments
    {
        private const char Quote = '\"';
        private const char Backslash = '\\';

        /// <summary>
        /// Repastes a set of arguments into a linear string that parses back into the originals under pre- or post-2008 VC parsing rules.
        /// The rules for parsing the executable name (argv[0]) are special, so you must indicate whether the first argument actually is argv[0].
        /// </summary>
        public static string Paste(IEnumerable<string> arguments, bool pasteFirstArgumentUsingArgV0Rules)
        {
            var stringBuilder = new StringBuilder();

            foreach (string argument in arguments)
            {
                if (pasteFirstArgumentUsingArgV0Rules && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    pasteFirstArgumentUsingArgV0Rules = false;

                    // Special rules for argv[0]
                    //   - Backslash is a normal character.
                    //   - Quotes used to include whitespace characters.
                    //   - Parsing ends at first whitespace outside quoted region.
                    //   - No way to get a literal quote past the parser.

                    bool hasWhitespace = false;
                    foreach (char c in argument)
                    {
                        if (c == Quote)
                        {
                            throw new ApplicationException("The argv[0] argument cannot include a double quote.");
                        }
                        if (char.IsWhiteSpace(c))
                        {
                            hasWhitespace = true;
                        }
                    }
                    if (argument.Length == 0 || hasWhitespace)
                    {
                        stringBuilder.Append(Quote);
                        stringBuilder.Append(argument);
                        stringBuilder.Append(Quote);
                    }
                    else
                    {
                        stringBuilder.Append(argument);
                    }
                }
                else
                {
                    AppendArgument(stringBuilder, argument);
                }
            }

            return stringBuilder.ToString();
        }

        public static void AppendArgument(StringBuilder stringBuilder, string argument)
        {
            if (stringBuilder.Length != 0)
            {
                stringBuilder.Append(' ');
            }

            // Parsing rules for non-argv[0] arguments:
            //   - Backslash is a normal character except followed by a quote.
            //   - 2N backslashes followed by a quote ==> N literal backslashes followed by unescaped quote
            //   - 2N+1 backslashes followed by a quote ==> N literal backslashes followed by a literal quote
            //   - Parsing stops at first whitespace outside of quoted region.
            //   - (post 2008 rule): A closing quote followed by another quote ==> literal quote, and parsing remains in quoting mode.
            if (argument.Length != 0 && ContainsNoWhitespaceOrQuotes(argument))
            {
                // Simple case - no quoting or changes needed.
                stringBuilder.Append(argument);
            }
            else
            {
                stringBuilder.Append(Quote);
                int idx = 0;
                while (idx < argument.Length)
                {
                    char c = argument[idx++];
                    if (c == Backslash)
                    {
                        int numBackSlash = 1;
                        while (idx < argument.Length && argument[idx] == Backslash)
                        {
                            idx++;
                            numBackSlash++;
                        }

                        if (idx == argument.Length)
                        {
                            // We'll emit an end quote after this so must double the number of backslashes.
                            stringBuilder.Append(Backslash, numBackSlash * 2);
                        }
                        else if (argument[idx] == Quote)
                        {
                            // Backslashes will be followed by a quote. Must double the number of backslashes.
                            stringBuilder.Append(Backslash, numBackSlash * 2 + 1);
                            stringBuilder.Append(Quote);
                            idx++;
                        }
                        else
                        {
                            // Backslash will not be followed by a quote, so emit as normal characters.
                            stringBuilder.Append(Backslash, numBackSlash);
                        }

                        continue;
                    }

                    if (c == Quote)
                    {
                        // Escape the quote so it appears as a literal. This also guarantees that we won't end up generating a closing quote followed
                        // by another quote (which parses differently pre-2008 vs. post-2008.)
                        stringBuilder.Append(Backslash);
                        stringBuilder.Append(Quote);
                        continue;
                    }

                    stringBuilder.Append(c);
                }

                stringBuilder.Append(Quote);
            }
        }

        private static bool ContainsNoWhitespaceOrQuotes(string s)
        {
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (char.IsWhiteSpace(c) || c == Quote)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
