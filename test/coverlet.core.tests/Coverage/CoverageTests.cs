// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Coverlet.Core.Abstractions;
using Coverlet.Core.Helpers;
using Coverlet.Core.Instrumentation;
using Coverlet.Core.Symbols;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace Coverlet.Core.Tests
{
  public partial class CoverageTests
  {
    private readonly Mock<ILogger> _mockLogger = new();

    [Fact]
    public void TestCoverage()
    {
      string module = GetType().Assembly.Location;
      string pdb = Path.Combine(Path.GetDirectoryName(module), Path.GetFileNameWithoutExtension(module) + ".pdb");

      DirectoryInfo directory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));

      File.Copy(module, Path.Combine(directory.FullName, Path.GetFileName(module)), true);
      File.Copy(pdb, Path.Combine(directory.FullName, Path.GetFileName(pdb)), true);

      // TODO: Find a way to mimick hits
      var instrumentationHelper =
          new InstrumentationHelper(new ProcessExitHandler(), new RetryHelper(), new FileSystem(), new Mock<ILogger>().Object,
                                    new SourceRootTranslator(module, new Mock<ILogger>().Object, new FileSystem(), new AssemblyAdapter()));

      var parameters = new CoverageParameters
      {
        IncludeFilters = new string[] { "[coverlet.tests.projectsample.excludedbyattribute*]*" },
        IncludeDirectories = Array.Empty<string>(),
        ExcludeFilters = Array.Empty<string>(),
        ExcludedSourceFiles = Array.Empty<string>(),
        ExcludeAttributes = Array.Empty<string>(),
        IncludeTestAssembly = false,
        SingleHit = false,
        MergeWith = string.Empty,
        UseSourceLink = false
      };

      var coverage = new Coverage(Path.Combine(directory.FullName, Path.GetFileName(module)), parameters, _mockLogger.Object, instrumentationHelper, new FileSystem(), new SourceRootTranslator(_mockLogger.Object, new FileSystem()), new CecilSymbolHelper());
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

      var instrumentationHelper =
          new InstrumentationHelper(new ProcessExitHandler(), new RetryHelper(), new FileSystem(), new Mock<ILogger>().Object,
                                    new SourceRootTranslator(module, new Mock<ILogger>().Object, new FileSystem(), new AssemblyAdapter()));

      var parameters = new CoverageParameters
      {
        IncludeFilters = Array.Empty<string>(),
        IncludeDirectories = Array.Empty<string>(),
        ExcludeFilters = Array.Empty<string>(),
        ExcludedSourceFiles = Array.Empty<string>(),
        ExcludeAttributes = Array.Empty<string>(),
        IncludeTestAssembly = true,
        SingleHit = false,
        MergeWith = string.Empty,
        UseSourceLink = false
      };

      var coverage = new Coverage(Path.Combine(directory.FullName, Path.GetFileName(module)), parameters, _mockLogger.Object, instrumentationHelper, new FileSystem(),
                                  new SourceRootTranslator(module, _mockLogger.Object, new FileSystem(), new AssemblyAdapter()), new CecilSymbolHelper());
      coverage.PrepareModules();

      string result = JsonConvert.SerializeObject(coverage.GetCoverageResult(), Formatting.Indented, new BranchDictionaryConverter());

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
      var instrumentationHelper =
          new InstrumentationHelper(new ProcessExitHandler(), new RetryHelper(), new FileSystem(), new Mock<ILogger>().Object,
                                    new SourceRootTranslator(module, new Mock<ILogger>().Object, new FileSystem(), new AssemblyAdapter()));

      var parameters = new CoverageParameters
      {
        IncludeFilters = new string[] { "[coverlet.tests.projectsample.excludedbyattribute*]*" },
        IncludeDirectories = Array.Empty<string>(),
        ExcludeFilters = Array.Empty<string>(),
        ExcludedSourceFiles = Array.Empty<string>(),
        ExcludeAttributes = Array.Empty<string>(),
        IncludeTestAssembly = false,
        SingleHit = false,
        MergeWith = Directory.GetFiles(Path.Combine(Directory.GetCurrentDirectory(), "TestAssets"), "MergeWith.coverage.json").First(),
        UseSourceLink = false
      };

      var coverage = new Coverage(Path.Combine(directory.FullName, Path.GetFileName(module)), parameters, _mockLogger.Object, instrumentationHelper, new FileSystem(), new SourceRootTranslator(_mockLogger.Object, new FileSystem()), new CecilSymbolHelper());
      coverage.PrepareModules();

      string result = JsonConvert.SerializeObject(coverage.GetCoverageResult(), Formatting.Indented, new BranchDictionaryConverter());

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
      var instrumentationHelper =
          new InstrumentationHelper(new ProcessExitHandler(), new RetryHelper(), new FileSystem(), new Mock<ILogger>().Object,
                                    new SourceRootTranslator(module, new Mock<ILogger>().Object, new FileSystem(), new AssemblyAdapter()));

      var parameters = new CoverageParameters
      {
        IncludeFilters = new string[] { "[coverlet.tests.projectsample.excludedbyattribute*]*" },
        IncludeDirectories = Array.Empty<string>(),
        ExcludeFilters = Array.Empty<string>(),
        ExcludedSourceFiles = Array.Empty<string>(),
        ExcludeAttributes = Array.Empty<string>(),
        IncludeTestAssembly = false,
        SingleHit = false,
        MergeWith = "FileDoesNotExist.json",
        UseSourceLink = false
      };

      var coverage = new Coverage(Path.Combine(directory.FullName, Path.GetFileName(module)), parameters, _mockLogger.Object, instrumentationHelper, new FileSystem(), new SourceRootTranslator(_mockLogger.Object, new FileSystem()), new CecilSymbolHelper());
      coverage.PrepareModules();

      JsonConvert.SerializeObject(coverage.GetCoverageResult());

      _mockLogger.Verify(l => l.LogInformation(It.Is<string>(v => v.Equals("MergeWith: file 'FileDoesNotExist.json' does not exist.")), It.IsAny<bool>()), Times.Once);

      directory.Delete(true);
    }

    [Fact]
    public void TestCoverageUnloadWithParameters()
    {
      string module = GetType().Assembly.Location;
      string pdb = Path.Combine(Path.GetDirectoryName(module), Path.GetFileNameWithoutExtension(module) + ".pdb");

      DirectoryInfo directory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));

      File.Copy(module, Path.Combine(directory.FullName, Path.GetFileName(module)), true);
      File.Copy(pdb, Path.Combine(directory.FullName, Path.GetFileName(pdb)), true);

      var mockInstrumentationHelper = new Mock<IInstrumentationHelper>();
      mockInstrumentationHelper.Setup(x => x.RestoreOriginalModule(It.IsAny<string>(), It.IsAny<string>()));

      var parameters = new CoverageParameters
      {
        IncludeFilters = new string[] { "[coverlet.tests.projectsample.excludedbyattribute*]*" },
        IncludeDirectories = Array.Empty<string>(),
        ExcludeFilters = Array.Empty<string>(),
        ExcludedSourceFiles = Array.Empty<string>(),
        ExcludeAttributes = Array.Empty<string>(),
        IncludeTestAssembly = false,
        SingleHit = false,
        MergeWith = string.Empty,
        UseSourceLink = false
      };

      var coverage = new Coverage(Path.Combine(directory.FullName, Path.GetFileName(module)), parameters, _mockLogger.Object, mockInstrumentationHelper.Object, new FileSystem(), new SourceRootTranslator(_mockLogger.Object, new FileSystem()), new CecilSymbolHelper());
      coverage.PrepareModules();
      coverage.UnloadModule(Path.Combine(directory.FullName, Path.GetFileName(module)));

      mockInstrumentationHelper.Verify(i => i.RestoreOriginalModule(It.Is<string>(v => v.Equals(Path.Combine(directory.FullName, Path.GetFileName(module)))), It.IsAny<string>()), Times.Once);
      _mockLogger.Verify(l => l.LogVerbose(It.Is<string>(v => v.Equals($"Module at {Path.Combine(directory.FullName, Path.GetFileName(module))} is unloaded."))), Times.Once);
    }

    [Fact]
    public void TestCoverageUnloadWithNoParameters()
    {
      string module = GetType().Assembly.Location;
      string pdb = Path.Combine(Path.GetDirectoryName(module), Path.GetFileNameWithoutExtension(module) + ".pdb");

      DirectoryInfo directory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));

      File.Copy(module, Path.Combine(directory.FullName, Path.GetFileName(module)), true);
      File.Copy(pdb, Path.Combine(directory.FullName, Path.GetFileName(pdb)), true);

      var mockInstrumentationHelper = new Mock<IInstrumentationHelper>();
      mockInstrumentationHelper
        .Setup(x => x.SelectModules(It.IsAny<IEnumerable<string>>(), It.IsAny<string[]>(), It.IsAny<string[]>()))
        .Returns(new List<string>(){"ModuleX"});
      mockInstrumentationHelper.Setup(x => x.RestoreOriginalModule(It.IsAny<string>(), It.IsAny<string>()));

      var parameters = new CoverageParameters
      {
        IncludeFilters = new string[] { "[coverlet.tests.projectsample.excludedbyattribute*]*" },
        IncludeDirectories = Array.Empty<string>(),
        ExcludeFilters = Array.Empty<string>(),
        ExcludedSourceFiles = Array.Empty<string>(),
        ExcludeAttributes = Array.Empty<string>(),
        IncludeTestAssembly = false,
        SingleHit = false,
        MergeWith = string.Empty,
        UseSourceLink = false
      };

      var coverage = new Coverage(Path.Combine(directory.FullName, Path.GetFileName(module)), parameters, _mockLogger.Object, mockInstrumentationHelper.Object, new FileSystem(), new SourceRootTranslator(_mockLogger.Object, new FileSystem()), new CecilSymbolHelper());
      coverage.PrepareModules();
      coverage.UnloadModules();

      mockInstrumentationHelper.Verify(i => i.RestoreOriginalModule(It.Is<string>(v => v.Equals("ModuleX")), It.IsAny<string>()), Times.Once);
      _mockLogger.Verify(l => l.LogVerbose(It.Is<string>(v => v.Equals("All Modules unloaded."))), Times.Once);
    }
  }
}

public class BranchDictionaryConverter: JsonConverter
{
  public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
  {
    Type type = value.GetType();
    var keys = (IEnumerable)type.GetProperty("Keys")?.GetValue(value, null);
    var values = (IEnumerable)type.GetProperty("Values")?.GetValue(value, null);
    IEnumerator valueEnumerator = values.GetEnumerator();

    writer.WriteStartArray();
    foreach (object key in keys)
    {
      valueEnumerator.MoveNext();

      writer.WriteStartArray();
      serializer.Serialize(writer, key);
      serializer.Serialize(writer, valueEnumerator.Current);
      writer.WriteEndArray();
    }
    writer.WriteEndArray();
  }

  public override object ReadJson(JsonReader reader, Type objectType, object existingValue, Newtonsoft.Json.JsonSerializer serializer)
  {
    throw new NotImplementedException();
  }

  public override bool CanConvert(Type objectType)
  {
    return typeof(Dictionary<BranchKey, Branch>).IsAssignableFrom(objectType);
  }
}
