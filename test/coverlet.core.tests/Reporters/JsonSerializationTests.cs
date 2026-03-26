// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.Json;
using Coverlet.Core.Abstractions;
using Coverlet.Core.Reporters;
using Moq;
using Xunit;

namespace Coverlet.Core.Tests.Reporters
{
  /// <summary>
  /// Tests for JSON serialization consistency.
  /// These tests validate that coverage data with special characters (like async state machine class names)
  /// serialize and deserialize correctly, addressing Issue #1843.
  /// </summary>
  public class JsonSerializationTests
  {
    /// <summary>
    /// Issue #1843: Verify that async state machine class names containing angle brackets
    /// are serialized correctly and can be read back.
    /// </summary>
    [Fact]
    public void JsonReporter_AsyncStateMachineClassName_SerializesCorrectly()
    {
      // Arrange - Create coverage result with async state machine class name
      var result = new CoverageResult
      {
        Identifier = Guid.NewGuid().ToString()
      };

      var lines = new Lines { { 1, 1 }, { 2, 0 } };
      var methods = new Methods();

      // Async method signature as it appears in coverage data
      string asyncMethodSignature = "System.Threading.Tasks.Task`1<System.Int32> TestNamespace.TestClass/<AsyncMethod>d__0::MoveNext()";
      methods.Add(asyncMethodSignature, new Method { Lines = lines });

      // Async state machine class name with angle brackets
      string asyncStateMachineClass = "TestNamespace.TestClass/<AsyncMethod>d__0";

      var classes = new Classes { { asyncStateMachineClass, methods } };
      var documents = new Documents { { "TestFile.cs", classes } };
      result.Modules = new Modules { { "TestModule.dll", documents } };

      var reporter = new JsonReporter();
      var mockSourceRootTranslator = new Mock<ISourceRootTranslator>();

      // Act
      string jsonOutput = reporter.Report(result, mockSourceRootTranslator.Object);

      // Assert - Verify the JSON contains the async class name
      Assert.Contains("TestNamespace.TestClass/<AsyncMethod>d__0", jsonOutput);
      Assert.Contains("MoveNext()", jsonOutput);

      // Verify the JSON is valid and can be deserialized
      var options = new JsonSerializerOptions
      {
        PropertyNameCaseInsensitive = true,
        IncludeFields = true
      };
      var deserializedModules = JsonSerializer.Deserialize<Modules>(jsonOutput, options);

      Assert.NotNull(deserializedModules);
      Assert.True(deserializedModules.ContainsKey("TestModule.dll"));
      Assert.True(deserializedModules["TestModule.dll"].ContainsKey("TestFile.cs"));
      Assert.True(deserializedModules["TestModule.dll"]["TestFile.cs"].ContainsKey(asyncStateMachineClass));
    }

    /// <summary>
    /// Issue #1843: Verify that multiple async classes serialize correctly.
    /// This is the exact scenario that caused empty coverage when both classes had async methods.
    /// </summary>
    [Fact]
    public void JsonReporter_MultipleAsyncClasses_AllSerializeCorrectly()
    {
      // Arrange - Create coverage result with multiple async state machine classes
      var result = new CoverageResult
      {
        Identifier = Guid.NewGuid().ToString()
      };

      // First async class
      var methods1 = new Methods();
      string asyncMethod1 = "System.Threading.Tasks.Task`1<System.Int32> SampleLibrary.StringLengthCalculator/<CalculateLengthAsync>d__0::MoveNext()";
      methods1.Add(asyncMethod1, new Method { Lines = new Lines { { 10, 1 }, { 11, 1 } } });
      string asyncClass1 = "SampleLibrary.StringLengthCalculator/<CalculateLengthAsync>d__0";

      // Second async class
      var methods2 = new Methods();
      string asyncMethod2 = "System.Threading.Tasks.Task`1<System.String> SampleLibrary.IntegerFormatter/<FormatToStringAsync>d__0::MoveNext()";
      methods2.Add(asyncMethod2, new Method { Lines = new Lines { { 20, 1 }, { 21, 1 } } });
      string asyncClass2 = "SampleLibrary.IntegerFormatter/<FormatToStringAsync>d__0";

      var classes = new Classes
      {
        { asyncClass1, methods1 },
        { asyncClass2, methods2 }
      };
      var documents = new Documents { { "AsyncClasses.cs", classes } };
      result.Modules = new Modules { { "SampleLibrary.dll", documents } };

      var reporter = new JsonReporter();

      // Act
      string jsonOutput = reporter.Report(result, new Mock<ISourceRootTranslator>().Object);

      // Assert - Both async classes should be present
      Assert.Contains("StringLengthCalculator/<CalculateLengthAsync>d__0", jsonOutput);
      Assert.Contains("IntegerFormatter/<FormatToStringAsync>d__0", jsonOutput);

      // Verify deserialization works
      var options = new JsonSerializerOptions
      {
        PropertyNameCaseInsensitive = true,
        IncludeFields = true
      };
      var deserializedModules = JsonSerializer.Deserialize<Modules>(jsonOutput, options);

      Assert.NotNull(deserializedModules);
      Assert.Equal(2, deserializedModules["SampleLibrary.dll"]["AsyncClasses.cs"].Count);
      Assert.True(deserializedModules["SampleLibrary.dll"]["AsyncClasses.cs"].ContainsKey(asyncClass1));
      Assert.True(deserializedModules["SampleLibrary.dll"]["AsyncClasses.cs"].ContainsKey(asyncClass2));
    }

    /// <summary>
    /// Issue #1843: Verify that JSON with PascalCase property names (Lines, Branches)
    /// deserializes correctly without DictionaryKeyPolicy.
    /// </summary>
    [Fact]
    public void JsonDeserialization_PascalCasePropertyNames_DeserializesCorrectly()
    {
      // Arrange - JSON with PascalCase property names as produced by JsonReporter
      string json = @"{
  ""TestModule.dll"": {
    ""TestFile.cs"": {
      ""TestNamespace.TestClass"": {
        ""System.Void TestNamespace.TestClass::TestMethod()"": {
          ""Lines"": {
            ""1"": 1,
            ""2"": 0
          },
          ""Branches"": []
        }
      }
    }
  }
}";

      var options = new JsonSerializerOptions
      {
        PropertyNameCaseInsensitive = true,
        IncludeFields = true
      };

      // Act
      var modules = JsonSerializer.Deserialize<Modules>(json, options);

      // Assert
      Assert.NotNull(modules);
      Assert.True(modules.ContainsKey("TestModule.dll"));

      var method = modules["TestModule.dll"]["TestFile.cs"]["TestNamespace.TestClass"]["System.Void TestNamespace.TestClass::TestMethod()"];
      Assert.NotNull(method);
      Assert.Equal(2, method.Lines.Count);
      Assert.Equal(1, method.Lines[1]);
      Assert.Equal(0, method.Lines[2]);
      Assert.Empty(method.Branches);
    }

    /// <summary>
    /// Issue #1843: Verify that JSON serialization round-trip preserves async class names.
    /// </summary>
    [Fact]
    public void JsonSerialization_RoundTrip_PreservesAsyncClassNames()
    {
      // Arrange
      var originalModules = new Modules();
      var documents = new Documents();
      var classes = new Classes();

      // Add various async state machine class names
      var asyncClassNames = new[]
      {
        "Namespace.Class/<Method>d__0",
        "Namespace.Class/<Method>d__1",
        "Namespace.Class+<>c__DisplayClass0_0",
        "Namespace.Class/<>c",
        "Namespace.Class/<Method>g__LocalFunc|0_0"
      };

      foreach (string className in asyncClassNames)
      {
        var methods = new Methods();
        methods.Add($"System.Void {className}::MoveNext()", new Method { Lines = new Lines { { 1, 1 } } });
        classes.Add(className, methods);
      }

      documents.Add("Test.cs", classes);
      originalModules.Add("Test.dll", documents);

      var options = new JsonSerializerOptions
      {
        PropertyNameCaseInsensitive = true,
        IncludeFields = true,
        WriteIndented = true
      };

      // Act - Serialize and deserialize
      string json = JsonSerializer.Serialize(originalModules, options);
      var deserializedModules = JsonSerializer.Deserialize<Modules>(json, options);

      // Assert - All class names should be preserved
      Assert.NotNull(deserializedModules);
      Assert.Equal(asyncClassNames.Length, deserializedModules["Test.dll"]["Test.cs"].Count);

      foreach (string className in asyncClassNames)
      {
        Assert.True(
          deserializedModules["Test.dll"]["Test.cs"].ContainsKey(className),
          $"Class name '{className}' was not preserved during serialization round-trip");
      }
    }

    /// <summary>
    /// Issue #1843: Verify that camelCase dictionary key policy transforms dictionary keys.
    /// This test documents the behavior when DictionaryKeyPolicy is set.
    /// </summary>
    [Fact]
    public void JsonSerialization_WithCamelCasePolicy_TransformsDictionaryKeys()
    {
      // Arrange
      var modules = new Modules();
      var documents = new Documents();
      var classes = new Classes();
      var methods = new Methods();

      // Use a simpler class name without special characters for clearer testing
      string className = "Namespace.TestClass";
      methods.Add("MoveNext", new Method { Lines = new Lines { { 1, 1 } } });
      classes.Add(className, methods);
      documents.Add("Test.cs", classes);
      modules.Add("Test.dll", documents);

      // Serialize with CamelCase policy
      var camelCaseOptions = new JsonSerializerOptions
      {
        PropertyNameCaseInsensitive = true,
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
        IncludeFields = true,
        WriteIndented = true
      };

      // Act
      string jsonWithCamelCase = JsonSerializer.Serialize(modules, camelCaseOptions);

      // Assert - CamelCase policy lowercases the first character of the entire key
      // "Test.dll" becomes "test.dll", "Test.cs" becomes "test.cs"
      // "Namespace.TestClass" becomes "namespace.TestClass"
      Assert.Contains("test.dll", jsonWithCamelCase);
      Assert.Contains("test.cs", jsonWithCamelCase);
      Assert.Contains("namespace.TestClass", jsonWithCamelCase);
      Assert.Contains("moveNext", jsonWithCamelCase);

      // Verify original keys are NOT present (they were transformed)
      Assert.DoesNotContain("\"Test.dll\"", jsonWithCamelCase);
      Assert.DoesNotContain("\"Namespace.TestClass\"", jsonWithCamelCase);
    }

    /// <summary>
    /// Verify that the correct serialization options (without CamelCase policy) preserve dictionary keys exactly.
    /// </summary>
    [Fact]
    public void JsonSerialization_WithoutCamelCasePolicy_PreservesDictionaryKeys()
    {
      // Arrange
      var modules = new Modules();
      var documents = new Documents();
      var classes = new Classes();
      var methods = new Methods();

      string className = "Namespace.TestClass";
      methods.Add("MoveNext", new Method { Lines = new Lines { { 1, 1 } } });
      classes.Add(className, methods);
      documents.Add("Test.cs", classes);
      modules.Add("Test.dll", documents);

      // Serialize WITHOUT CamelCase policy (the correct configuration)
      var correctOptions = new JsonSerializerOptions
      {
        PropertyNameCaseInsensitive = true,
        IncludeFields = true,
        WriteIndented = true
      };

      // Act
      string json = JsonSerializer.Serialize(modules, correctOptions);
      var deserializedModules = JsonSerializer.Deserialize<Modules>(json, correctOptions);

      // Assert - Dictionary keys should be preserved exactly (case-sensitive)
      Assert.Contains("\"Test.dll\"", json);
      Assert.Contains("\"Test.cs\"", json);
      Assert.Contains("\"Namespace.TestClass\"", json);
      Assert.Contains("\"MoveNext\"", json);

      Assert.NotNull(deserializedModules);
      Assert.True(deserializedModules.ContainsKey("Test.dll"),
        "Module key should be preserved exactly without CamelCase policy");
      Assert.True(deserializedModules["Test.dll"]["Test.cs"].ContainsKey(className),
        "Class name should be preserved exactly without CamelCase policy");
    }

    /// <summary>
    /// Issue #1843: Verify MergeWith JSON file format compatibility.
    /// The MergeWith file uses PascalCase property names.
    /// </summary>
    [Fact]
    public void MergeWith_JsonFormat_IsCompatibleWithDeserialization()
    {
      // Arrange - JSON format as used in MergeWith.coverage.json files
      string mergeWithJson = @"{
  ""coverletsamplelib.integration.template.dll"": {
    ""C:\\source\\Program.cs"": {
      ""HelloWorld.Program"": {
        ""System.Void HelloWorld.Program::Main(System.String[])"": {
          ""Lines"": {
            ""10"": 1,
            ""11"": 1
          },
          ""Branches"": []
        }
      }
    }
  }
}";

      var options = new JsonSerializerOptions
      {
        PropertyNameCaseInsensitive = true,
        IncludeFields = true
      };

      // Act
      var modules = JsonSerializer.Deserialize<Modules>(mergeWithJson, options);

      // Assert
      Assert.NotNull(modules);
      Assert.True(modules.ContainsKey("coverletsamplelib.integration.template.dll"));

      var method = modules["coverletsamplelib.integration.template.dll"]
        ["C:\\source\\Program.cs"]
        ["HelloWorld.Program"]
        ["System.Void HelloWorld.Program::Main(System.String[])"];

      Assert.NotNull(method);
      Assert.Equal(2, method.Lines.Count);
      Assert.Empty(method.Branches);
    }

    /// <summary>
    /// Verify that branch coverage data serializes correctly.
    /// </summary>
    [Fact]
    public void JsonSerialization_BranchCoverage_SerializesCorrectly()
    {
      // Arrange
      var result = new CoverageResult
      {
        Identifier = Guid.NewGuid().ToString()
      };

      var method = new Method
      {
        Lines = new Lines { { 1, 1 }, { 2, 0 } },
        Branches = new Branches
        {
          new BranchInfo { Line = 1, Offset = 0, EndOffset = 10, Path = 0, Ordinal = 0, Hits = 1 },
          new BranchInfo { Line = 1, Offset = 0, EndOffset = 10, Path = 1, Ordinal = 1, Hits = 0 }
        }
      };

      var methods = new Methods { { "TestMethod", method } };
      var classes = new Classes { { "TestClass", methods } };
      var documents = new Documents { { "Test.cs", classes } };
      result.Modules = new Modules { { "Test.dll", documents } };

      var reporter = new JsonReporter();

      // Act
      string json = reporter.Report(result, new Mock<ISourceRootTranslator>().Object);

      // Assert
      Assert.Contains("\"Branches\":", json);
      Assert.Contains("\"Line\": 1", json);
      Assert.Contains("\"Hits\": 1", json);
      Assert.Contains("\"Hits\": 0", json);

      // Verify deserialization
      var options = new JsonSerializerOptions
      {
        PropertyNameCaseInsensitive = true,
        IncludeFields = true
      };
      var deserializedModules = JsonSerializer.Deserialize<Modules>(json, options);

      Assert.NotNull(deserializedModules);
      var deserializedMethod = deserializedModules["Test.dll"]["Test.cs"]["TestClass"]["TestMethod"];
      Assert.Equal(2, deserializedMethod.Branches.Count);
      Assert.Equal(1, deserializedMethod.Branches[0].Hits);
      Assert.Equal(0, deserializedMethod.Branches[1].Hits);
    }
  }
}
