// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Testing.Platform.Extensions.CommandLine;
using Xunit;

namespace Coverlet.MTP.CommandLine.Tests;

public class CoverletMTPCommandLineTests
{
  private readonly CoverletExtension _extension = new();
  private readonly CoverletExtensionCommandLineProvider _provider;

  public CoverletMTPCommandLineTests()
  {
    _provider = new CoverletExtensionCommandLineProvider(_extension);
  }

  [Theory]
  [InlineData(CoverletOptionNames.Formats, "invalid", "The value 'invalid' is not a valid option for 'coverlet-output-format'.")]
  [InlineData(CoverletOptionNames.ExcludeAssembliesWithoutSources, "invalid", "The value 'invalid' is not a valid option for 'coverlet-exclude-assemblies-without-sources'.")]
  [InlineData(CoverletOptionNames.ExcludeAssembliesWithoutSources, "", "At least one value must be specified for 'coverlet-exclude-assemblies-without-sources'.")]
  public async Task IsInvalidWhenOptionHasInvalidValue(string optionName, string value, string expectedError)
  {
    CommandLineOption option = _provider.GetCommandLineOptions().First(x => x.Name == optionName);
    var arguments = string.IsNullOrEmpty(value) ? Array.Empty<string>() : [value];

    var result = await _provider.ValidateOptionArgumentsAsync(option, arguments);

    Assert.False(result.IsValid);
    Assert.Equal(expectedError, result.ErrorMessage);
  }

  [Theory]
  [InlineData(CoverletOptionNames.Formats, "json")]
  [InlineData(CoverletOptionNames.Formats, "lcov")]
  [InlineData(CoverletOptionNames.Formats, "opencover")]
  [InlineData(CoverletOptionNames.Formats, "cobertura")]
  [InlineData(CoverletOptionNames.Formats, "teamcity")]
  [InlineData(CoverletOptionNames.ExcludeAssembliesWithoutSources, "MissingAll")]
  [InlineData(CoverletOptionNames.ExcludeAssembliesWithoutSources, "MissingAny")]
  [InlineData(CoverletOptionNames.ExcludeAssembliesWithoutSources, "None")]
  public async Task IsValidWhenOptionHasValidValue(string optionName, string value)
  {
    CommandLineOption option = _provider.GetCommandLineOptions().First(x => x.Name == optionName);

    var result = await _provider.ValidateOptionArgumentsAsync(option, [value]);

    Assert.True(result.IsValid);
    Assert.True(string.IsNullOrEmpty(result.ErrorMessage));
  }

  [Theory]
  [InlineData(CoverletOptionNames.Exclude)]
  [InlineData(CoverletOptionNames.Include)]
  [InlineData(CoverletOptionNames.ExcludeByFile)]
  [InlineData(CoverletOptionNames.IncludeDirectory)]
  [InlineData(CoverletOptionNames.ExcludeByAttribute)]
  [InlineData(CoverletOptionNames.DoesNotReturnAttribute)]
  public async Task IsValidForNonValidatedOptions(string optionName)
  {
    CommandLineOption option = _provider.GetCommandLineOptions().First(x => x.Name == optionName);

    var result = await _provider.ValidateOptionArgumentsAsync(option, ["any-value"]);

    Assert.True(result.IsValid);
    Assert.True(string.IsNullOrEmpty(result.ErrorMessage));
  }

  [Theory]
  [InlineData(CoverletOptionNames.IncludeTestAssembly)]
  [InlineData(CoverletOptionNames.SingleHit)]
  [InlineData(CoverletOptionNames.SkipAutoProps)]
  public async Task IsValidForFlagOptions(string optionName)
  {
    CommandLineOption option = _provider.GetCommandLineOptions().First(x => x.Name == optionName);

    var result = await _provider.ValidateOptionArgumentsAsync(option, []);

    Assert.True(result.IsValid);
    Assert.True(string.IsNullOrEmpty(result.ErrorMessage));
  }

  [Fact]
  public void GetCommandLineOptionsReturnsAllExpectedOptions()
  {
    var options = _provider.GetCommandLineOptions();

    var expectedOptions = new[]
    {
        CoverletOptionNames.Coverage,
        CoverletOptionNames.Formats,
        CoverletOptionNames.Include,
        CoverletOptionNames.IncludeDirectory,
        CoverletOptionNames.Exclude,
        CoverletOptionNames.ExcludeByFile,
        CoverletOptionNames.ExcludeByAttribute,
        CoverletOptionNames.IncludeTestAssembly,
        CoverletOptionNames.SingleHit,
        CoverletOptionNames.SkipAutoProps,
        CoverletOptionNames.DoesNotReturnAttribute,
        CoverletOptionNames.ExcludeAssembliesWithoutSources,
     };

    Assert.Equal(expectedOptions.Length, options.Count);
    Assert.All(expectedOptions, name => Assert.Contains(options, o => o.Name == name));
  }

  [Fact]
  public async Task ValidateCommandLineOptionsIsAlwaysValid()
  {
    var validateOptionsResult = await _provider.ValidateCommandLineOptionsAsync(new TestCommandLineOptions([]));
    Assert.True(validateOptionsResult.IsValid);
    Assert.True(string.IsNullOrEmpty(validateOptionsResult.ErrorMessage));
  }

  internal sealed class TestCommandLineOptions : Microsoft.Testing.Platform.CommandLine.ICommandLineOptions
  {
    private readonly Dictionary<string, string[]> _options;

    public TestCommandLineOptions(Dictionary<string, string[]> options) => _options = options;

    public bool IsOptionSet(string optionName) => _options.ContainsKey(optionName);

    public bool TryGetOptionArgumentList(string optionName, [NotNullWhen(true)] out string[]? arguments) => _options.TryGetValue(optionName, out arguments);
  }
}
