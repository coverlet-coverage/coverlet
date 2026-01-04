// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Coverlet.MTP.Configuration;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace coverlet.MTP.Tests.Configuration;

public class CoverletMTPSettingsParserTests
{
  private readonly CoverletMTPSettingsParser _parser = new();

  [Fact]
  public void Parse_WithNullConfiguration_ReturnsDefaultSettings()
  {
    // Act
    CoverletMTPSettings settings = _parser.Parse(null, "test.dll");

    // Assert
    Assert.Equal("test.dll", settings.TestModule);
    Assert.Equal(["cobertura"], settings.ReportFormats);
    Assert.Equal(["[coverlet.*]*"], settings.ExcludeFilters);
    Assert.Empty(settings.IncludeFilters);
    Assert.False(settings.UseSourceLink);
    Assert.False(settings.SingleHit);
  }

  [Fact]
  public void Parse_WithValidConfiguration_ReturnsCorrectSettings()
  {
    // Arrange
    IConfiguration configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
          ["Coverlet:Include"] = "[MyApp.*]*,[MyLib.*]*",
          ["Coverlet:Exclude"] = "[*.Tests]*",
          ["Coverlet:ExcludeByAttribute"] = "GeneratedCode,ExcludeFromCodeCoverage",
          ["Coverlet:Format"] = "cobertura,json",
          ["Coverlet:UseSourceLink"] = "true",
          ["Coverlet:SingleHit"] = "true",
          ["Coverlet:IncludeTestAssembly"] = "true",
          ["Coverlet:SkipAutoProps"] = "true",
          ["Coverlet:DeterministicReport"] = "true"
        })
        .Build();

    // Act
    CoverletMTPSettings settings = _parser.Parse(configuration, "test.dll");

    // Assert
    Assert.Equal("test.dll", settings.TestModule);
    Assert.Equal(["[MyApp.*]*", "[MyLib.*]*"], settings.IncludeFilters);
    Assert.Equal(["[coverlet.*]*", "[*.Tests]*"], settings.ExcludeFilters); // Default + configured
    Assert.Equal(["GeneratedCode", "ExcludeFromCodeCoverage"], settings.ExcludeAttributes);
    Assert.Equal(["cobertura", "json"], settings.ReportFormats);
    Assert.True(settings.UseSourceLink);
    Assert.True(settings.SingleHit);
    Assert.True(settings.IncludeTestAssembly);
    Assert.True(settings.SkipAutoProps);
    Assert.True(settings.DeterministicReport);
  }

  [Theory]
  [InlineData("[*]*,[coverlet]*", new[] { "[*]*", "[coverlet]*" })]
  [InlineData("[*]*, [coverlet]*", new[] { "[*]*", "[coverlet]*" })]
  [InlineData("[*]*,\t[coverlet]*", new[] { "[*]*", "[coverlet]*" })]
  [InlineData("[*]*, \r\n [coverlet]*", new[] { "[*]*", "[coverlet]*" })]
  [InlineData(" [*]* , [coverlet]* ", new[] { "[*]*", "[coverlet]*" })]
  [InlineData("[*]*,,[coverlet]*", new[] { "[*]*", "[coverlet]*" })]
  [InlineData("[*]*, ,[coverlet]*", new[] { "[*]*", "[coverlet]*" })]
  public void Parse_WithVariousDelimiters_ParsesCorrectly(string includeValue, string[] expected)
  {
    // Arrange
    IConfiguration configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
          ["Coverlet:Include"] = includeValue
        })
        .Build();

    // Act
    CoverletMTPSettings settings = _parser.Parse(configuration, "test.dll");

    // Assert
    Assert.Equal(expected, settings.IncludeFilters);
  }

  [Theory]
  [InlineData("json,cobertura", 2, new[] { "json", "cobertura" })]
  [InlineData("json, cobertura", 2, new[] { "json", "cobertura" })]
  [InlineData(" , json,, cobertura ", 2, new[] { "json", "cobertura" })]
  [InlineData("opencover", 1, new[] { "opencover" })]
  public void Parse_WithMultipleFormats_ParsesCorrectly(string formats, int count, string[] expected)
  {
    // Arrange
    IConfiguration configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
          ["Coverlet:Format"] = formats
        })
        .Build();

    // Act
    CoverletMTPSettings settings = _parser.Parse(configuration, "test.dll");

    // Assert
    Assert.Equal(expected, settings.ReportFormats);
    Assert.Equal(count, settings.ReportFormats.Length);
  }

  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("   ")]
  public void Parse_WithEmptyOrNullFormat_ReturnsDefaultFormat(string? formatValue)
  {
    // Arrange
    IConfiguration configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
          ["Coverlet:Format"] = formatValue
        })
        .Build();

    // Act
    CoverletMTPSettings settings = _parser.Parse(configuration, "test.dll");

    // Assert
    Assert.Single(settings.ReportFormats);
    Assert.Equal("cobertura", settings.ReportFormats[0]);
  }

  [Theory]
  [InlineData("true", true)]
  [InlineData("True", true)]
  [InlineData("TRUE", true)]
  [InlineData("false", false)]
  [InlineData("False", false)]
  [InlineData("", false)]
  [InlineData("invalid", false)]
  public void Parse_BooleanValues_ParsesCorrectly(string value, bool expected)
  {
    // Arrange
    IConfiguration configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
          ["Coverlet:UseSourceLink"] = value
        })
        .Build();

    // Act
    CoverletMTPSettings settings = _parser.Parse(configuration, "test.dll");

    // Assert
    Assert.Equal(expected, settings.UseSourceLink);
  }

  [Fact]
  public void Parse_ExcludeFilters_AlwaysIncludesDefault()
  {
    // Arrange
    IConfiguration configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
          ["Coverlet:Exclude"] = "[MyApp.Tests]*"
        })
        .Build();

    // Act
    CoverletMTPSettings settings = _parser.Parse(configuration, "test.dll");

    // Assert
    Assert.Contains("[coverlet.*]*", settings.ExcludeFilters);
    Assert.Contains("[MyApp.Tests]*", settings.ExcludeFilters);
    Assert.Equal("[coverlet.*]*", settings.ExcludeFilters[0]); // Default is first
  }

  [Fact]
  public void Parse_FromJsonFile_LoadsCorrectly()
  {
    // Arrange
    IConfiguration configuration = new ConfigurationBuilder()
        .AddJsonFile("TestAssets/coverlet.mtp.appsettings.json")
        .Build();

    // Act
    CoverletMTPSettings settings = _parser.Parse(configuration, "test.dll");

    // Assert
    Assert.Equal("[MyApp.*]*", settings.IncludeFilters[0]);
    Assert.Contains("[*.Tests]*", settings.ExcludeFilters);
    Assert.Equal(["cobertura", "json"], settings.ReportFormats);
    Assert.True(settings.SkipAutoProps);
  }

  [Fact]
  public void Parse_ExcludeAssembliesWithoutSources_DefaultsToMissingAll()
  {
    // Arrange
    IConfiguration configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>())
        .Build();

    // Act
    CoverletMTPSettings settings = _parser.Parse(configuration, "test.dll");

    // Assert
    Assert.Equal("MissingAll", settings.ExcludeAssembliesWithoutSources);
  }

  [Theory]
  [InlineData("MissingAll")]
  [InlineData("MissingAny")]
  [InlineData("None")]
  public void Parse_ExcludeAssembliesWithoutSources_AcceptsValidValues(string value)
  {
    // Arrange
    IConfiguration configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
          ["Coverlet:ExcludeAssembliesWithoutSources"] = value
        })
        .Build();

    // Act
    CoverletMTPSettings settings = _parser.Parse(configuration, "test.dll");

    // Assert
    Assert.Equal(value, settings.ExcludeAssembliesWithoutSources);
  }
}
