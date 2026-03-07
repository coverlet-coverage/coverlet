// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace Coverlet.Core.Tests.Infrastructure;

/// <summary>
/// Executes test methods in a separate process for isolation.
/// Replacement for Tmds.ExecFunction.
/// </summary>
public static class ProcessExecutor
{
  private const string ExecArgPrefix = "--exec-method";

  /// <summary>
  /// Runs a static async method in a separate process.
  /// </summary>
  /// <param name="methodType">The type containing the method</param>
  /// <param name="methodName">The name of the static method to execute</param>
  /// <param name="args">Arguments to pass to the method</param>
  /// <returns>The process exit code</returns>
  public static int Run(Type methodType, string methodName, string[] args = null)
  {
    string assemblyPath = typeof(ProcessExecutor).Assembly.Location;

    // For single-file deployments, Location may be empty
    if (string.IsNullOrEmpty(assemblyPath))
    {
      assemblyPath = AppContext.BaseDirectory;
    }

    string depsFile = Path.ChangeExtension(assemblyPath, ".deps.json");
    string runtimeConfig = Path.ChangeExtension(assemblyPath, ".runtimeconfig.json");

    var argumentList = new List<string>
    {
      "exec"
    };

    if (File.Exists(depsFile))
    {
      argumentList.Add("--depsfile");
      argumentList.Add(depsFile);
    }

    if (File.Exists(runtimeConfig))
    {
      argumentList.Add("--runtimeconfig");
      argumentList.Add(runtimeConfig);
    }

    argumentList.Add(assemblyPath);
    argumentList.Add(ExecArgPrefix);
    argumentList.Add(methodType.AssemblyQualifiedName);
    argumentList.Add(methodName);

    if (args is not null)
    {
      argumentList.AddRange(args);
    }

    var psi = new ProcessStartInfo
    {
      FileName = "dotnet",
      UseShellExecute = false,
      RedirectStandardOutput = true,
      RedirectStandardError = true,
      CreateNoWindow = true
    };

    foreach (string arg in argumentList)
    {
      psi.ArgumentList.Add(arg);
    }

    using Process process = Process.Start(psi);

    Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync();
    Task<string> stderrTask = process.StandardError.ReadToEndAsync();
    process.WaitForExit();
    string stdout = stdoutTask.Result;
    string stderr = stderrTask.Result;

    if (process.ExitCode != 0)
    {
      string errorMessage = $"Process exited with code {process.ExitCode}.";
      if (!string.IsNullOrWhiteSpace(stderr))
      {
        errorMessage += $" StdErr: {stderr}";
      }
      if (!string.IsNullOrWhiteSpace(stdout))
      {
        errorMessage += $" StdOut: {stdout}";
      }
      throw new InvalidOperationException(errorMessage);
    }

    return process.ExitCode;
  }

  /// <summary>
  /// Entry point handler for child process execution.
  /// Call this from Main() to handle --exec-method invocations.
  /// </summary>
  /// <returns>True if this was an exec invocation and it was handled; false otherwise</returns>
  public static bool TryExecute(string[] args)
  {
    if (args.Length < 3 || args[0] != ExecArgPrefix)
    {
      return false;
    }

    string typeName = args[1];
    string methodName = args[2];
    string[] methodArgs = args.Length > 3 ? args[3..] : Array.Empty<string>();

    Type type = Type.GetType(typeName)
      ?? throw new InvalidOperationException($"Type not found: {typeName}");

    MethodInfo method = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
      ?? throw new InvalidOperationException($"Method not found: {methodName} in type {typeName}");

    object[] invokeArgs = method.GetParameters().Length > 0 ? [methodArgs] : null;
    object result = method.Invoke(null, invokeArgs);

    // Handle async methods
    if (result is Task task)
    {
      task.GetAwaiter().GetResult();
      if (result is Task<int> taskWithResult)
      {
        Environment.ExitCode = taskWithResult.Result;
      }
    }
    else if (result is int exitCode)
    {
      Environment.ExitCode = exitCode;
    }

    return true;
  }
}
