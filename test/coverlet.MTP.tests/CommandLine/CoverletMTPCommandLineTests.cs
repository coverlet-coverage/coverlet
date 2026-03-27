// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
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
  public async Task IsValidForFilePrefixWithValue()
  {
    CommandLineOption option = _provider.GetCommandLineOptions().First(x => x.Name == CoverletOptionNames.FilePrefix);

    var result = await _provider.ValidateOptionArgumentsAsync(option, ["MyProject"]);

    Assert.True(result.IsValid);
    Assert.True(string.IsNullOrEmpty(result.ErrorMessage));
  }

  [Theory]
  [InlineData("MyProject")]
  [InlineData("My-Project_v1.0")]
  [InlineData("test.unit")]
  [InlineData("ProjectA.Tests")]
  public async Task IsValidForFilePrefixWithSafeValues(string prefix)
  {
    CommandLineOption option = _provider.GetCommandLineOptions().First(x => x.Name == CoverletOptionNames.FilePrefix);

    var result = await _provider.ValidateOptionArgumentsAsync(option, [prefix]);

    Assert.True(result.IsValid);
    Assert.True(string.IsNullOrEmpty(result.ErrorMessage));
  }

  [Theory]
  [InlineData("../malicious", "contains invalid character")]
  [InlineData("..\\malicious", "must not contain directory separators")]
  [InlineData("/absolute/path", "must not be a rooted path")]
  [InlineData("path/to/file", "contains invalid character")]
  [InlineData("path\\to\\file", "must not contain directory separators")]
  [InlineData("..", "must not contain path traversal patterns")]
  [InlineData("..test", "must not contain path traversal patterns")]
  public async Task IsInvalidForFilePrefixWithPathTraversal(string prefix, string expectedErrorPart)
  {
    // expectedErrorPart is different for Linux or MacOS because of the different invalid character (directory separator vs null char)
    Assert.SkipUnless(RuntimeInformation.IsOSPlatform(OSPlatform.Windows), "Test requires Windows");

    CommandLineOption option = _provider.GetCommandLineOptions().First(x => x.Name == CoverletOptionNames.FilePrefix);

    var result = await _provider.ValidateOptionArgumentsAsync(option, [prefix]);

    Assert.False(result.IsValid);
    Assert.Contains(expectedErrorPart, result.ErrorMessage);
  }

  [Theory]
  [InlineData("../malicious", "must not contain directory separators")]
  [InlineData("..\\malicious", "must not contain path traversal patterns")]
  [InlineData("/absolute/path", "must not be a rooted path")]
  [InlineData("path/to/file", "must not contain directory separators")]
  [InlineData("..", "must not contain path traversal patterns")]
  [InlineData("..test", "must not contain path traversal patterns")]
  public async Task IsInvalidForFilePrefixWithPathTraversalLinux(string prefix, string expectedErrorPart)
  {
    // expectedErrorPart is different for Linux or MacOS because of the different invalid character (directory separator vs null char)
    Assert.SkipWhen(RuntimeInformation.IsOSPlatform(OSPlatform.Windows), "Test requires Linux or MacOS");

    CommandLineOption option = _provider.GetCommandLineOptions().First(x => x.Name == CoverletOptionNames.FilePrefix);

    var result = await _provider.ValidateOptionArgumentsAsync(option, [prefix]);

    //Assert.False(result.IsValid);
    Assert.Contains(expectedErrorPart, result.ErrorMessage);
  }

  [Fact]
  public void ValidateFilePrefixReturnsNullForValidPrefix()
  {
    string? result = CoverletExtensionCommandLineProvider.ValidateFilePrefix("MyProject");

    Assert.Null(result);
  }

  [Theory]
  [InlineData("../evil")]
  [InlineData("..\\evil")]
  [InlineData("path/traversal")]
  [InlineData("..")]
  [InlineData("..hidden")]
  public void ValidateFilePrefixReturnsErrorForDangerousPrefix(string prefix)
  {
    string? result = CoverletExtensionCommandLineProvider.ValidateFilePrefix(prefix);

    Assert.NotNull(result);
  }

  [Theory]
  [InlineData("path\\traversal")]

  public void ValidateFilePrefixReturnsErrorForDangerousPrefixWindows(string prefix)
  {
    Assert.SkipUnless(RuntimeInformation.IsOSPlatform(OSPlatform.Windows), "Test requires Windows");

    string? result = CoverletExtensionCommandLineProvider.ValidateFilePrefix(prefix);

    Assert.NotNull(result);
  }

  [Theory]
  [InlineData("test\"file")]
  [InlineData("test>")]
  [InlineData("<test")]
  [InlineData("test|file")]
  [InlineData("test*file")]
  [InlineData("test?file")]
  [InlineData("test:file")]
  [InlineData("test\0file")]
  public void ValidateFilePrefixReturnsErrorForInvalidFilenameCharacters(string prefix)
  {

    Assert.SkipUnless(RuntimeInformation.IsOSPlatform(OSPlatform.Windows), "Test requires Windows");

    string? result = CoverletExtensionCommandLineProvider.ValidateFilePrefix(prefix);

    Assert.NotNull(result);
    Assert.Contains("invalid character", result);
  }

  [Theory]
  [InlineData("")]
  [InlineData("MyProject")]
  [InlineData("My-Project_v1.0")]
  [InlineData("test.unit")]
  [InlineData("123")]
  [InlineData("a")]
  public void ValidateFilePrefixReturnsNullForValidPrefixes(string prefix)
  {
    string? result = CoverletExtensionCommandLineProvider.ValidateFilePrefix(prefix);

    Assert.Null(result);
  }

  [Fact]
  public void ValidateFilePrefixReturnsErrorForDirectorySeparator()
  {
    Assert.SkipUnless(RuntimeInformation.IsOSPlatform(OSPlatform.Windows), "Test requires Windows");

    string? result = CoverletExtensionCommandLineProvider.ValidateFilePrefix("path/file");

    Assert.NotNull(result);
    Assert.Contains("contains invalid character", result);
  }

  [Fact]
  public void ValidateFilePrefixReturnsErrorForDirectorySeparatorLinux()
  {
    Assert.SkipWhen(RuntimeInformation.IsOSPlatform(OSPlatform.Windows), "Test requires Linux or MacOS");
    string? result = CoverletExtensionCommandLineProvider.ValidateFilePrefix("path/file");

    Assert.NotNull(result);
    Assert.Contains("must not contain directory separators", result);
  }

  [Fact]
  public void ValidateFilePrefixReturnsErrorForPathTraversal()
  {
    string? result = CoverletExtensionCommandLineProvider.ValidateFilePrefix("..");

    Assert.NotNull(result);
    Assert.Contains("path traversal", result);
  }

  [Fact]
  public void ValidateFilePrefixReturnsErrorForPathTraversalWithSuffix()
  {
    string? result = CoverletExtensionCommandLineProvider.ValidateFilePrefix("..hidden");

    Assert.NotNull(result);
    Assert.Contains("path traversal", result);
  }

  [Fact]
  public void GetCommandLineOptionsReturnsAllExpectedOptions()
  {
    var options = _provider.GetCommandLineOptions();

    var expectedOptions = new[]
    {
        CoverletOptionNames.Coverage,
        CoverletOptionNames.Formats,
        CoverletOptionNames.FilePrefix,
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
