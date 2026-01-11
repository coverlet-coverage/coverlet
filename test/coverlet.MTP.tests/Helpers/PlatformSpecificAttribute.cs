// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

namespace Coverlet.MTP.Tests.Helpers;

/// <summary>
/// Marks a test that should only run on Windows.
/// </summary>
public sealed class WindowsOnlyFactAttribute(
  [CallerFilePath] string? sourceFilePath = null,
  [CallerLineNumber] int sourceLineNumber = -1)
    : FactAttribute(sourceFilePath, sourceLineNumber)
{
  public new string? Skip
  {
    get => RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
      ? base.Skip
      : "This test requires Windows";
    set => base.Skip = value;
  }
}

/// <summary>
/// Marks a theory that should only run on Windows.
/// </summary>
public sealed class WindowsOnlyTheoryAttribute(
  [CallerFilePath] string? sourceFilePath = null,
  [CallerLineNumber] int sourceLineNumber = -1)
    : TheoryAttribute(sourceFilePath, sourceLineNumber)
{
  public new string? Skip
  {
    get => RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
      ? base.Skip
      : "This test requires Windows";
    set => base.Skip = value;
  }
}

/// <summary>
/// Marks a test that should skip on Linux (runs on Windows and macOS).
/// </summary>
public sealed class SkipOnLinuxFactAttribute(
  [CallerFilePath] string? sourceFilePath = null,
  [CallerLineNumber] int sourceLineNumber = -1)
    : FactAttribute(sourceFilePath, sourceLineNumber)
{
  public new string? Skip
  {
    get => !RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
      ? base.Skip
      : "This test is skipped on Linux";
    set => base.Skip = value;
  }
}

/// <summary>
/// Marks a theory that should skip on Linux (runs on Windows and macOS).
/// </summary>
public sealed class SkipOnLinuxTheoryAttribute(
  [CallerFilePath] string? sourceFilePath = null,
  [CallerLineNumber] int sourceLineNumber = -1)
    : TheoryAttribute(sourceFilePath, sourceLineNumber)
{
  public new string? Skip
  {
    get => !RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
      ? base.Skip
      : "This test is skipped on Linux";
    set => base.Skip = value;
  }
}
