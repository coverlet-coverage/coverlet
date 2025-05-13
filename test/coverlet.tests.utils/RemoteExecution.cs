// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Runtime.InteropServices;

// from https://github.com/dotnet/command-line-api/blob/main/src/System.CommandLine.Tests/Utility/RemoteExecution.cs
namespace coverlet.tests.utils
{
  public class RemoteExecution : IDisposable
  {
    private const int FailWaitTimeoutMilliseconds = 60 * 1000;
    private readonly string _exceptionFile;

    public RemoteExecution(System.Diagnostics.Process process, string className, string methodName, string exceptionFile)
    {
      Process = process;
      ClassName = className;
      MethodName = methodName;
      _exceptionFile = exceptionFile;
    }

    public System.Diagnostics.Process Process { get; private set; }
    public string ClassName { get; }
    public string MethodName { get; }

    public string FunctionExecutor;

    public void Dispose()
    {
      GC.SuppressFinalize(this); // before Dispose(true) in case the Dispose call throws
      Dispose(disposing: true);
    }

    private void Dispose(bool disposing)
    {
      //Assert.True(disposing, $"A test {ClassName}.{MethodName} forgot to Dispose() the result of RemoteInvoke()");

      if (Process != null)
      {
        //Assert.True(Process.WaitForExit(FailWaitTimeoutMilliseconds),
        //$"Timed out after {FailWaitTimeoutMilliseconds}ms waiting for remote process {Process.Id}");

        // A bit unorthodox to do throwing operations in a Dispose, but by doing it here we avoid
        // needing to do this in every derived test and keep each test much simpler.
        try
        {
          if (File.Exists(_exceptionFile))
          {
            throw new RemoteExecutionException(File.ReadAllText(_exceptionFile));
          }
        }
        finally
        {
          if (File.Exists(_exceptionFile))
          {
            File.Delete(_exceptionFile);
          }

          // Cleanup
          try { Process.Kill(); }
          catch { } // ignore all cleanup errors
        }

        Process.Dispose();
        Process = null;
      }
    }

    private sealed class RemoteExecutionException : Exception
    {
      private readonly string _stackTrace;

      internal RemoteExecutionException(string stackTrace)
          : base("Remote process failed with an unhandled exception.")
      {
        _stackTrace = stackTrace;
      }

      public override string StackTrace => _stackTrace ?? base.StackTrace;
    }
  }
  internal static class DotnetMuxer
  {
    public static FileInfo Path { get; }

    static DotnetMuxer()
    {
      var muxerFileName = ExecutableName("dotnet");
      var fxDepsFile = GetDataFromAppDomain("FX_DEPS_FILE");

      if (string.IsNullOrEmpty(fxDepsFile))
      {
        return;
      }

      var muxerDir = new FileInfo(fxDepsFile).Directory?.Parent?.Parent?.Parent;

      if (muxerDir is null)
      {
        return;
      }

      var muxerCandidate = new FileInfo(System.IO.Path.Combine(muxerDir.FullName, muxerFileName));

      if (muxerCandidate.Exists)
      {
        Path = muxerCandidate;
      }
      else
      {
        throw new InvalidOperationException("no muxer!");
      }
    }

    public static string GetDataFromAppDomain(string propertyName)
    {
      return AppContext.GetData(propertyName) as string;
    }

    public static string ExecutableName(this string withoutExtension) =>
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? withoutExtension + ".exe"
            : withoutExtension;
  }
  public static class Process
  {
    public static int RunToCompletion(
        string command,
        string args,
        Action<string> stdOut = null,
        Action<string> stdErr = null,
        string workingDirectory = null,
        params (string key, string value)[] environmentVariables)
    {
      args ??= "";

      var process = new System.Diagnostics.Process
      {
        StartInfo =
            {
                Arguments = args,
                FileName = command,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                UseShellExecute = false
            }
      };

      if (!string.IsNullOrWhiteSpace(workingDirectory))
      {
        process.StartInfo.WorkingDirectory = workingDirectory;
      }

      if (environmentVariables.Length > 0)
      {
        for (var i = 0; i < environmentVariables.Length; i++)
        {
          var (key, value) = environmentVariables[i];
          process.StartInfo.Environment.Add(key, value);
        }
      }

      if (stdOut != null)
      {
        process.OutputDataReceived += (sender, eventArgs) =>
        {
          if (eventArgs.Data != null)
          {
            stdOut(eventArgs.Data);
          }
        };
      }

      if (stdErr != null)
      {
        process.ErrorDataReceived += (sender, eventArgs) =>
        {
          if (eventArgs.Data != null)
          {
            stdErr(eventArgs.Data);
          }
        };
      }

      process.Start();

      process.BeginOutputReadLine();
      process.BeginErrorReadLine();

      process.WaitForExit();

      return process.ExitCode;
    }
  }
}
