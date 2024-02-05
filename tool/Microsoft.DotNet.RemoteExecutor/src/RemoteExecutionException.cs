// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;

namespace Microsoft.DotNet.RemoteExecutor
{
    /// <summary>
    /// Exception thrown for a remote execution failure.
    /// </summary>
    public sealed class RemoteExecutionException : Exception
    {
        private readonly string? _stackTrace;
        public RemoteExecutionException(string message, string? stackTrace = null)
            : base(message)
        {
            _stackTrace = stackTrace;
        }

        public override string? StackTrace => _stackTrace ?? base.StackTrace;
    }
}
