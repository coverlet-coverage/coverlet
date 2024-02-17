// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Coverlet.Core.Abstractions;
using Coverlet.Core.Helpers;
using Coverlet.Core.Instrumentation;
using Coverlet.Core.Symbols;
using Moq;
using Xunit;

namespace Coverlet.Core.Tests
{
  public partial class CoverageTests
  {
    private readonly Mock<ILogger> _mockLogger = new();
    readonly JsonSerializerOptions _options = new()
    {
      Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
      IncludeFields = true,
      WriteIndented = true,
      Converters =
        {
          new BranchDictionaryConverterFactory()
        }
    };

    [Fact]
    public void TestCoverage()
    {
      string module = GetType().Assembly.Location;
      string pdb = Path.Combine(Path.GetDirectoryName(module), Path.GetFileNameWithoutExtension(module) + ".pdb");

      DirectoryInfo directory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));

      File.Copy(module, Path.Combine(directory.FullName, Path.GetFileName(module)), true);
      File.Copy(pdb, Path.Combine(directory.FullName, Path.GetFileName(pdb)), true);

      // TODO: Find a way to mimick hits
      InstrumentationHelper instrumentationHelper =
          new(new ProcessExitHandler(), new RetryHelper(), new FileSystem(), new Mock<ILogger>().Object,
                                    new SourceRootTranslator(module, new Mock<ILogger>().Object, new FileSystem(), new AssemblyAdapter()));

      CoverageParameters parameters = new()
      {
        IncludeFilters = ["[coverlet.tests.projectsample.excludedbyattribute*]*"],
        IncludeDirectories = [],
        ExcludeFilters = [],
        ExcludedSourceFiles = [],
        ExcludeAttributes = [],
        IncludeTestAssembly = false,
        SingleHit = false,
        MergeWith = string.Empty,
        UseSourceLink = false
      };

      Coverage coverage = new(Path.Combine(directory.FullName, Path.GetFileName(module)), parameters, _mockLogger.Object, instrumentationHelper, new FileSystem(), new SourceRootTranslator(_mockLogger.Object, new FileSystem()), new CecilSymbolHelper());
      coverage.PrepareModules();

      CoverageResult result = coverage.GetCoverageResult();

      Assert.Empty(result.Modules);

      directory.Delete(true);
    }

    [Fact]
    public void TestCoverageWithTestAssembly()
    {
      string module = GetType().Assembly.Location;
      string pdb = Path.Combine(Path.GetDirectoryName(module), Path.GetFileNameWithoutExtension(module) + ".pdb");

      DirectoryInfo directory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));

      File.Copy(module, Path.Combine(directory.FullName, Path.GetFileName(module)), true);
      File.Copy(pdb, Path.Combine(directory.FullName, Path.GetFileName(pdb)), true);

      InstrumentationHelper instrumentationHelper =
          new(new ProcessExitHandler(), new RetryHelper(), new FileSystem(), new Mock<ILogger>().Object,
                                    new SourceRootTranslator(module, new Mock<ILogger>().Object, new FileSystem(), new AssemblyAdapter()));

      CoverageParameters parameters = new()
      {
        IncludeFilters = [],
        IncludeDirectories = [],
        ExcludeFilters = [],
        ExcludedSourceFiles = [],
        ExcludeAttributes = [],
        IncludeTestAssembly = true,
        SingleHit = false,
        MergeWith = string.Empty,
        UseSourceLink = false
      };

      Coverage coverage = new(Path.Combine(directory.FullName, Path.GetFileName(module)), parameters, _mockLogger.Object, instrumentationHelper, new FileSystem(),
                                  new SourceRootTranslator(module, _mockLogger.Object, new FileSystem(), new AssemblyAdapter()), new CecilSymbolHelper());
      coverage.PrepareModules();

      string result = JsonSerializer.Serialize(coverage.GetCoverageResult(), _options);

      Assert.Contains("coverlet.core.tests.dll", result);

      directory.Delete(true);
    }

    [Fact]
    public void TestCoverageMergeWithParameter()
    {
      string module = GetType().Assembly.Location;
      string pdb = Path.Combine(Path.GetDirectoryName(module), Path.GetFileNameWithoutExtension(module) + ".pdb");

      DirectoryInfo directory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));

      File.Copy(module, Path.Combine(directory.FullName, Path.GetFileName(module)), true);
      File.Copy(pdb, Path.Combine(directory.FullName, Path.GetFileName(pdb)), true);

      // TODO: Find a way to mimick hits
      InstrumentationHelper instrumentationHelper =
          new(new ProcessExitHandler(), new RetryHelper(), new FileSystem(), new Mock<ILogger>().Object,
                                    new SourceRootTranslator(module, new Mock<ILogger>().Object, new FileSystem(), new AssemblyAdapter()));

      CoverageParameters parameters = new()
      {
        IncludeFilters = ["[coverlet.tests.projectsample.excludedbyattribute*]*"],
        IncludeDirectories = [],
        ExcludeFilters = [],
        ExcludedSourceFiles = [],
        ExcludeAttributes = [],
        IncludeTestAssembly = false,
        SingleHit = false,
        MergeWith = Directory.GetFiles(Path.Combine(Directory.GetCurrentDirectory(), "TestAssets"), "MergeWith.coverage.json").First(),
        UseSourceLink = false
      };

      Coverage coverage = new(Path.Combine(directory.FullName, Path.GetFileName(module)), parameters, _mockLogger.Object, instrumentationHelper, new FileSystem(), new SourceRootTranslator(_mockLogger.Object, new FileSystem()), new CecilSymbolHelper());
      coverage.PrepareModules();

      string result = JsonSerializer.Serialize(coverage.GetCoverageResult(), _options);

      Assert.Contains("DeepThought.cs", result);

      _mockLogger.Verify(l => l.LogInformation(It.Is<string>(v => v.StartsWith("MergeWith: '") && v.EndsWith("MergeWith.coverage.json'.")), It.IsAny<bool>()), Times.Once);

      directory.Delete(true);
    }

    [Fact]
    public void TestCoverageMergeWithWrongParameter()
    {
      string module = GetType().Assembly.Location;
      string pdb = Path.Combine(Path.GetDirectoryName(module), Path.GetFileNameWithoutExtension(module) + ".pdb");

      DirectoryInfo directory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));

      File.Copy(module, Path.Combine(directory.FullName, Path.GetFileName(module)), true);
      File.Copy(pdb, Path.Combine(directory.FullName, Path.GetFileName(pdb)), true);

      // TODO: Find a way to mimick hits
      InstrumentationHelper instrumentationHelper =
          new(new ProcessExitHandler(), new RetryHelper(), new FileSystem(), new Mock<ILogger>().Object,
                                    new SourceRootTranslator(module, new Mock<ILogger>().Object, new FileSystem(), new AssemblyAdapter()));

      CoverageParameters parameters = new()
      {
        IncludeFilters = ["[coverlet.tests.projectsample.excludedbyattribute*]*"],
        IncludeDirectories = [],
        ExcludeFilters = [],
        ExcludedSourceFiles = [],
        ExcludeAttributes = [],
        IncludeTestAssembly = false,
        SingleHit = false,
        MergeWith = "FileDoesNotExist.json",
        UseSourceLink = false
      };

      Coverage coverage = new(Path.Combine(directory.FullName, Path.GetFileName(module)), parameters, _mockLogger.Object, instrumentationHelper, new FileSystem(), new SourceRootTranslator(_mockLogger.Object, new FileSystem()), new CecilSymbolHelper());
      coverage.PrepareModules();

      string result = JsonSerializer.Serialize(coverage.GetCoverageResult(), _options);

      _mockLogger.Verify(l => l.LogInformation(It.Is<string>(v => v.Equals("MergeWith: file 'FileDoesNotExist.json' does not exist.")), It.IsAny<bool>()), Times.Once);

      directory.Delete(true);
    }
  }
}
public class BranchDictionaryConverterFactory : JsonConverterFactory
{
  public override bool CanConvert(Type typeToConvert)
  {
    return typeof(Dictionary<BranchKey, Branch>).IsAssignableFrom(typeToConvert);
  }

  public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
  {
    Type[] genericArgs = typeToConvert.GetGenericArguments();
    Type keyType = genericArgs[0];
    Type valueType = genericArgs[1];

    JsonConverter converter = (JsonConverter)Activator.CreateInstance(
        typeof(BranchDictionaryConverter<,>).MakeGenericType([keyType, valueType]));

    return converter;
  }
}

public class BranchDictionaryConverter<TKey, TValue> : JsonConverter<Dictionary<TKey, TValue>>
{
  public override Dictionary<TKey, TValue> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
  {
    throw new NotImplementedException();
  }

  public override void Write(Utf8JsonWriter writer, Dictionary<TKey, TValue> value, JsonSerializerOptions options)
  {
    writer.WriteStartObject();

    foreach (KeyValuePair<TKey, TValue> pair in value)
    {
      writer.WritePropertyName(pair.Key.ToString());
      JsonSerializer.Serialize(writer, pair.Value, options);
    }

    writer.WriteEndObject();
  }
}
