// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Coverlet.Core;
using Coverlet.Core.CoverageSamples.Tests;
using Coverlet.Core.Tests;
using Coverlet.Tests.Utils;
using Xunit;

namespace Coverlet.CoreCoverage.Tests
{
  public partial class CoverageTests
  {
    [Theory]
    [InlineData(nameof(Issue2058.WhileTrue_IfThenTryFinally))]
    [InlineData(nameof(Issue2058.WhileTrue_TryFinallyIfTryFinally))]
    [InlineData(nameof(Issue2058.WhileTrue_LockIfLock))]
    [InlineData(nameof(Issue2058.TryFinally_DoWhileBackEdge))]
    public void Issue2058_ReleaseInstrumentation_PrepareMethod_DoesNotThrow(string methodName)
    {
      BuildConfiguration configuration = TestUtils.GetAssemblyBuildConfiguration();
      string path = Path.GetTempFileName();
      try
      {
        FunctionExecutor.Run(async (string[] pathSerialize) =>
        {
          await TestInstrumentationHelper.Run<Issue2058>(instance =>
          {
            MethodInfo instrumentedMethod = instance.GetType().GetMethod(methodName);
            Assert.NotNull(instrumentedMethod);

            MethodInfo originalMethod = typeof(Issue2058).GetMethod(methodName);
            Assert.NotNull(originalMethod);

            string ilDumpPath = Path.Combine(Path.GetDirectoryName(pathSerialize[0]),
                $"Issue2058.{configuration}.{methodName}.il.txt");
            File.WriteAllText(ilDumpPath,
                BuildIlDiagnostic(methodName, configuration, originalMethod, instrumentedMethod));

            Exception exception = Record.Exception(() => RuntimeHelpers.PrepareMethod(instrumentedMethod.MethodHandle));
            Assert.True(exception is null,
                $"Expected JIT preparation to succeed for {methodName} in {configuration}. " +
                $"IL dump written to '{ilDumpPath}'. Exception: {exception}");
            return Task.CompletedTask;
          }, persistPrepareResultToFile: pathSerialize[0]);

          return 0;

        }, [path]);

        CoverageResult result = TestInstrumentationHelper.GetCoverageResult(path);
        Assert.True(result.IsPresent("Instrumentation.Issue2058.cs"));

        AssertMethodHasBothBranchPaths(result, methodName);
      }
      finally
      {
        File.Delete(path);
      }
    }

    private static void AssertMethodHasBothBranchPaths(CoverageResult result, string methodName)
    {
      var branches = result.Document("Instrumentation.Issue2058.cs").Branches.Values
          .Where(b => b.Method.Contains($"::{methodName}(", StringComparison.Ordinal))
          .ToList();

      Assert.NotEmpty(branches);
      Assert.Contains(branches, b => b.Path == 0);
      Assert.Contains(branches, b => b.Path == 1);
    }

    private static string BuildIlDiagnostic(string methodName, BuildConfiguration configuration, MethodInfo originalMethod, MethodInfo instrumentedMethod)
    {
      var builder = new StringBuilder();
      builder.AppendLine($"Method: {methodName}");
      builder.AppendLine($"Configuration: {configuration}");
      builder.AppendLine($"Original assembly: {originalMethod.Module.Assembly.Location}");
      builder.AppendLine($"Instrumented assembly: {instrumentedMethod.Module.Assembly.Location}");
      builder.AppendLine();
      builder.AppendLine("Original IL:");
      AppendMethodBody(builder, originalMethod);
      builder.AppendLine();
      builder.AppendLine("Instrumented IL:");
      AppendMethodBody(builder, instrumentedMethod);
      return builder.ToString();
    }

    private static void AppendMethodBody(StringBuilder builder, MethodInfo method)
    {
      MethodBody body = method.GetMethodBody();
      if (body is null)
      {
        builder.AppendLine("<no method body>");
        return;
      }

      byte[] il = body.GetILAsByteArray();
      builder.AppendLine($"MaxStack: {body.MaxStackSize}");
      builder.AppendLine($"InitLocals: {body.InitLocals}");
      builder.AppendLine($"LocalCount: {body.LocalVariables.Count}");
      builder.AppendLine($"ILBytesLength: {il.Length}");
      builder.AppendLine($"ILBytes: {BitConverter.ToString(il)}");

      if (body.ExceptionHandlingClauses.Count == 0)
      {
        builder.AppendLine("EH: <none>");
      }
      else
      {
        builder.AppendLine("EH:");
        foreach (ExceptionHandlingClause clause in body.ExceptionHandlingClauses)
        {
          string filter = clause.Flags == ExceptionHandlingClauseOptions.Filter
              ? clause.FilterOffset.ToString()
              : "n/a";
          string catchType = clause.Flags == ExceptionHandlingClauseOptions.Clause
              ? clause.CatchType?.FullName ?? "null"
              : "n/a";

          builder.AppendLine(
              $"  {clause.Flags} try[{clause.TryOffset}..{clause.TryOffset + clause.TryLength}) " +
              $"handler[{clause.HandlerOffset}..{clause.HandlerOffset + clause.HandlerLength}) " +
              $"filter[{filter}] catchType[{catchType}]");
        }
      }
    }
  }
}
