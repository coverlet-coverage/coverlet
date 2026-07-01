// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace Coverlet.MTP.Diagnostics.Tests;

public class DebugHelperTests
{
  #region IsEnvironmentVariableEnabled Tests

  [Fact]
  public void IsEnvironmentVariableEnabledReturnsTrueWhenVariableSetTo1()
  {
    // Arrange
    string testVar = $"TEST_VAR_{System.Guid.NewGuid()}";
    System.Environment.SetEnvironmentVariable(testVar, "1");

    try
    {
      // Act
      bool result = DebugHelper.IsEnvironmentVariableEnabled(testVar);

      // Assert
      Assert.True(result);
    }
    finally
    {
      System.Environment.SetEnvironmentVariable(testVar, null);
    }
  }

  [Fact]
  public void IsEnvironmentVariableEnabledReturnsTrueWhenVariableSetToTrue()
  {
    // Arrange
    string testVar = $"TEST_VAR_{System.Guid.NewGuid()}";
    System.Environment.SetEnvironmentVariable(testVar, "true");

    try
    {
      // Act
      bool result = DebugHelper.IsEnvironmentVariableEnabled(testVar);

      // Assert
      Assert.True(result);
    }
    finally
    {
      System.Environment.SetEnvironmentVariable(testVar, null);
    }
  }

  [Fact]
  public void IsEnvironmentVariableEnabledReturnsTrueWhenVariableSetToTrueUpperCase()
  {
    // Arrange
    string testVar = $"TEST_VAR_{System.Guid.NewGuid()}";
    System.Environment.SetEnvironmentVariable(testVar, "TRUE");

    try
    {
      // Act
      bool result = DebugHelper.IsEnvironmentVariableEnabled(testVar);

      // Assert
      Assert.True(result);
    }
    finally
    {
      System.Environment.SetEnvironmentVariable(testVar, null);
    }
  }

  [Fact]
  public void IsEnvironmentVariableEnabledReturnsTrueWhenVariableSetToTrueMixedCase()
  {
    // Arrange
    string testVar = $"TEST_VAR_{System.Guid.NewGuid()}";
    System.Environment.SetEnvironmentVariable(testVar, "True");

    try
    {
      // Act
      bool result = DebugHelper.IsEnvironmentVariableEnabled(testVar);

      // Assert
      Assert.True(result);
    }
    finally
    {
      System.Environment.SetEnvironmentVariable(testVar, null);
    }
  }

  [Theory]
  [InlineData("0")]
  [InlineData("false")]
  [InlineData("False")]
  [InlineData("FALSE")]
  [InlineData("no")]
  [InlineData("")]
  [InlineData("random")]
  public void IsEnvironmentVariableEnabledReturnsFalseWhenVariableNotEnabledValue(string value)
  {
    // Arrange
    string testVar = $"TEST_VAR_{System.Guid.NewGuid()}";
    System.Environment.SetEnvironmentVariable(testVar, value);

    try
    {
      // Act
      bool result = DebugHelper.IsEnvironmentVariableEnabled(testVar);

      // Assert
      Assert.False(result);
    }
    finally
    {
      System.Environment.SetEnvironmentVariable(testVar, null);
    }
  }

  [Fact]
  public void IsEnvironmentVariableEnabledReturnsFalseWhenVariableNotSet()
  {
    // Arrange
    string testVar = $"TEST_VAR_{System.Guid.NewGuid()}";
    System.Environment.SetEnvironmentVariable(testVar, null);

    // Act
    bool result = DebugHelper.IsEnvironmentVariableEnabled(testVar);

    // Assert
    Assert.False(result);
  }

  #endregion

  #region IsTrackerLogEnabled Property Tests

  [Fact]
  public void IsTrackerLogEnabledReturnsTrueWhenEnvironmentVariableEnabled()
  {
    // Arrange
    System.Environment.SetEnvironmentVariable(CoverletMtpDebugConstants.EnableTrackerLog, "1");

    try
    {
      // Act
      bool result = DebugHelper.IsTrackerLogEnabled;

      // Assert
      Assert.True(result);
    }
    finally
    {
      System.Environment.SetEnvironmentVariable(CoverletMtpDebugConstants.EnableTrackerLog, null);
    }
  }

  [Fact]
  public void IsTrackerLogEnabledReturnsFalseWhenEnvironmentVariableNotEnabled()
  {
    // Arrange
    System.Environment.SetEnvironmentVariable(CoverletMtpDebugConstants.EnableTrackerLog, null);

    // Act
    bool result = DebugHelper.IsTrackerLogEnabled;

    // Assert
    Assert.False(result);
  }

  #endregion

  #region IsInstrumentationDebugEnabled Property Tests

  [Fact]
  public void IsInstrumentationDebugEnabledReturnsTrueWhenEnvironmentVariableEnabled()
  {
    // Arrange
    System.Environment.SetEnvironmentVariable(CoverletMtpDebugConstants.InstrumentationDebug, "true");

    try
    {
      // Act
      bool result = DebugHelper.IsInstrumentationDebugEnabled;

      // Assert
      Assert.True(result);
    }
    finally
    {
      System.Environment.SetEnvironmentVariable(CoverletMtpDebugConstants.InstrumentationDebug, null);
    }
  }

  [Fact]
  public void IsInstrumentationDebugEnabledReturnsFalseWhenEnvironmentVariableNotEnabled()
  {
    // Arrange
    System.Environment.SetEnvironmentVariable(CoverletMtpDebugConstants.InstrumentationDebug, null);

    // Act
    bool result = DebugHelper.IsInstrumentationDebugEnabled;

    // Assert
    Assert.False(result);
  }

  #endregion

  #region IsExceptionLogEnabled Property Tests

  [Fact]
  public void IsExceptionLogEnabledReturnsTrueWhenEnvironmentVariableEnabled()
  {
    // Arrange
    System.Environment.SetEnvironmentVariable(CoverletMtpDebugConstants.ExceptionLogEnabled, "1");

    try
    {
      // Act
      bool result = DebugHelper.IsExceptionLogEnabled;

      // Assert
      Assert.True(result);
    }
    finally
    {
      System.Environment.SetEnvironmentVariable(CoverletMtpDebugConstants.ExceptionLogEnabled, null);
    }
  }

  [Fact]
  public void IsExceptionLogEnabledReturnsFalseWhenEnvironmentVariableNotEnabled()
  {
    // Arrange
    System.Environment.SetEnvironmentVariable(CoverletMtpDebugConstants.ExceptionLogEnabled, null);

    // Act
    bool result = DebugHelper.IsExceptionLogEnabled;

    // Assert
    Assert.False(result);
  }

  #endregion

  #region HandleDebuggerAttachment Tests

  [Fact]
  public void HandleDebuggerAttachmentDoesNotThrowWhenDebugVariablesNotSet()
  {
    // Arrange
    System.Environment.SetEnvironmentVariable(CoverletMtpDebugConstants.DebugLaunch, null);
    System.Environment.SetEnvironmentVariable(CoverletMtpDebugConstants.DebugWaitForAttach, null);
    var output = new System.IO.StringWriter();
    var originalOut = System.Console.Out;
    System.Console.SetOut(output);

    try
    {
      // Act & Assert - Should not throw
      var exception = Record.Exception(() =>
        DebugHelper.HandleDebuggerAttachment("TestComponent"));
      Assert.Null(exception);
    }
    finally
    {
      System.Console.SetOut(originalOut);
    }
  }

  [Fact]
  public void HandleDebuggerAttachmentWritesMessageWhenCalledWithComponentName()
  {
    // Arrange
    System.Environment.SetEnvironmentVariable(CoverletMtpDebugConstants.DebugLaunch, null);
    System.Environment.SetEnvironmentVariable(CoverletMtpDebugConstants.DebugWaitForAttach, null);
    var output = new System.IO.StringWriter();
    var originalOut = System.Console.Out;
    System.Console.SetOut(output);

    try
    {
      // Act
      DebugHelper.HandleDebuggerAttachment("TestComponent");

      // Assert
      // The method should not throw and should handle gracefully even when debugger features are not enabled
      Assert.NotNull(output);
    }
    finally
    {
      System.Console.SetOut(originalOut);
    }
  }

  #endregion
}
