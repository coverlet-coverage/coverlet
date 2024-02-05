// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace Microsoft.DotNet.RemoteExecutor
{
    internal static class MiniDump
    {
        public static void Create(Process process, string destinationPath)
        {
            if (process is null)
            {
                throw new ArgumentNullException(nameof(process));
            }
            if (destinationPath is null)
            {
                throw new ArgumentNullException(nameof(destinationPath));
            }

            using (FileStream fs = File.Create(destinationPath))
            {
                const MINIDUMP_TYPE MiniDumpType =
                    MINIDUMP_TYPE.MiniDumpWithFullMemory |
                    MINIDUMP_TYPE.MiniDumpIgnoreInaccessibleMemory;

                if (MiniDumpWriteDump(process.SafeHandle, process.Id, fs.SafeFileHandle, MiniDumpType, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero) == 0)
                {
                    throw new Win32Exception();
                }
            }
        }

        [DllImport("DbgHelp.dll", SetLastError = true)]
        private static extern int MiniDumpWriteDump(
            SafeHandle hProcess,
            int ProcessId,
            SafeHandle hFile,
            MINIDUMP_TYPE DumpType,
            IntPtr ExceptionParam,
            IntPtr UserStreamParam,
            IntPtr CallbackParam);

        private enum MINIDUMP_TYPE : int
        {
            MiniDumpNormal = 0x00000000,
            MiniDumpWithDataSegs = 0x00000001,
            MiniDumpWithFullMemory = 0x00000002,
            MiniDumpWithHandleData = 0x00000004,
            MiniDumpFilterMemory = 0x00000008,
            MiniDumpScanMemory = 0x00000010,
            MiniDumpWithUnloadedModules = 0x00000020,
            MiniDumpWithIndirectlyReferencedMemory = 0x00000040,
            MiniDumpFilterModulePaths = 0x00000080,
            MiniDumpWithProcessThreadData = 0x00000100,
            MiniDumpWithPrivateReadWriteMemory = 0x00000200,
            MiniDumpWithoutOptionalData = 0x00000400,
            MiniDumpWithFullMemoryInfo = 0x00000800,
            MiniDumpWithThreadInfo = 0x00001000,
            MiniDumpWithCodeSegs = 0x00002000,
            MiniDumpWithoutAuxiliaryState = 0x00004000,
            MiniDumpWithFullAuxiliaryState = 0x00008000,
            MiniDumpWithPrivateWriteCopyMemory = 0x00010000,
            MiniDumpIgnoreInaccessibleMemory = 0x00020000,
            MiniDumpWithTokenInformation = 0x00040000,
            MiniDumpWithModuleHeaders = 0x00080000,
            MiniDumpFilterTriage = 0x00100000,
            MiniDumpValidTypeFlags = 0x001fffff
        }
    }
}
