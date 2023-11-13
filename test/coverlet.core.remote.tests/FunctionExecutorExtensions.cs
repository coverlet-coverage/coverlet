// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Tmds.Utils;
using Xunit;

namespace coverlet.core.remote.tests
{
  public abstract class ExternalProcessExecutionTest
  {
    protected FunctionExecutor FunctionExecutor = new(
    o =>
    {
      o.StartInfo.RedirectStandardError = true;
      o.OnExit = p =>
      {
        if (p.ExitCode != 0)
        {
          string message = $"Function exit code failed with exit code: {p.ExitCode}" + Environment.NewLine +
                                p.StandardError.ReadToEnd();
          throw new Xunit.Sdk.XunitException(message);
        }
      };
    });
  }

  public static class FunctionExecutorExtensions
  {
    public static void RunInProcess(this FunctionExecutor executor, Func<string[], Task<int>> func, string[] args)
    {
      Assert.Equal(0, func(args).Result);
    }

    public static void RunInProcess(this FunctionExecutor executor, Func<Task<int>> func)
    {
      Assert.Equal(0, func().Result);
    }
  }
}

