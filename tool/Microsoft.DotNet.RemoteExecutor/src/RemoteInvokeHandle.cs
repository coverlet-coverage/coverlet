// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace Microsoft.DotNet.RemoteExecutor
{
    /// <summary>
    /// A cleanup handle to the Process created for the remote invocation.
    /// </summary>
    public sealed class RemoteInvokeHandle : IDisposable
    {
        public RemoteInvokeHandle(Process process, RemoteInvokeOptions options, string assemblyName = null, string className = null, string methodName = null, IEnumerable<IDisposable> subDisposables = null)
        {
            Process = process;
            Options = options;
            AssemblyName = assemblyName;
            ClassName = className;
            MethodName = methodName;
            SubDisposables = subDisposables;
        }

        public int ExitCode
        {
            get
            {
                Process.WaitForExit();
                return Process.ExitCode;
            }
        }

        public Process Process { get; set; }

        public RemoteInvokeOptions Options { get; private set; }

        public string AssemblyName { get; private set; }

        public string ClassName { get; private set; }

        public string MethodName { get; private set; }

        private IEnumerable<IDisposable> SubDisposables { get; }

        public void Dispose()
        {
            GC.SuppressFinalize(this); // before Dispose(true) in case the Dispose call throws
            Dispose(disposing: true);
        }

        /// <summary>
        /// Lock access to working with CLRMD.
        /// </summary>
        /// <remarks>
        /// ClrMD doesn't like attaching to multiple processes concurrently.  If we happen to
        /// hit multiple remote failures concurrently, only dump out one of them.
        /// </remarks>
        private static int s_clrMdLock;

        [StructLayout(LayoutKind.Sequential)]
        internal struct MEMORYSTATUSEX
        {
            // The length field must be set to the size of this data structure.
            internal int _length;
            internal int _memoryLoad;
            internal ulong _totalPhys;
            internal ulong _availPhys;
            internal ulong _totalPageFile;
            internal ulong _availPageFile;
            internal ulong _totalVirtual;
            internal ulong _availVirtual;
            internal ulong _availExtendedVirtual;
        }

        [DllImport("kernel32.dll", SetLastError = true, EntryPoint = "GlobalMemoryStatusEx")]
        private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

        private unsafe int GetMemoryLoad()
        {
            MEMORYSTATUSEX buffer = default;
            buffer._length = sizeof(MEMORYSTATUSEX);
            GlobalMemoryStatusEx(ref buffer);
            return buffer._memoryLoad;
        }

        private void Dispose(bool disposing)
        {
            if (!disposing)
            {
                throw new InvalidOperationException($"A test {AssemblyName}!{ClassName}.{MethodName} forgot to Dispose() the result of RemoteInvoke()");
            }

            try
            {
                if (Process != null)
                {
                    // A bit unorthodox to do throwing operations in a Dispose, but by doing it here we avoid
                    // needing to do this in every derived test and keep each test much simpler.
                    try
                    {
                        int halfTimeOut = Options.TimeOut == Timeout.Infinite ? Options.TimeOut : Options.TimeOut / 2;

                        if (!Process.WaitForExit(halfTimeOut))
                        {
                            var description = new StringBuilder();
                            description.AppendLine($"Half-way through waiting for remote process.");

                            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                            {
                                int memoryLoad = GetMemoryLoad();
                                description.AppendLine($"Memory load: {memoryLoad}");
                                if (memoryLoad > 80)
                                {
                                    foreach (Process p in Process.GetProcesses())
                                    {
                                        description.AppendLine($"Process: {p.Id} {p.ProcessName} PrivateMemory: {p.PrivateMemorySize64}");
                                    }
                                }

                                try
                                {
                                    Process p = Process.Start(new ProcessStartInfo()
                                    {
                                        FileName = "tasklist.exe",
                                        Arguments = "/svc /fi \"imagename eq svchost.exe\"",
                                        UseShellExecute = false,
                                        RedirectStandardOutput = true,
                                    }
                                    );

                                    description.Append(p.StandardOutput.ReadToEnd());
                                }
                                catch
                                {
                                }
                            }

                            if (!Process.WaitForExit(halfTimeOut))
                            {
                                description.AppendLine($"Timed out at {DateTime.Now} after {Options.TimeOut}ms waiting for remote process.");

                                // Create a dump if possible
                                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                                {
                                    string uploadPath = Environment.GetEnvironmentVariable("HELIX_WORKITEM_UPLOAD_ROOT");
                                    if (!string.IsNullOrWhiteSpace(uploadPath))
                                    {
                                        try
                                        {
                                            string miniDmpPath = Path.Combine(uploadPath, $"{Process.Id}.{Path.GetRandomFileName()}.dmp");
                                            MiniDump.Create(Process, miniDmpPath);
                                            description.AppendLine($"Wrote mini dump to: {miniDmpPath}");
                                        }
                                        catch (Exception exc)
                                        {
                                            description.AppendLine($"Failed to create mini dump: {exc.Message}");
                                        }
                                    }
                                }

                                // Gather additional details about the process if possible
                                try
                                {
                                    description.AppendLine($"\tProcess ID: {Process.Id}");
                                    description.AppendLine($"\tHandle: {Process.Handle}");
                                    description.AppendLine($"\tName: {Process.ProcessName}");
                                    description.AppendLine($"\tMainModule: {Process.MainModule?.FileName}");
                                    description.AppendLine($"\tStartTime: {Process.StartTime}");
                                    description.AppendLine($"\tTotalProcessorTime: {Process.TotalProcessorTime}");

                                    // Attach ClrMD to gather some additional details.
                                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && // As of Microsoft.Diagnostics.Runtime v1.0.5, process attach only works on Windows.
                                        Interlocked.CompareExchange(ref s_clrMdLock, 1, 0) == 0) // Make sure we only attach to one process at a time.
                                    {
                                        try
                                        {
                                            using DataTarget dt = DataTarget.AttachToProcess(Process.Id, msecTimeout: 20_000); // arbitrary timeout
                                            ClrRuntime runtime = dt.ClrVersions.FirstOrDefault()?.CreateRuntime();
                                            if (runtime != null)
                                            {
                                                // Dump the threads in the remote process.
                                                description.AppendLine("\tThreads:");
                                                foreach (ClrThread thread in runtime.Threads.Where(t => t.IsAlive))
                                                {
                                                    string threadKind =
                                                        thread.IsThreadpoolCompletionPort ? "[Thread pool completion port]" :
                                                        thread.IsThreadpoolGate ? "[Thread pool gate]" :
                                                        thread.IsThreadpoolTimer ? "[Thread pool timer]" :
                                                        thread.IsThreadpoolWait ? "[Thread pool wait]" :
                                                        thread.IsThreadpoolWorker ? "[Thread pool worker]" :
                                                        thread.IsFinalizer ? "[Finalizer]" :
                                                        thread.IsGC ? "[GC]" :
                                                        "";

                                                    string isBackground = thread.IsBackground ? "[Background]" : "";
                                                    string apartmentModel = thread.IsMTA ? "[MTA]" :
                                                                            thread.IsSTA ? "[STA]" :
                                                                            "";

                                                    description.AppendLine($"\t\tThread #{thread.ManagedThreadId} (OS 0x{thread.OSThreadId:X}) {threadKind} {isBackground} {apartmentModel}");
                                                    foreach (ClrStackFrame frame in thread.StackTrace)
                                                    {
                                                        description.AppendLine($"\t\t\t{frame}");
                                                    }
                                                }
                                            }
                                        }
                                        finally
                                        {
                                            Interlocked.Exchange(ref s_clrMdLock, 0);
                                        }
                                    }
                                }
                                catch { }

                                throw new RemoteExecutionException(description.ToString());
                            }
                        }

                        FileInfo exceptionFileInfo = new FileInfo(Options.ExceptionFile);
                        if (exceptionFileInfo.Exists && exceptionFileInfo.Length != 0)
                        {
                            throw new RemoteExecutionException("Remote process failed with an unhandled exception.", File.ReadAllText(Options.ExceptionFile));
                        }

                        if (Options.CheckExitCode)
                        {
                            int expected = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? Options.ExpectedExitCode : unchecked((sbyte)Options.ExpectedExitCode);
                            int actual = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? Process.ExitCode : unchecked((sbyte)Process.ExitCode);

                            if (expected != actual)
                            {
                                throw new RemoteExecutionException($"Exit code was {Process.ExitCode} but it should have been {Options.ExpectedExitCode}");
                            }
                        }
                    }
                    finally
                    {
                        if (File.Exists(Options.ExceptionFile))
                        {
                            File.Delete(Options.ExceptionFile);
                        }

                        // Cleanup
                        try { Process.Kill(); }
                        catch { } // ignore all cleanup errors

                        Process.Dispose();
                        Process = null;
                    }
                }
            }
            finally
            {
                if (SubDisposables != null)
                {
                    foreach (IDisposable disposable in SubDisposables)
                    {
                        disposable.Dispose();
                    }
                }
            }
        }

        ~RemoteInvokeHandle()
        {
            // Finalizer flags tests that omitted the explicit Dispose() call; they must have it, or they aren't
            // waiting on the remote execution
            Dispose(disposing: false);
        }
    }
}
