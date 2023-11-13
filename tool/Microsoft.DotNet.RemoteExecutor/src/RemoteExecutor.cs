// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using IOPath = System.IO.Path;

namespace Microsoft.DotNet.RemoteExecutor
{
    public static partial class RemoteExecutor
    {
        /// <summary>
        /// A timeout (milliseconds) after which a wait on a remote operation should be considered a failure.
        /// </summary>
        public const int FailWaitTimeoutMilliseconds = 60 * 1000;

        /// <summary>
        /// The exit code returned when the test process exits successfully.
        /// </summary>
        public const int SuccessExitCode = 42;

        /// <summary>
        /// The path of the remote executor.
        /// </summary>
        public static readonly string Path;

        /// <summary>
        /// The name of the host.
        /// </summary>
        public static string HostRunnerName;

        /// <summary>
        /// The path of the host.
        /// </summary>
        public static readonly string HostRunner;

        private static string s_runtimeConfigPath;
        private static string s_depsJsonPath;

        static RemoteExecutor()
        {
            if (!IsSupported)
            {
                return;
            }

            string processFileName = Process.GetCurrentProcess().MainModule?.FileName;
            if (processFileName == null)
            {
                return;
            }

            Path = typeof(RemoteExecutor).Assembly.Location;

            if (IsNetCore())
            {
                HostRunner = processFileName;

                string hostName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "dotnet.exe" : "dotnet";

                // Partially addressing https://github.com/dotnet/arcade/issues/6371
                // We expect to run tests with dotnet. However in certain scenarios we may have a different apphost (e.g. Visual Studio testhost).
                // Attempt to find and use dotnet.
                if (!IOPath.GetFileName(HostRunner).Equals(hostName, StringComparison.OrdinalIgnoreCase))
                {
                    string runtimePath = IOPath.GetDirectoryName(typeof(object).Assembly.Location);

                    // In case we are running the app via a runtime, dotnet.exe is located 3 folders above the runtime. Example:
                    // runtime    ->  C:\Program Files\dotnet\shared\Microsoft.NETCore.App\5.0.6\
                    // dotnet.exe ->  C:\Program Files\dotnet\shared\dotnet.exe
                    // This should also work on Unix and locally built runtime/testhost.
                    string directory = GetDirectoryName(GetDirectoryName(GetDirectoryName(runtimePath)));
                    if (directory != string.Empty)
                    {
                        string dotnetExe = IOPath.Combine(directory, hostName);
                        if (File.Exists(dotnetExe))
                        {
                            HostRunner = dotnetExe;
                        }
                    }
                }
            }
            else if (RuntimeInformation.FrameworkDescription.StartsWith(".NET Framework", StringComparison.OrdinalIgnoreCase))
            {
                HostRunner = Path;
            }

            HostRunnerName = IOPath.GetFileName(HostRunner);

            static string GetDirectoryName(string path) => string.IsNullOrEmpty(path) ? string.Empty : IOPath.GetDirectoryName(path);
        }

        private static bool IsNetCore() =>
            Environment.Version.Major >= 5 || RuntimeInformation.FrameworkDescription.StartsWith(".NET Core", StringComparison.OrdinalIgnoreCase);

        /// <summary>Returns true if the RemoteExecutor works on the current platform, otherwise false.</summary>
        public static bool IsSupported { get; } =
            !RuntimeInformation.IsOSPlatform(OSPlatform.Create("IOS")) &&
            !RuntimeInformation.IsOSPlatform(OSPlatform.Create("ANDROID")) &&
            !RuntimeInformation.IsOSPlatform(OSPlatform.Create("TVOS")) &&
            !RuntimeInformation.IsOSPlatform(OSPlatform.Create("MACCATALYST")) &&
            !RuntimeInformation.IsOSPlatform(OSPlatform.Create("WATCHOS")) &&
            !RuntimeInformation.IsOSPlatform(OSPlatform.Create("BROWSER")) &&
            !RuntimeInformation.IsOSPlatform(OSPlatform.Create("WASI")) &&
            Environment.GetEnvironmentVariable("DOTNET_REMOTEEXECUTOR_SUPPORTED") != "0";

        /// <summary>Invokes the method from this assembly in another process using the specified arguments.</summary>
        /// <param name="method">The method to invoke.</param>
        /// <param name="options">Options to use for the invocation.</param>
        public static RemoteInvokeHandle Invoke(Action method, RemoteInvokeOptions options = null)
        {
            return Invoke(GetMethodInfo(method), Array.Empty<string>(), options);
        }


        /// <summary>Invokes the method from this assembly in another process using the specified arguments.</summary>
        /// <param name="method">The method to invoke.</param>
        /// <param name="arg">The argument to pass to the method.</param>
        /// <param name="options">Options to use for the invocation.</param>
        public static RemoteInvokeHandle Invoke(Action<string> method, string arg, RemoteInvokeOptions options = null)
        {
            return Invoke(GetMethodInfo(method), new[] { arg }, options);
        }

        /// <summary>Invokes the method from this assembly in another process using the specified arguments.</summary>
        /// <param name="method">The method to invoke.</param>
        /// <param name="arg1">The first argument to pass to the method.</param>
        /// <param name="arg2">The second argument to pass to the method.</param>
        /// <param name="options">Options to use for the invocation.</param>
        public static RemoteInvokeHandle Invoke(Action<string, string> method, string arg1, string arg2,
            RemoteInvokeOptions options = null)
        {
            return Invoke(GetMethodInfo(method), new[] { arg1, arg2 }, options);
        }

        /// <summary>Invokes the method from this assembly in another process using the specified arguments.</summary>
        /// <param name="method">The method to invoke.</param>
        /// <param name="arg1">The first argument to pass to the method.</param>
        /// <param name="arg2">The second argument to pass to the method.</param>
        /// <param name="arg3">The third argument to pass to the method.</param>
        /// <param name="options">Options to use for the invocation.</param>
        public static RemoteInvokeHandle Invoke(Action<string, string, string> method, string arg1, string arg2,
            string arg3, RemoteInvokeOptions options = null)
        {
            return Invoke(GetMethodInfo(method), new[] { arg1, arg2, arg3 }, options);
        }

        /// <summary>Invokes the method from this assembly in another process using the specified arguments.</summary>
        /// <param name="method">The method to invoke.</param>
        /// <param name="arg1">The first argument to pass to the method.</param>
        /// <param name="arg2">The second argument to pass to the method.</param>
        /// <param name="arg3">The third argument to pass to the method.</param>
        /// <param name="arg4">The fourth argument to pass to the method.</param>
        /// <param name="options">Options to use for the invocation.</param>
        public static RemoteInvokeHandle Invoke(Action<string, string, string, string> method, string arg1,
            string arg2, string arg3, string arg4, RemoteInvokeOptions options = null)
        {
            return Invoke(GetMethodInfo(method), new[] { arg1, arg2, arg3, arg4 }, options);
        }

        /// <summary>Invokes the method from this assembly in another process using the specified arguments.</summary>
        /// <param name="method">The method to invoke.</param>
        /// <param name="arg1">The first argument to pass to the method.</param>
        /// <param name="arg2">The second argument to pass to the method.</param>
        /// <param name="arg3">The third argument to pass to the method.</param>
        /// <param name="arg4">The fourth argument to pass to the method.</param>
        /// <param name="arg5">The fifth argument to pass to the method.</param>
        /// <param name="options">Options to use for the invocation.</param>
        public static RemoteInvokeHandle Invoke(Action<string, string, string, string, string> method, string arg1,
            string arg2, string arg3, string arg4, string arg5, RemoteInvokeOptions options = null)
        {
            return Invoke(GetMethodInfo(method), new[] { arg1, arg2, arg3, arg4, arg5 }, options);
        }

        /// <summary>Invokes the method from this assembly in another process using the specified arguments.</summary>
        /// <param name="method">The method to invoke.</param>
        /// <param name="options">Options to use for the invocation.</param>
        public static RemoteInvokeHandle Invoke(Func<Task<int>> method, RemoteInvokeOptions options = null)
        {
            return Invoke(GetMethodInfo(method), Array.Empty<string>(), options);
        }

        /// <summary>Invokes the method from this assembly in another process using the specified arguments.</summary>
        /// <param name="method">The method to invoke.</param>
        /// <param name="arg">The argument to pass to the method.</param>
        /// <param name="options">Options to use for the invocation.</param>
        public static RemoteInvokeHandle Invoke(Func<string, Task<int>> method, string arg,
            RemoteInvokeOptions options = null)
        {
            return Invoke(GetMethodInfo(method), new[] { arg }, options);
        }

        /// <summary>Invokes the method from this assembly in another process using the specified arguments.</summary>
        /// <param name="method">The method to invoke.</param>
        /// <param name="arg1">The first argument to pass to the method.</param>
        /// <param name="arg2">The second argument to pass to the method.</param>
        /// <param name="options">Options to use for the invocation.</param>
        public static RemoteInvokeHandle Invoke(Func<string, string, Task<int>> method, string arg1, string arg2,
            RemoteInvokeOptions options = null)
        {
            return Invoke(GetMethodInfo(method), new[] { arg1, arg2 }, options);
        }

        /// <summary>Invokes the method from this assembly in another process using the specified arguments.</summary>
        /// <param name="method">The method to invoke.</param>
        /// <param name="arg1">The first argument to pass to the method.</param>
        /// <param name="arg2">The second argument to pass to the method.</param>
        /// <param name="arg3">The third argument to pass to the method.</param>
        /// <param name="options">Options to use for the invocation.</param>
        public static RemoteInvokeHandle Invoke(Func<string, string, string, Task<int>> method, string arg1,
            string arg2, string arg3, RemoteInvokeOptions options = null)
        {
            return Invoke(GetMethodInfo(method), new[] { arg1, arg2, arg3 }, options);
        }

        /// <summary>Invokes the method from this assembly in another process using the specified arguments.</summary>
        /// <param name="method">The method to invoke.</param>
        /// <param name="arg1">The first argument to pass to the method.</param>
        /// <param name="arg2">The second argument to pass to the method.</param>
        /// <param name="arg3">The third argument to pass to the method.</param>
        /// <param name="arg4">The fourth argument to pass to the method.</param>
        /// <param name="options">Options to use for the invocation.</param>
        public static RemoteInvokeHandle Invoke(Func<string, string, string, string, Task<int>> method, string arg1,
            string arg2, string arg3, string arg4, RemoteInvokeOptions options = null)
        {
            return Invoke(GetMethodInfo(method), new[] { arg1, arg2, arg3, arg4 }, options);
        }

        /// <summary>Invokes the method from this assembly in another process using the specified arguments.</summary>
        /// <param name="method">The method to invoke.</param>
        /// <param name="arg1">The first argument to pass to the method.</param>
        /// <param name="arg2">The second argument to pass to the method.</param>
        /// <param name="arg3">The third argument to pass to the method.</param>
        /// <param name="arg4">The fourth argument to pass to the method.</param>
        /// <param name="arg5">The fifth argument to pass to the method.</param>
        /// <param name="options">Options to use for the invocation.</param>
        public static RemoteInvokeHandle Invoke(Func<string, string, string, string, string, Task<int>> method, string arg1,
            string arg2, string arg3, string arg4, string arg5, RemoteInvokeOptions options = null)
        {
            return Invoke(GetMethodInfo(method), new[] { arg1, arg2, arg3, arg4, arg5 }, options);
        }

        /// <summary>Invokes the method from this assembly in another process using the specified arguments.</summary>
        /// <param name="method">The method to invoke.</param>
        /// <param name="options">Options to use for the invocation.</param>
        public static RemoteInvokeHandle Invoke(Func<Task> method, RemoteInvokeOptions options = null)
        {
            return Invoke(GetMethodInfo(method), Array.Empty<string>(), options);
        }

        /// <summary>Invokes the method from this assembly in another process using the specified arguments.</summary>
        /// <param name="method">The method to invoke.</param>
        /// <param name="arg">The argument to pass to the method.</param>
        /// <param name="options">Options to use for the invocation.</param>
        public static RemoteInvokeHandle Invoke(Func<string, Task> method, string arg,
            RemoteInvokeOptions options = null)
        {
            return Invoke(GetMethodInfo(method), new[] { arg }, options);
        }

        /// <summary>Invokes the method from this assembly in another process using the specified arguments.</summary>
        /// <param name="method">The method to invoke.</param>
        /// <param name="arg1">The first argument to pass to the method.</param>
        /// <param name="arg2">The second argument to pass to the method.</param>
        /// <param name="options">Options to use for the invocation.</param>
        public static RemoteInvokeHandle Invoke(Func<string, string, Task> method, string arg1, string arg2,
            RemoteInvokeOptions options = null)
        {
            return Invoke(GetMethodInfo(method), new[] { arg1, arg2 }, options);
        }

        /// <summary>Invokes the method from this assembly in another process using the specified arguments.</summary>
        /// <param name="method">The method to invoke.</param>
        /// <param name="arg1">The first argument to pass to the method.</param>
        /// <param name="arg2">The second argument to pass to the method.</param>
        /// <param name="arg3">The third argument to pass to the method.</param>
        /// <param name="options">Options to use for the invocation.</param>
        public static RemoteInvokeHandle Invoke(Func<string, string, string, Task> method, string arg1,
            string arg2, string arg3, RemoteInvokeOptions options = null)
        {
            return Invoke(GetMethodInfo(method), new[] { arg1, arg2, arg3 }, options);
        }

        /// <summary>Invokes the method from this assembly in another process using the specified arguments.</summary>
        /// <param name="method">The method to invoke.</param>
        /// <param name="arg1">The first argument to pass to the method.</param>
        /// <param name="arg2">The second argument to pass to the method.</param>
        /// <param name="arg3">The third argument to pass to the method.</param>
        /// <param name="arg4">The fourth argument to pass to the method.</param>
        /// <param name="options">Options to use for the invocation.</param>
        public static RemoteInvokeHandle Invoke(Func<string, string, string, string, Task> method, string arg1,
            string arg2, string arg3, string arg4, RemoteInvokeOptions options = null)
        {
            return Invoke(GetMethodInfo(method), new[] { arg1, arg2, arg3, arg4 }, options);
        }

        /// <summary>Invokes the method from this assembly in another process using the specified arguments.</summary>
        /// <param name="method">The method to invoke.</param>
        /// <param name="arg1">The first argument to pass to the method.</param>
        /// <param name="arg2">The second argument to pass to the method.</param>
        /// <param name="arg3">The third argument to pass to the method.</param>
        /// <param name="arg4">The fourth argument to pass to the method.</param>
        /// <param name="arg5">The fifth argument to pass to the method.</param>
        /// <param name="options">Options to use for the invocation.</param>
        public static RemoteInvokeHandle Invoke(Func<string, string, string, string, string, Task> method, string arg1,
            string arg2, string arg3, string arg4, string arg5, RemoteInvokeOptions options = null)
        {
            return Invoke(GetMethodInfo(method), new[] { arg1, arg2, arg3, arg4, arg5 }, options);
        }

        /// <summary>Invokes the method from this assembly in another process using the specified arguments.</summary>
        /// <param name="method">The method to invoke.</param>
        /// <param name="options">Options to use for the invocation.</param>
        public static RemoteInvokeHandle Invoke(Func<int> method, RemoteInvokeOptions options = null)
        {
            return Invoke(GetMethodInfo(method), Array.Empty<string>(), options);
        }

        /// <summary>Invokes the method from this assembly in another process using the specified arguments.</summary>
        /// <param name="method">The method to invoke.</param>
        /// <param name="arg">The argument to pass to the method.</param>
        /// <param name="options">Options to use for the invocation.</param>
        public static RemoteInvokeHandle Invoke(Func<string, int> method, string arg,
            RemoteInvokeOptions options = null)
        {
            return Invoke(GetMethodInfo(method), new[] { arg }, options);
        }

        /// <summary>Invokes the method from this assembly in another process using the specified arguments.</summary>
        /// <param name="method">The method to invoke.</param>
        /// <param name="arg1">The first argument to pass to the method.</param>
        /// <param name="arg2">The second argument to pass to the method.</param>
        /// <param name="options">Options to use for the invocation.</param>
        public static RemoteInvokeHandle Invoke(Func<string, string, int> method, string arg1, string arg2,
            RemoteInvokeOptions options = null)
        {
            return Invoke(GetMethodInfo(method), new[] { arg1, arg2 }, options);
        }

        /// <summary>Invokes the method from this assembly in another process using the specified arguments.</summary>
        /// <param name="method">The method to invoke.</param>
        /// <param name="arg1">The first argument to pass to the method.</param>
        /// <param name="arg2">The second argument to pass to the method.</param>
        /// <param name="arg3">The third argument to pass to the method.</param>
        /// <param name="options">Options to use for the invocation.</param>
        public static RemoteInvokeHandle Invoke(Func<string, string, string, int> method, string arg1,
            string arg2, string arg3, RemoteInvokeOptions options = null)
        {
            return Invoke(GetMethodInfo(method), new[] { arg1, arg2, arg3 }, options);
        }

        /// <summary>Invokes the method from this assembly in another process using the specified arguments.</summary>
        /// <param name="method">The method to invoke.</param>
        /// <param name="arg1">The first argument to pass to the method.</param>
        /// <param name="arg2">The second argument to pass to the method.</param>
        /// <param name="arg3">The third argument to pass to the method.</param>
        /// <param name="arg4">The fourth argument to pass to the method.</param>
        /// <param name="options">Options to use for the invocation.</param>
        public static RemoteInvokeHandle Invoke(Func<string, string, string, string, int> method, string arg1,
            string arg2, string arg3, string arg4, RemoteInvokeOptions options = null)
        {
            return Invoke(GetMethodInfo(method), new[] { arg1, arg2, arg3, arg4 }, options);
        }

        /// <summary>Invokes the method from this assembly in another process using the specified arguments.</summary>
        /// <param name="method">The method to invoke.</param>
        /// <param name="arg1">The first argument to pass to the method.</param>
        /// <param name="arg2">The second argument to pass to the method.</param>
        /// <param name="arg3">The third argument to pass to the method.</param>
        /// <param name="arg4">The fourth argument to pass to the method.</param>
        /// <param name="arg5">The fifth argument to pass to the method.</param>
        /// <param name="options">Options to use for the invocation.</param>
        public static RemoteInvokeHandle Invoke(Func<string, string, string, string, string, int> method,
            string arg1, string arg2, string arg3, string arg4, string arg5, RemoteInvokeOptions options = null)
        {
            return Invoke(GetMethodInfo(method), new[] { arg1, arg2, arg3, arg4, arg5 }, options);
        }

        /// <summary>Invokes the method from this assembly in another process using the specified arguments without performing any modifications to the arguments.</summary>
        /// <param name="method">The method to invoke.</param>
        /// <param name="unparsedArg">The arguments to pass to the method.</param>
        /// <param name="options">Options to use for the invocation.</param>
        public static RemoteInvokeHandle InvokeRaw(Delegate method, string unparsedArg,
            RemoteInvokeOptions options = null)
        {
            return Invoke(GetMethodInfo(method), new[] { unparsedArg }, options, pasteArguments: false);
        }

        /// <summary>Invokes the method from this assembly in another process using the specified arguments.</summary>
        /// <param name="method">The method to invoke.</param>
        /// <param name="args">The arguments to pass to the method.</param>
        /// <param name="options">Options to use for the invocation.</param>
        /// <param name="pasteArguments">true if this function should paste the arguments (e.g. surrounding with quotes); false if that responsibility is left up to the caller.</param>
        private static RemoteInvokeHandle Invoke(MethodInfo method, string[] args,
            RemoteInvokeOptions options, bool pasteArguments = true)
        {
            options = options ?? new RemoteInvokeOptions();

            // For platforms that do not support RemoteExecutor
            if (!IsSupported)
            {
                throw new PlatformNotSupportedException("RemoteExecutor is not supported on this platform.");
            }

            // Verify the specified method returns an int (the exit code) or nothing,
            // and that if it accepts any arguments, they're all strings.
            if (method.ReturnType != typeof(void)
                && method.ReturnType != typeof(int)
                && method.ReturnType != typeof(Task)
                && method.ReturnType != typeof(Task<int>))
            {
                throw new ArgumentException($"Invalid return type: {method.ReturnType}. Expected void, int, or Task", nameof(method));
            }
            foreach (ParameterInfo param in method.GetParameters())
            {
                if (param.ParameterType != typeof(string))
                {
                    throw new ArgumentException($"Invalid parameter type: {param.ParameterType}. Expected string", nameof(method));
                }
            }

            // And make sure it's in this assembly.  This isn't critical, but it helps with deployment to know
            // that the method to invoke is available because we're already running in this assembly.
            Type t = method.DeclaringType;
            Assembly a = t.GetTypeInfo().Assembly;

            // Start the other process and return a wrapper for it to handle its lifetime and exit checking.
            ProcessStartInfo psi = options.StartInfo;
            psi.UseShellExecute = false;

            if (!options.EnableProfiling)
            {
                // Profilers / code coverage tools doing coverage of the test process set environment
                // variables to tell the targeted process what profiler to load.  We don't want the child process
                // to be profiled / have code coverage, so we remove these environment variables for that process
                // before it's started.
                psi.Environment.Remove("Cor_Profiler");
                psi.Environment.Remove("Cor_Enable_Profiling");
                psi.Environment.Remove("CoreClr_Profiler");
                psi.Environment.Remove("CoreClr_Enable_Profiling");
            }

            // If we need the host (if it exists), use it, otherwise target the console app directly.
            string metadataArgs = PasteArguments.Paste(new string[] { a.FullName, t.FullName, method.Name, options.ExceptionFile }, pasteFirstArgumentUsingArgV0Rules: false);
            string passedArgs = pasteArguments ? PasteArguments.Paste(args, pasteFirstArgumentUsingArgV0Rules: false) : string.Join(" ", args);
            string consoleAppArgs = GetConsoleAppArgs(options, out IEnumerable<IDisposable> toDispose);
            string testConsoleAppArgs = consoleAppArgs + " " + metadataArgs + " " + passedArgs;

            if (options.RunAsSudo)
            {
                psi.FileName = "sudo";
                psi.Arguments = HostRunner + " " + testConsoleAppArgs;

                // Create exception file up front so there are no permission issue when RemoteInvokeHandle tries to delete it.
                File.WriteAllText(options.ExceptionFile, "");
            }
            else
            {
                psi.FileName = HostRunner;
                psi.Arguments = testConsoleAppArgs;
            }

            // Return the handle to the process, which may or not be started
            return new RemoteInvokeHandle(options.Start ? Process.Start(psi) : new Process() { StartInfo = psi },
                options, a.FullName, t.FullName, method.Name, toDispose);
        }

        private static string GetConsoleAppArgs(RemoteInvokeOptions options, out IEnumerable<IDisposable> toDispose)
        {
            bool isNetCore = IsNetCore();
            if (options.RuntimeConfigurationOptions?.Any() == true && !isNetCore)
            {
                throw new InvalidOperationException("RuntimeConfigurationOptions are only supported on .NET Core");
            }

            if (!isNetCore)
            {
                toDispose = null;
                return string.Empty;
            }

            string args = "exec";

            string runtimeConfigPath = GetRuntimeConfigPath(options, out toDispose);
            if (runtimeConfigPath != null)
            {
                args += $" --runtimeconfig \"{runtimeConfigPath}\"";
            }

            if (DepsJsonPath != null)
            {
                args += $" --depsfile \"{DepsJsonPath}\"";
            }

            if (!string.IsNullOrEmpty(options.RollForward))
            {
                args += $" --roll-forward {options.RollForward}";
            }

            args += $" \"{Path}\"";
            return args;
        }

        private static string GetRuntimeConfigPath(RemoteInvokeOptions options, out IEnumerable<IDisposable> toDispose)
        {
            if (options.RuntimeConfigurationOptions?.Any() != true)
            {
                toDispose = null;
                return RuntimeConfigPath;
            }

            // to support RuntimeConfigurationOptions, copy the runtimeconfig.json file to
            // a temp file and add the options to the runtimeconfig.dev.json file.

            // NOTE: using the dev.json file so we don't need to parse and edit the runtimeconfig.json
            // which would require a reference to System.Text.Json.

            string tempFile = System.IO.Path.GetTempFileName();
            string configFile = tempFile + ".runtimeconfig.json";
            string devConfigFile = System.IO.Path.ChangeExtension(configFile, "dev.json");

            File.Copy(RuntimeConfigPath, configFile);

            string configProperties = string.Join(
                "," + Environment.NewLine,
                options.RuntimeConfigurationOptions.Select(kvp => $"\"{kvp.Key}\": {ToJsonString(kvp.Value)}"));

            string devConfigFileContents =
@"
{
  ""runtimeOptions"": {
    ""configProperties"": {
"
+ configProperties +
@"
    }
  }
}";

            File.WriteAllText(devConfigFile, devConfigFileContents);

            toDispose = new IDisposable[] { new FileDeleter(tempFile, configFile, devConfigFile) };
            return configFile;
        }

        private static string ToJsonString(object value) =>
            value switch
            {
                string s => $"\"{s}\"",
                bool b => b ? "true" : "false",
                _ => value.ToString(),
            };

        private class FileDeleter : IDisposable
        {
            private readonly string[] _filesToDelete;

            public FileDeleter(params string[] filesToDelete)
            {
                _filesToDelete = filesToDelete;
            }

            public void Dispose()
            {
                foreach (string file in _filesToDelete)
                {
                    File.Delete(file);
                }
            }
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
                foreach (FieldInfo fi in targetType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                {
                    if (fi.Name.IndexOf('<') == -1)
                    {
                        throw new ArgumentException($"Field marshaling is not supported by {nameof(Invoke)}: {fi.Name}");

                    }
                }
            }

            return d.GetMethodInfo();
        }

        private static string RuntimeConfigPath
        {
            get
            {
                if (s_runtimeConfigPath == null)
                {
                    InitializePaths();
                }

                return s_runtimeConfigPath;
            }
        }

        private static string DepsJsonPath
        {
            get
            {
                if (s_depsJsonPath == null)
                {
                    InitializePaths();
                }

                return s_depsJsonPath;
            }
        }

        private static void InitializePaths()
        {
            Assembly currentAssembly = typeof(RemoteExecutor).Assembly;

            // We deep-dive into the loaded assemblies and search for the most inner runtimeconfig.json.
            // We need to check for null as global methods in a module don't belong to a type.
            IEnumerable<Assembly> assemblies = new StackTrace().GetFrames()
                .Select(frame => frame.GetMethod()?.ReflectedType?.Assembly)
                .Where(asm => asm != null && asm != currentAssembly)
                .Distinct();

            s_runtimeConfigPath = assemblies
                .Select(asm => System.IO.Path.Combine(AppContext.BaseDirectory, asm.GetName().Name + ".runtimeconfig.json"))
                .Where(File.Exists)
                .FirstOrDefault();

            s_depsJsonPath = assemblies
                .Select(asm => System.IO.Path.Combine(AppContext.BaseDirectory, asm.GetName().Name + ".deps.json"))
                .Where(File.Exists)
                .FirstOrDefault();
        }
    }
}
