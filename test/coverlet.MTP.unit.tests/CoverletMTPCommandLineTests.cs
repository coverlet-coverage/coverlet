// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using coverlet.Extension;
using Microsoft.Testing.Platform.Extensions.CommandLine;
using Xunit;

namespace coverlet.MTP.unit.tests
{
  public class CoverletMTPCommandLineTests
  {
    private readonly CoverletExtension _extension = new();
    private readonly CoverletExtensionCommandLineProvider _provider;

    public CoverletMTPCommandLineTests()
    {
      _provider = new CoverletExtensionCommandLineProvider(_extension);
    }

    [Theory]
    [InlineData("formats", "invalid", "The value 'invalid' is not a valid option for 'formats'.")]
    [InlineData("exclude-assemblies-without-sources", "invalid", "The value 'invalid' is not a valid option for 'exclude-assemblies-without-sources'.")]
    [InlineData("exclude-assemblies-without-sources", "", "At least one value must be specified for 'exclude-assemblies-without-sources'.")]
    public async Task IsInvalid_When_Option_Has_InvalidValue(string optionName, string value, string expectedError)
    {
      CommandLineOption option = _provider.GetCommandLineOptions().First(x => x.Name == optionName);
      var arguments = string.IsNullOrEmpty(value) ? Array.Empty<string>() : [value];

      var result = await _provider.ValidateOptionArgumentsAsync(option, arguments);

      Assert.False(result.IsValid);
      Assert.Equal(expectedError, result.ErrorMessage);
    }

    [Theory]
    [InlineData("formats", "json")]
    [InlineData("formats", "lcov")]
    [InlineData("formats", "opencover")]
    [InlineData("formats", "cobertura")]
    [InlineData("formats", "teamcity")]
    [InlineData("exclude-assemblies-without-sources", "MissingAll")]
    [InlineData("exclude-assemblies-without-sources", "MissingAny")]
    [InlineData("exclude-assemblies-without-sources", "None")]
    public async Task IsValid_When_Option_Has_ValidValue(string optionName, string value)
    {
      CommandLineOption option = _provider.GetCommandLineOptions().First(x => x.Name == optionName);

      var result = await _provider.ValidateOptionArgumentsAsync(option, [value]);

      Assert.True(result.IsValid);
      Assert.True(string.IsNullOrEmpty(result.ErrorMessage));
    }

    [Theory]
    [InlineData("exclude")]
    [InlineData("include")]
    [InlineData("exclude-by-file")]
    [InlineData("include-directory")]
    [InlineData("exclude-by-attribute")]
    [InlineData("does-not-return-attribute")]
    [InlineData("source-mapping-file")]
    public async Task IsValid_For_NonValidated_Options(string optionName)
    {
      CommandLineOption option = _provider.GetCommandLineOptions().First(x => x.Name == optionName);

      var result = await _provider.ValidateOptionArgumentsAsync(option, ["any-value"]);

      Assert.True(result.IsValid);
      Assert.True(string.IsNullOrEmpty(result.ErrorMessage));
    }

    [Theory]
    [InlineData("include-test-assembly")]
    [InlineData("single-hit")]
    [InlineData("skipautoprops")]
    public async Task IsValid_For_FlagOptions(string optionName)
    {
      CommandLineOption option = _provider.GetCommandLineOptions().First(x => x.Name == optionName);

      var result = await _provider.ValidateOptionArgumentsAsync(option, []);

      Assert.True(result.IsValid);
      Assert.True(string.IsNullOrEmpty(result.ErrorMessage));
    }

    [Fact]
    public void GetCommandLineOptions_Returns_AllExpectedOptions()
    {
      var options = _provider.GetCommandLineOptions();

      var expectedOptions = new[]
      {
                "formats",
                "exclude",
                "include",
                "exclude-by-file",
                "include-directory",
                "exclude-by-attribute",
                "include-test-assembly",
                "single-hit",
                "skipautoprops",
                "does-not-return-attribute",
                "exclude-assemblies-without-sources",
                "source-mapping-file"
            };

      Assert.Equal(expectedOptions.Length, options.Count);
      Assert.All(expectedOptions, name => Assert.Contains(options, o => o.Name == name));
    }

    [Fact]
    public async Task ValidateCommandLineOptions_IsAlwaysValid()
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
}
