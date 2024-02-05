// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.DotNet.RemoteExecutor.Tests
{
    public class RemoteExecutorTests
    {
        [Fact]
        public void Action()
        {
            RemoteInvokeHandle h = RemoteExecutor.Invoke(() => { }, new RemoteInvokeOptions { RollForward = "Major" });
            using (h)
            {
                Assert.Equal(RemoteExecutor.SuccessExitCode, h.ExitCode);
            }
        }

        [Fact]
        public void AsyncAction_ThrowException()
        {
            Assert.Throws<RemoteExecutionException>(() =>
                RemoteExecutor.Invoke(async () =>
                {
                    Assert.True(false);
                    await Task.Delay(1);
                }, new RemoteInvokeOptions { RollForward = "Major" }).Dispose()
            );
        }

        [Fact]
        public void AsyncAction()
        {
            RemoteInvokeHandle h = RemoteExecutor.Invoke(async () =>
            {
                await Task.Delay(1);
            }, new RemoteInvokeOptions { RollForward = "Major" });
            using (h)
            {
                Assert.Equal(RemoteExecutor.SuccessExitCode, h.ExitCode);
            }
        }

        [Fact]
        public void AsyncFunc_ThrowException()
        {
            Assert.Throws<RemoteExecutionException>(() =>
                RemoteExecutor.Invoke(async () =>
                {
                    Assert.True(false);
                    await Task.Delay(1);
                    return 1;
                }, new RemoteInvokeOptions { RollForward = "Major" }).Dispose()
            );
        }

        [Fact]
        public void AsyncFuncFiveArgs_ThrowException()
        {
            Assert.Throws<RemoteExecutionException>(() =>
                RemoteExecutor.Invoke(async (a, b, c, d, e) =>
                {
                    Assert.True(false);
                    await Task.Delay(1);
                }, "a", "b", "c", "d", "e", new RemoteInvokeOptions { RollForward = "Major" }).Dispose()
            );
        }

        [Fact]
        public void AsyncFunc_InvalidReturnCode()
        {
            Assert.ThrowsAny<RemoteExecutionException>(() =>
                RemoteExecutor.Invoke(async () =>
                {
                    await Task.Delay(1);
                    return 1;
                }, new RemoteInvokeOptions { RollForward = "Major" }).Dispose()
            );
        }

        [Fact]
        public void AsyncFunc_NoThrow_ValidReturnCode()
        {
            RemoteExecutor.Invoke(async () =>
            {
                await Task.Delay(1);
                return RemoteExecutor.SuccessExitCode;
            }, new RemoteInvokeOptions { RollForward = "Major" }).Dispose();
        }

        [Fact]
        public static void AsyncAction_FatalError_AV()
        {
            // Invocation should report as failing on AV
            Assert.ThrowsAny<RemoteExecutionException>(() =>
                RemoteExecutor.Invoke(async () =>
                {
                    await Task.Delay(1);
                    unsafe
                    {
                        *(int*)0x10000 = 0;
                    }
                }, new RemoteInvokeOptions { RollForward = "Major" }).Dispose()
            );
        }

        [Fact]
        public static void AsyncAction_FatalError_Runtime()
        {
            // Invocation should report as failing on fatal runtime error
            Assert.ThrowsAny<RemoteExecutionException>(() =>
                RemoteExecutor.Invoke(async () =>
                {
                    await Task.Delay(1);
                    System.Runtime.InteropServices.Marshal.StructureToPtr(1, new IntPtr(1), true);
                }, new RemoteInvokeOptions { RollForward = "Major" }).Dispose()
            );
        }

        [Fact]
        public static unsafe void FatalError_AV()
        {
            // Invocation should report as failing on AV
            Assert.ThrowsAny<RemoteExecutionException>(() =>
                RemoteExecutor.Invoke(() =>
                {
                    *(int*)0x10000 = 0;
                }, new RemoteInvokeOptions { RollForward = "Major" }).Dispose()
            );
        }

        [Fact]
        public static void FatalError_Runtime()
        {
            // Invocation should report as failing on fatal runtime error
            Assert.ThrowsAny<RemoteExecutionException>(() =>
                RemoteExecutor.Invoke(() =>
                {
                    System.Runtime.InteropServices.Marshal.StructureToPtr(1, new IntPtr(1), true);
                }, new RemoteInvokeOptions { RollForward = "Major" }).Dispose()
            );
        }

        [Fact]
        public static void IgnoreExitCode()
        {
            int exitCode = 1;
            RemoteInvokeHandle h = RemoteExecutor.Invoke(
                s => int.Parse(s),
                exitCode.ToString(),
                new RemoteInvokeOptions { RollForward = "Major", CheckExitCode = false, ExpectedExitCode = 0 });
            using(h)
            {
                Assert.Equal(exitCode, h.ExitCode);
            }
        }
    }
}
