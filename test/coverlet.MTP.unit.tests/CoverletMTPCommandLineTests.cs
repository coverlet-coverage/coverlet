// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using coverlet.Extension;
using Coverlet.MTP.CommandLine;
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
    [InlineData(CoverletOptionNames.Formats, "invalid", "The value 'invalid' is not a valid option for 'coverage-output-format'.")]
    [InlineData(CoverletOptionNames.ExcludeAssembliesWithoutSources, "invalid", "The value 'invalid' is not a valid option for 'coverage-exclude-assemblies-without-sources'.")]
    [InlineData(CoverletOptionNames.ExcludeAssembliesWithoutSources, "", "At least one value must be specified for 'coverage-exclude-assemblies-without-sources'.")]
    public async Task IsInvalid_When_Option_Has_InvalidValue(string optionName, string value, string expectedError)
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
    public async Task IsValid_When_Option_Has_ValidValue(string optionName, string value)
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
    //[InlineData(CoverletOptionNames.SourceMappingFile)]
    public async Task IsValid_For_NonValidated_Options(string optionName)
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
                CoverletOptionNames.Coverage,
                CoverletOptionNames.Formats,
                CoverletOptionNames.Exclude,
                CoverletOptionNames.Include,
                CoverletOptionNames.ExcludeByFile,
                CoverletOptionNames.IncludeDirectory,
                CoverletOptionNames.ExcludeByAttribute,
                CoverletOptionNames.IncludeTestAssembly,
                CoverletOptionNames.SingleHit,
                CoverletOptionNames.SkipAutoProps,
                CoverletOptionNames.DoesNotReturnAttribute,
                CoverletOptionNames.ExcludeAssembliesWithoutSources
                //CoverletOptionNames.SourceMappingFile
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
