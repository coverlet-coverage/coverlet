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
/// </summary>
public class FunctionExecutor
{
  private readonly Action<FunctionExecutorOptions> _configure;

  public FunctionExecutor(Action<FunctionExecutorOptions> configure = null)
  {
    _configure = configure;
  }

  /// <summary>
  /// Runs a function that takes no arguments in a separate process.
  /// </summary>
  public void Run(Func<Task<int>> func)
  {
    ProcessExecutor.Run(func.Method.DeclaringType, func.Method.Name, null);
  }

  /// <summary>
  /// Runs a function with string array arguments in a separate process.
  /// </summary>
  public void Run(Func<string[], Task<int>> func, string[] args)
  {
    ProcessExecutor.Run(func.Method.DeclaringType, func.Method.Name, args);
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
