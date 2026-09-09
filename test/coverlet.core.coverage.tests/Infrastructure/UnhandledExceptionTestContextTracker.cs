// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Text;
using System.Threading;
using Xunit.v3;

namespace Coverlet.Core.Tests.Infrastructure;

internal static class UnhandledExceptionTestContextTracker
{
  private static readonly AsyncLocal<TestExecutionContext> s_currentContext = new();
  private static string s_lastKnownContext = string.Empty;

  public static void Set(IXunitTest test)
  {
    if (test is null)
    {
      throw new ArgumentNullException(nameof(test));
    }

    var context = new TestExecutionContext(
      GetTestDisplayName(test),
      test.TestCase.UniqueID,
      test.TestCase.TestClassName,
      test.TestCase.TestMethodName,
      DateTimeOffset.UtcNow);

    s_currentContext.Value = context;
    Volatile.Write(ref s_lastKnownContext, context.ToDiagnosticString());
  }

  public static void Clear()
  {
    s_currentContext.Value = null;
  }

  public static string GetCurrentDiagnosticMessage()
  {
    TestExecutionContext currentContext = s_currentContext.Value;
    if (currentContext is not null)
    {
      return currentContext.ToDiagnosticString();
    }

    string lastKnownContext = Volatile.Read(ref s_lastKnownContext);
    return string.IsNullOrWhiteSpace(lastKnownContext)
      ? "Last running test: <unknown>"
      : lastKnownContext;
  }

  private static string GetTestDisplayName(IXunitTest test)
  {
    string testCaseDisplayName = test.TestCase.TestCaseDisplayName;
    if (!string.IsNullOrWhiteSpace(testCaseDisplayName))
    {
      return testCaseDisplayName;
    }

    string className = test.TestCase.TestClassName;
    string methodName = test.TestCase.TestMethodName;
    return string.IsNullOrWhiteSpace(className) || string.IsNullOrWhiteSpace(methodName)
      ? test.ToString() ?? "<unknown test>"
      : $"{className}.{methodName}";
  }

  private sealed class TestExecutionContext(
    string displayName,
    string testCaseUniqueId,
    string testClassName,
    string testMethodName,
    DateTimeOffset startedAtUtc)
  {
    public string ToDiagnosticString()
    {
      var builder = new StringBuilder();
      builder.Append("Last running test: ").Append(displayName);

      if (!string.IsNullOrWhiteSpace(testCaseUniqueId))
      {
        builder.Append(Environment.NewLine)
          .Append("Test case ID: ")
          .Append(testCaseUniqueId);
      }

      if (!string.IsNullOrWhiteSpace(testClassName))
      {
        builder.Append(Environment.NewLine)
          .Append("Test class: ")
          .Append(testClassName);
      }

      if (!string.IsNullOrWhiteSpace(testMethodName))
      {
        builder.Append(Environment.NewLine)
          .Append("Test method: ")
          .Append(testMethodName);
      }

      builder.Append(Environment.NewLine)
        .Append("Started (UTC): ")
        .Append(startedAtUtc.ToString("O"));

      return builder.ToString();
    }
  }
}

[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
internal sealed class TrackCurrentTestAttribute : Attribute, IBeforeAfterTestAttribute
{
  public void Before(MethodInfo methodUnderTest, IXunitTest test)
  {
    ArgumentNullException.ThrowIfNull(methodUnderTest);
    ArgumentNullException.ThrowIfNull(test);

    UnhandledExceptionTestContextTracker.Set(test);
  }

  public void After(MethodInfo methodUnderTest, IXunitTest test)
  {
    ArgumentNullException.ThrowIfNull(methodUnderTest);
    ArgumentNullException.ThrowIfNull(test);

    UnhandledExceptionTestContextTracker.Clear();
  }
}
