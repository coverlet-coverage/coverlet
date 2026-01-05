// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Coverlet.MTP.CommandLine;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.CommandLine;
using Moq;
using Xunit;

namespace Coverlet.MTP.Tests.CommandLine;

public class CoverletExtensionCommandLineProviderTests
{
  private readonly Mock<IExtension> _mockExtension;
  private readonly CoverletExtensionCommandLineProvider _provider;

  public CoverletExtensionCommandLineProviderTests()
  {
    _mockExtension = new Mock<IExtension>();
    _mockExtension.Setup(e => e.Uid).Returns("test-uid");
    _mockExtension.Setup(e => e.Version).Returns("8.0.0");
    _mockExtension.Setup(e => e.DisplayName).Returns("Test Extension");
    _mockExtension.Setup(e => e.Description).Returns("Test Description");

    _provider = new CoverletExtensionCommandLineProvider(_mockExtension.Object);
  }

  // ===== Property Tests (New Coverage) =====

  [Fact]
  public void Uid_ShouldReturnExtensionUid()
  {
    // Act
    string uid = _provider.Uid;

    // Assert
    Assert.Equal("test-uid", uid);
    _mockExtension.Verify(e => e.Uid, Times.Once);
  }

  [Fact]
  public void Version_ShouldReturnExtensionVersion()
  {
    // Act
    string version = _provider.Version;

    // Assert
    Assert.Equal("8.0.0", version);
    _mockExtension.Verify(e => e.Version, Times.Once);
  }

  [Fact]
  public void DisplayName_ShouldReturnExtensionDisplayName()
  {
    // Act
    string displayName = _provider.DisplayName;

    // Assert
    Assert.Equal("Test Extension", displayName);
    _mockExtension.Verify(e => e.DisplayName, Times.Once);
  }

  [Fact]
  public void Description_ShouldReturnExtensionDescription()
  {
    // Act
    string description = _provider.Description;

    // Assert
    Assert.Equal("Test Description", description);
    _mockExtension.Verify(e => e.Description, Times.Once);
  }

  // ===== IsEnabledAsync Test (New Coverage) =====

  [Fact]
  public async Task IsEnabledAsync_ShouldReturnTrue()
  {
    // Act
    bool result = await _provider.IsEnabledAsync();

    // Assert
    Assert.True(result);
  }

  // ===== Edge Case Tests (Add Unique Value) =====

  [Fact]
  public async Task ValidateOptionArgumentsAsync_Formats_WithEmptyArray_ShouldReturnValid()
  {
    // Arrange
    var commandOption = new CommandLineOption(CoverletOptionNames.Formats, "Format option description", ArgumentArity.OneOrMore, false);
    string[] arguments = [];

    // Act
    ValidationResult result = await _provider.ValidateOptionArgumentsAsync(commandOption, arguments);

    // Assert
    Assert.True(result.IsValid);
  }

  [Fact]
  public async Task ValidateOptionArgumentsAsync_Formats_WithWhitespaceArgument_ShouldReturnValid()
  {
    // Arrange
    var commandOption = new CommandLineOption(CoverletOptionNames.Formats, "Format option description", ArgumentArity.OneOrMore, false);
    string[] arguments = ["  ", ""];

    // Act
    ValidationResult result = await _provider.ValidateOptionArgumentsAsync(commandOption, arguments);

    // Assert
    Assert.True(result.IsValid);
  }

  [Fact]
  public async Task ValidateOptionArgumentsAsync_Formats_WithMixedValidAndInvalidFormats_ShouldReturnInvalid()
  {
    // Arrange
    var commandOption = new CommandLineOption(CoverletOptionNames.Formats, "Format option description", ArgumentArity.OneOrMore, false);
    string[] arguments = ["json", "invalid", "lcov"];

    // Act
    ValidationResult result = await _provider.ValidateOptionArgumentsAsync(commandOption, arguments);

    // Assert
    Assert.False(result.IsValid);
    Assert.Contains("invalid", result.ErrorMessage);
  }

  [Fact]
  public async Task ValidateOptionArgumentsAsync_ExcludeAssembliesWithoutSources_WithMultipleValues_ShouldReturnInvalid()
  {
    // Arrange
    var commandOption = new CommandLineOption(CoverletOptionNames.ExcludeAssembliesWithoutSources, "Exclude assemblies without sources option description", ArgumentArity.ZeroOrOne, false);
    string[] arguments = ["MissingAll", "None"];

    // Act
    ValidationResult result = await _provider.ValidateOptionArgumentsAsync(commandOption, arguments);

    // Assert
    Assert.False(result.IsValid);
    Assert.Contains("Only one value is allowed", result.ErrorMessage);
  }
}

