// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Coverlet.Core.Tests.Infrastructure;

/// <summary>
/// Compatibility wrapper that maintains the existing FunctionExecutor API.
/// This allows minimal changes to existing test code.
/// Replacement for Tmds.ExecFunction.FunctionExecutor.
///
/// Note: Unlike Tmds.ExecFunction which executed lambdas in a separate process
/// via expression tree serialization, this implementation runs lambdas in-process.
/// For test isolation purposes, the instrumented assembly copy mechanism in
/// TestInstrumentationHelper.Run already provides the necessary isolation.
/// </summary>
public class FunctionExecutor
{
  private readonly Action<FunctionExecutorOptions> _configure;

  public FunctionExecutor(Action<FunctionExecutorOptions> configure = null)
  {
    // Store the configuration action for potential future use, even though it's not utilized in this in-process implementation.
    _configure = configure;
  }

  /// <summary>
  /// Runs a function that takes no arguments.
  /// The function is executed in the current process on a thread pool thread
  /// to avoid SynchronizationContext deadlocks in Visual Studio.
  /// </summary>
  public void Run(Func<Task<int>> func)
  {
    // Use Task.Run to escape any SynchronizationContext (e.g., Visual Studio UI context)
    // This prevents deadlocks when calling .GetAwaiter().GetResult()
    int result = Task.Run(async () => await func().ConfigureAwait(false)).GetAwaiter().GetResult();
    if (result != 0)
    {
      throw new InvalidOperationException($"Function returned non-zero exit code: {result}");
    }
  }

  /// <summary>
  /// Runs a function with string array arguments.
  /// The function is executed in the current process on a thread pool thread
  /// to avoid SynchronizationContext deadlocks in Visual Studio.
  /// </summary>
  public void Run(Func<string[], Task<int>> func, string[] args)
  {
    // Use Task.Run to escape any SynchronizationContext (e.g., Visual Studio UI context)
    // This prevents deadlocks when calling .GetAwaiter().GetResult()
    int result = Task.Run(async () => await func(args).ConfigureAwait(false)).GetAwaiter().GetResult();
    if (result != 0)
    {
      throw new InvalidOperationException($"Function returned non-zero exit code: {result}");
    }
  }
}

/// <summary>
/// Options for configuring process execution.
/// Provided for API compatibility with Tmds.ExecFunction.
/// </summary>
public class FunctionExecutorOptions
{
  public ProcessStartInfo StartInfo { get; set; } = new ProcessStartInfo();
  public Action<Process> OnExit { get; set; }
}
