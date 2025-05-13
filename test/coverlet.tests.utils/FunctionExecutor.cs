// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace coverlet.tests.utils
{
  public class FunctionExecutor
  {
    private readonly Action<ExecFunctionOptions> _configure;

    public FunctionExecutor(Action<ExecFunctionOptions> configure)
    {
      _configure = configure;
    }

    public Process Start(Action action, Action<ExecFunctionOptions> configure = null)
        => ExecFunction.Start(action, _configure + configure);

    public Process Start(Action<string[]> action, string[] args, Action<ExecFunctionOptions> configure = null)
        => ExecFunction.Start(action, args, _configure + configure);

    public Process Start(Func<int> action, Action<ExecFunctionOptions> configure = null)
        => ExecFunction.Start(action, _configure + configure);

    public Process Start(Func<string[], int> action, string[] args, Action<ExecFunctionOptions> configure = null)
        => ExecFunction.Start(action, args, _configure + configure);

    public Process Start(Func<Task> action, Action<ExecFunctionOptions> configure = null)
        => ExecFunction.Start(action, _configure + configure);

    public Process Start(Func<string[], Task> action, string[] args, Action<ExecFunctionOptions> configure = null)
        => ExecFunction.Start(action, args, _configure + configure);

    public Process Start(Func<Task<int>> action, Action<ExecFunctionOptions> configure = null)
        => ExecFunction.Start(action, _configure + configure);

    public Process Start(Func<string[], Task<int>> action, string[] args, Action<ExecFunctionOptions> configure = null)
        => ExecFunction.Start(action, args, _configure + configure);

    public void Run(Action action, Action<ExecFunctionOptions> configure = null)
        => ExecFunction.Run(action, _configure + configure);

    public void Run(Action<string[]> action, string[] args, Action<ExecFunctionOptions> configure = null)
        => ExecFunction.Run(action, args, _configure + configure);

    public void Run(Func<int> action, Action<ExecFunctionOptions> configure = null)
        => ExecFunction.Run(action, _configure + configure);

    public void Run(Func<string[], int> action, string[] args, Action<ExecFunctionOptions> configure = null)
        => ExecFunction.Run(action, args, _configure + configure);

    public void Run(Func<Task> action, Action<ExecFunctionOptions> configure = null)
        => ExecFunction.Run(action, _configure + configure);

    public void Run(Func<string[], Task> action, string[] args, Action<ExecFunctionOptions> configure = null)
        => ExecFunction.Run(action, args, _configure + configure);

    public void Run(Func<Task<int>> action, Action<ExecFunctionOptions> configure = null)
        => ExecFunction.Run(action, _configure + configure);

    public void Run(Func<string[], Task<int>> action, string[] args, Action<ExecFunctionOptions> configure = null)
        => ExecFunction.Run(action, args, _configure + configure);

    public Task RunAsync(Action action, Action<ExecFunctionOptions> configure = null)
        => ExecFunction.RunAsync(action, _configure + configure);

    public Task RunAsync(Action<string[]> action, string[] args, Action<ExecFunctionOptions> configure = null)
        => ExecFunction.RunAsync(action, args, _configure + configure);

    public Task RunAsync(Func<int> action, Action<ExecFunctionOptions> configure = null)
        => ExecFunction.RunAsync(action, _configure + configure);

    public Task RunAsync(Func<string[], int> action, string[] args, Action<ExecFunctionOptions> configure = null)
        => ExecFunction.RunAsync(action, args, _configure + configure);

    public Task RunAsync(Func<Task> action, Action<ExecFunctionOptions> configure = null)
        => ExecFunction.RunAsync(action, _configure + configure);

    public Task RunAsync(Func<string[], Task> action, string[] args, Action<ExecFunctionOptions> configure = null)
        => ExecFunction.RunAsync(action, args, _configure + configure);

    public Task RunAsync(Func<Task<int>> action, Action<ExecFunctionOptions> configure = null)
        => ExecFunction.RunAsync(action, _configure + configure);

    public Task RunAsync(Func<string[], Task<int>> action, string[] args, Action<ExecFunctionOptions> configure = null)
        => ExecFunction.RunAsync(action, args, _configure + configure);
  }
}
