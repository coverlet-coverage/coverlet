// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace Microsoft.DotNet.RemoteExecutor
{
    /// <summary>
    /// Options used with RemoteInvoke.
    /// </summary>
    public sealed class RemoteInvokeOptions
    {
        private bool _runAsSudo;

        public bool Start { get; set; } = true;

        public ProcessStartInfo StartInfo { get; set; } = new ProcessStartInfo();

        public bool EnableProfiling { get; set; } = true;

        public bool CheckExitCode { get; set; } = true;

        /// <summary>
        /// A timeout (milliseconds) after which a wait on a remote operation should be considered a failure.
        /// </summary>
        public int TimeOut { get; set; } = RemoteExecutor.FailWaitTimeoutMilliseconds;

        /// <summary>
        /// The exit code returned when the test process exits successfully.
        /// </summary>
        public int ExpectedExitCode { get; set; } = RemoteExecutor.SuccessExitCode;

        public string ExceptionFile { get; } = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        /// <summary>
        /// Gets the runtimeconfig options (or AppContext settings) to set for the remote process.
        /// </summary>
        /// <remarks>
        /// This option only works with .NET Core processes.
        /// </remarks>
        public IDictionary<string, object> RuntimeConfigurationOptions { get; } = new Dictionary<string, object>();

        public bool RunAsSudo
        {
            get => _runAsSudo;
            set
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    throw new PlatformNotSupportedException();
                }

                _runAsSudo = value;
            }
        }

        /// <summary>
        /// Specifies the roll-forward policy for dotnet cli to use. Only applies when running .NET Core
        /// </summary>
        public string RollForward { get; set; }
    }
}
