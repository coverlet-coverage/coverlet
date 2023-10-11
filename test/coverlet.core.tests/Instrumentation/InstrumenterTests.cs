// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Coverlet.Core.Abstractions;
using Coverlet.Core.Helpers;
using Coverlet.Core.Samples.Tests;
using Coverlet.Core.Symbols;
using Coverlet.Core.Tests;
using Coverlet.Tests.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.VisualStudio.TestPlatform;
using Mono.Cecil;
using Moq;
using Xunit;

namespace Coverlet.Core.Instrumentation.Tests
{
  public class InstrumenterTests : IDisposable
  {
    private readonly Mock<ILogger> _mockLogger = new();
    private Action _disposeAction;

    public void Dispose()
    {
      _disposeAction?.Invoke();
    }

    [Fact]
    public void TestCoreLibInstrumentation()
    {
      DirectoryInfo directory = Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), nameof(TestCoreLibInstrumentation)));
      string[] files = new[]
      {
                "System.Private.CoreLib.dll",
                "System.Private.CoreLib.pdb"
            };

      foreach (string file in files)
      {
        File.Copy(Path.Combine(Directory.GetCurrentDirectory(), "TestAssets", file), Path.Combine(directory.FullName, file), overwrite: true);
      }

      var partialMockFileSystem = new Mock<FileSystem>();
      partialMockFileSystem.CallBase = true;
      partialMockFileSystem.Setup(fs => fs.OpenRead(It.IsAny<string>())).Returns((string path) =>
      {
        if (Path.GetFileName(path.Replace(@"\", @"/")) == files[1])
        {
          return File.OpenRead(Path.Combine(Path.Combine(Directory.GetCurrentDirectory(), "TestAssets"), files[1]));
        }
        else
        {
          return File.OpenRead(path);
        }
      });
      partialMockFileSystem.Setup(fs => fs.Exists(It.IsAny<string>())).Returns((string path) =>
      {
        if (Path.GetFileName(path.Replace(@"\", @"/")) == files[1])
        {
          return File.Exists(Path.Combine(Path.Combine(Directory.GetCurrentDirectory(), "TestAssets"), files[1]));
        }
        else
        {
          if (path.Contains(@":\git\runtime"))
          {
            return true;
          }
          else
          {
            return File.Exists(path);
          }
        }
      });
      var sourceRootTranslator = new SourceRootTranslator(_mockLogger.Object, new FileSystem());
      var parameters = new CoverageParameters();
      var instrumentationHelper =
          new InstrumentationHelper(new ProcessExitHandler(), new RetryHelper(), partialMockFileSystem.Object, _mockLogger.Object, sourceRootTranslator);
      var instrumenter = new Instrumenter(Path.Combine(directory.FullName, files[0]), "_coverlet_instrumented", parameters, _mockLogger.Object, instrumentationHelper, partialMockFileSystem.Object, sourceRootTranslator, new CecilSymbolHelper());

      Assert.True(instrumenter.CanInstrument());
      InstrumenterResult result = instrumenter.Instrument();
      Assert.NotNull(result);
      Assert.Equal(1052, result.Documents.Count);
      foreach ((string docName, Document _) in result.Documents)
      {
        Assert.False(docName.EndsWith(@"System.Private.CoreLib\src\System\Threading\Interlocked.cs"));
      }
      directory.Delete(true);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TestInstrument(bool singleHit)
    {
      InstrumenterTest instrumenterTest = CreateInstrumentor(singleHit: singleHit);

      InstrumenterResult result = instrumenterTest.Instrumenter.Instrument();

      Assert.Equal(Path.GetFileNameWithoutExtension(instrumenterTest.Module), result.Module);
      Assert.Equal(instrumenterTest.Module, result.ModulePath);

      instrumenterTest.Directory.Delete(true);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TestInstrumentCoreLib(bool singleHit)
    {
      InstrumenterTest instrumenterTest = CreateInstrumentor(fakeCoreLibModule: true, singleHit: singleHit);

      InstrumenterResult result = instrumenterTest.Instrumenter.Instrument();

      Assert.Equal(Path.GetFileNameWithoutExtension(instrumenterTest.Module), result.Module);
      Assert.Equal(instrumenterTest.Module, result.ModulePath);

      instrumenterTest.Directory.Delete(true);
    }

    [Theory]
    [InlineData(typeof(ClassExcludedByCodeAnalysisCodeCoverageAttr))]
    [InlineData(typeof(ClassExcludedByCoverletCodeCoverageAttr))]
    public void TestInstrument_ClassesWithExcludeAttributeAreExcluded(Type excludedType)
    {
      InstrumenterTest instrumenterTest = CreateInstrumentor();
      InstrumenterResult result = instrumenterTest.Instrumenter.Instrument();

      Document doc = result.Documents.Values.FirstOrDefault(d => Path.GetFileName(d.Path) == "Samples.cs");
      Assert.NotNull(doc);

      bool found = doc.Lines.Values.Any(l => l.Class == excludedType.FullName);
      Assert.False(found, "Class decorated with with exclude attribute should be excluded");

      instrumenterTest.Directory.Delete(true);
    }

    [Theory]
    [InlineData(typeof(ClassExcludedByAttrWithoutAttributeNameSuffix), nameof(TestSDKAutoGeneratedCode))]
    public void TestInstrument_ClassesWithExcludeAttributeWithoutAttributeNameSuffixAreExcluded(Type excludedType, string excludedAttribute)
    {
      InstrumenterTest instrumenterTest = CreateInstrumentor(attributesToIgnore: new string[] { excludedAttribute });
      InstrumenterResult result = instrumenterTest.Instrumenter.Instrument();

      Document doc = result.Documents.Values.FirstOrDefault(d => Path.GetFileName(d.Path) == "Samples.cs");
      Assert.NotNull(doc);

      bool found = doc.Lines.Values.Any(l => l.Class == excludedType.FullName);
      Assert.False(found, "Class decorated with with exclude attribute should be excluded");

      instrumenterTest.Directory.Delete(true);
    }

    [Theory]
    [InlineData(nameof(ObsoleteAttribute))]
    [InlineData("Obsolete")]
    [InlineData(nameof(TestSDKAutoGeneratedCode))]
    public void TestInstrument_ClassesWithCustomExcludeAttributeAreExcluded(string excludedAttribute)
    {
      InstrumenterTest instrumenterTest = CreateInstrumentor(attributesToIgnore: new string[] { excludedAttribute });
      InstrumenterResult result = instrumenterTest.Instrumenter.Instrument();

      Document doc = result.Documents.Values.FirstOrDefault(d => Path.GetFileName(d.Path) == "Samples.cs");
      Assert.NotNull(doc);
#pragma warning disable CS0612 // Type or member is obsolete
      bool found = doc.Lines.Values.Any(l => l.Class.Equals(nameof(ClassExcludedByObsoleteAttr)));
#pragma warning restore CS0612 // Type or member is obsolete
      Assert.False(found, "Class decorated with with exclude attribute should be excluded");

      instrumenterTest.Directory.Delete(true);
    }

    [Theory]
    [InlineData(nameof(ObsoleteAttribute), "ClassWithMethodExcludedByObsoleteAttr")]
    [InlineData("Obsolete", "ClassWithMethodExcludedByObsoleteAttr")]
    [InlineData(nameof(TestSDKAutoGeneratedCode), "ClassExcludedByAttrWithoutAttributeNameSuffix")]
    public void TestInstrument_ClassesWithMethodWithCustomExcludeAttributeAreExcluded(string excludedAttribute, string testClassName)
    {
      InstrumenterTest instrumenterTest = CreateInstrumentor(attributesToIgnore: new string[] { excludedAttribute });
      InstrumenterResult result = instrumenterTest.Instrumenter.Instrument();

      Document doc = result.Documents.Values.FirstOrDefault(d => Path.GetFileName(d.Path) == "Samples.cs");
      Assert.NotNull(doc);
      bool found = doc.Lines.Values.Any(l => l.Method.Equals($"System.String Coverlet.Core.Samples.Tests.{testClassName}::Method(System.String)"));
      Assert.False(found, "Method decorated with with exclude attribute should be excluded");

      instrumenterTest.Directory.Delete(true);
    }

    [Theory]
    [InlineData(nameof(ObsoleteAttribute), "ClassWithMethodExcludedByObsoleteAttr")]
    [InlineData("Obsolete", "ClassWithMethodExcludedByObsoleteAttr")]
    [InlineData(nameof(TestSDKAutoGeneratedCode), "ClassExcludedByAttrWithoutAttributeNameSuffix")]
    public void TestInstrument_ClassesWithPropertyWithCustomExcludeAttributeAreExcluded(string excludedAttribute, string testClassName)
    {
      InstrumenterTest instrumenterTest = CreateInstrumentor(attributesToIgnore: new string[] { excludedAttribute });
      InstrumenterResult result = instrumenterTest.Instrumenter.Instrument();

      Document doc = result.Documents.Values.FirstOrDefault(d => Path.GetFileName(d.Path) == "Samples.cs");
      Assert.NotNull(doc);
      bool getFound = doc.Lines.Values.Any(l => l.Method.Equals($"System.String Coverlet.Core.Samples.Tests.{testClassName}::get_Property()"));
      Assert.False(getFound, "Property getter decorated with with exclude attribute should be excluded");

      bool setFound = doc.Lines.Values.Any(l => l.Method.Equals($"System.String Coverlet.Core.Samples.Tests.{testClassName}::set_Property()"));
      Assert.False(setFound, "Property setter decorated with with exclude attribute should be excluded");

      instrumenterTest.Directory.Delete(true);
    }

    private InstrumenterTest CreateInstrumentor(bool fakeCoreLibModule = false, string[] attributesToIgnore = null, string[] excludedFiles = null, bool singleHit = false)
    {
      string module = GetType().Assembly.Location;
      string pdb = Path.Combine(Path.GetDirectoryName(module), Path.GetFileNameWithoutExtension(module) + ".pdb");
      string identifier = Guid.NewGuid().ToString();

      DirectoryInfo directory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), identifier));

      string destModule, destPdb;
      if (fakeCoreLibModule)
      {
        destModule = "System.Private.CoreLib.dll";
        destPdb = "System.Private.CoreLib.pdb";
      }
      else
      {
        destModule = Path.GetFileName(module);
        destPdb = Path.GetFileName(pdb);
      }

      File.Copy(module, Path.Combine(directory.FullName, destModule), true);
      File.Copy(pdb, Path.Combine(directory.FullName, destPdb), true);

      var instrumentationHelper =
          new InstrumentationHelper(new ProcessExitHandler(), new RetryHelper(), new FileSystem(), new Mock<ILogger>().Object, new SourceRootTranslator(new Mock<ILogger>().Object, new FileSystem()));

      module = Path.Combine(directory.FullName, destModule);
      CoverageParameters parameters = new()
      {
        ExcludeAttributes = attributesToIgnore,
        DoesNotReturnAttributes = new string[] { "DoesNotReturnAttribute" }
      };
      var instrumenter = new Instrumenter(module, identifier, parameters, _mockLogger.Object, instrumentationHelper, new FileSystem(), new SourceRootTranslator(_mockLogger.Object, new FileSystem()), new CecilSymbolHelper());
      return new InstrumenterTest
      {
        Instrumenter = instrumenter,
        Module = module,
        Identifier = identifier,
        Directory = directory
      };
    }

    class InstrumenterTest
    {
      public Instrumenter Instrumenter { get; set; }

      public string Module { get; set; }

      public string Identifier { get; set; }

      public DirectoryInfo Directory { get; set; }
    }

    [Fact]
    public void TestInstrument_NetStandardAwareAssemblyResolver_FromRuntime()
    {
      var netstandardResolver = new NetstandardAwareAssemblyResolver(null, _mockLogger.Object);

      // We ask for "official" netstandard.dll implementation with know MS public key cc7b13ffcd2ddd51 same in all runtime
      AssemblyDefinition resolved = netstandardResolver.Resolve(AssemblyNameReference.Parse("netstandard, Version=0.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51"));
      Assert.NotNull(resolved);

      // We check that netstandard.dll was resolved from runtime folder, where System.Object is
      Assert.Equal(Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location), "netstandard.dll"), resolved.MainModule.FileName);
    }

    [Fact]
    public void TestInstrument_NetStandardAwareAssemblyResolver_FromFolder()
    {
      // Someone could create a custom dll named netstandard.dll we need to be sure that not
      // conflicts with "official" resolution

      // We create dummy netstandard.dll
      var compilation = CSharpCompilation.Create(
          "netstandard",
          new[] { CSharpSyntaxTree.ParseText("") },
          new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
          new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

      Assembly newAssemlby;
      using (var dllStream = new MemoryStream())
      {
        EmitResult emitResult = compilation.Emit(dllStream);
        Assert.True(emitResult.Success);
        newAssemlby = Assembly.Load(dllStream.ToArray());
        // remove if exists
        File.Delete("netstandard.dll");
        File.WriteAllBytes("netstandard.dll", dllStream.ToArray());
      }

      var netstandardResolver = new NetstandardAwareAssemblyResolver(newAssemlby.Location, _mockLogger.Object);
      AssemblyDefinition resolved = netstandardResolver.Resolve(AssemblyNameReference.Parse(newAssemlby.FullName));

      // We check if final netstandard.dll resolved is local folder one and not "official" netstandard.dll
      Assert.Equal(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "netstandard.dll"), Path.GetFullPath(resolved.MainModule.FileName));
    }

    public static IEnumerable<object[]> TestInstrument_ExcludedFilesHelper_Data()
    {
      yield return new object[] { new string[]{ @"one.txt" }, new ValueTuple<string, bool, bool>[]
                                        {
                                            (@"one.txt", true, false),
                                            (@"c:\dir\one.txt", false, true),
                                            (@"dir/one.txt", false, false)
                                        }};
      yield return new object[] { new string[]{ @"*one.txt" }, new ValueTuple<string, bool, bool>[]
                                        {
                                            (@"one.txt", true , false),
                                            (@"c:\dir\one.txt", false, true),
                                            (@"dir/one.txt", false, false)
                                        }};
      yield return new object[] { new string[]{ @"*.txt" }, new ValueTuple<string, bool, bool>[]
                                        {
                                            (@"one.txt", true, false),
                                            (@"c:\dir\one.txt", false, true),
                                            (@"dir/one.txt", false, false)
                                        }};
      yield return new object[] { new string[]{ @"*.*" }, new ValueTuple<string, bool, bool>[]
                                        {
                                            (@"one.txt", true, false),
                                            (@"c:\dir\one.txt", false, true),
                                            (@"dir/one.txt", false, false)
                                        }};
      yield return new object[] { new string[]{ @"one.*" }, new ValueTuple<string, bool, bool>[]
                                        {
                                            (@"one.txt", true, false),
                                            (@"c:\dir\one.txt", false, true),
                                            (@"dir/one.txt", false, false)
                                        }};
      yield return new object[] { new string[]{ @"dir/*.txt" }, new ValueTuple<string, bool, bool>[]
                                        {
                                            (@"one.txt", false, false),
                                            (@"c:\dir\one.txt", true, true),
                                            (@"dir/one.txt", true, false)
                                        }};
      yield return new object[] { new string[]{ @"dir\*.txt" }, new ValueTuple<string, bool, bool>[]
                                        {
                                            (@"one.txt", false, false),
                                            (@"c:\dir\one.txt", true, true),
                                            (@"dir/one.txt", true, false)
                                        }};
      yield return new object[] { new string[]{ @"**/*" }, new ValueTuple<string, bool, bool>[]
                                        {
                                            (@"one.txt", true, false),
                                            (@"c:\dir\one.txt", true, true),
                                            (@"dir/one.txt", true, false)
                                        }};
      yield return new object[] { new string[]{ @"dir/**/*" }, new ValueTuple<string, bool, bool>[]
                                        {
                                            (@"one.txt", false, false),
                                            (@"c:\dir\one.txt", true, true),
                                            (@"dir/one.txt", true, false),
                                            (@"c:\dir\dir2\one.txt", true, true),
                                            (@"dir/dir2/one.txt", true, false)
                                        }};
      yield return new object[] { new string[]{ @"one.txt", @"dir\*two.txt" }, new ValueTuple<string, bool, bool>[]
                                        {
                                            (@"one.txt", true, false),
                                            (@"c:\dir\imtwo.txt", true, true),
                                            (@"dir/one.txt", false, false)
                                        }};

      // This is a special case test different drive same path
      // We strip out drive from path to check for globbing
      // BTW I don't know if makes sense add a filter with full path maybe we should forbid
      yield return new object[] { new string[]{ @"c:\dir\one.txt" }, new ValueTuple<string, bool, bool>[]
                                        {
                                            (@"c:\dir\one.txt", true, true),
                                            (@"d:\dir\one.txt", true, true) // maybe should be false?
                                        }};

      yield return new object[] { new string[]{ null }, new ValueTuple<string, bool, bool>[]
                                        {
                                            (null, false, false),
                                        }};
    }

    [Theory]
    [MemberData(nameof(TestInstrument_ExcludedFilesHelper_Data))]
    public void TestInstrument_ExcludedFilesHelper(string[] excludeFilterHelper, ValueTuple<string, bool, bool>[] result)
    {
      var exludeFilterHelper = new ExcludedFilesHelper(excludeFilterHelper, new Mock<ILogger>().Object);
      foreach (ValueTuple<string, bool, bool> checkFile in result)
      {
        if (checkFile.Item3) // run test only on windows platform
        {
          if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
          {
            Assert.Equal(checkFile.Item2, exludeFilterHelper.Exclude(checkFile.Item1));
          }
        }
        else
        {
          Assert.Equal(checkFile.Item2, exludeFilterHelper.Exclude(checkFile.Item1));
        }
      }
    }

    [Fact]
    public void SkipEmbeddedPpdbWithoutLocalSource()
    {
      string xunitDll = Directory.GetFiles(Directory.GetCurrentDirectory(), "xunit.core.dll").First();
      var loggerMock = new Mock<ILogger>();

      var instrumentationHelper =
          new InstrumentationHelper(new ProcessExitHandler(), new RetryHelper(), new FileSystem(), loggerMock.Object,
                                    new SourceRootTranslator(xunitDll, new Mock<ILogger>().Object, new FileSystem(), new AssemblyAdapter()));

      var instrumenter = new Instrumenter(xunitDll, "_xunit_instrumented", new CoverageParameters(), loggerMock.Object, instrumentationHelper, new FileSystem(), new SourceRootTranslator(xunitDll, loggerMock.Object, new FileSystem(), new AssemblyAdapter()), new CecilSymbolHelper());
      Assert.True(instrumentationHelper.HasPdb(xunitDll, out bool embedded));
      Assert.True(embedded);
      Assert.False(instrumenter.CanInstrument());
      loggerMock.Verify(l => l.LogVerbose(It.IsAny<string>()));

      // Default case
      string sample = Directory.GetFiles(Directory.GetCurrentDirectory(), "coverlet.tests.projectsample.empty.dll").First();
      instrumentationHelper =
          new InstrumentationHelper(new ProcessExitHandler(), new RetryHelper(), new FileSystem(), new Mock<ILogger>().Object,
                                    new SourceRootTranslator(Assembly.GetExecutingAssembly().Location, new Mock<ILogger>().Object, new FileSystem(), new AssemblyAdapter()));

      instrumenter = new Instrumenter(sample, "_coverlet_tests_projectsample_empty", new CoverageParameters(), loggerMock.Object, instrumentationHelper, new FileSystem(), new SourceRootTranslator(Assembly.GetExecutingAssembly().Location, loggerMock.Object, new FileSystem(), new AssemblyAdapter()), new CecilSymbolHelper());

      Assert.True(instrumentationHelper.HasPdb(sample, out embedded));
      Assert.False(embedded);
      Assert.True(instrumenter.CanInstrument());
      loggerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public void SkipPpdbWithoutLocalSource()
    {
      string dllFileName = "75d9f96508d74def860a568f426ea4a4.dll";
      string pdbFileName = "75d9f96508d74def860a568f426ea4a4.pdb";

      var partialMockFileSystem = new Mock<FileSystem>();
      partialMockFileSystem.CallBase = true;
      partialMockFileSystem.Setup(fs => fs.OpenRead(It.IsAny<string>())).Returns((string path) =>
      {
        if (Path.GetFileName(path.Replace(@"\", @"/")) == pdbFileName)
        {
          return File.OpenRead(Path.Combine(Path.Combine(Directory.GetCurrentDirectory(), "TestAssets"), pdbFileName));
        }
        else
        {
          return File.OpenRead(path);
        }
      });
      partialMockFileSystem.Setup(fs => fs.Exists(It.IsAny<string>())).Returns((string path) =>
      {
        if (Path.GetFileName(path.Replace(@"\", @"/")) == pdbFileName)
        {
          return File.Exists(Path.Combine(Path.Combine(Directory.GetCurrentDirectory(), "TestAssets"), pdbFileName));
        }
        else
        {
          return File.Exists(path);
        }
      });

      var instrumentationHelper =
          new InstrumentationHelper(new ProcessExitHandler(), new RetryHelper(), partialMockFileSystem.Object, _mockLogger.Object, new SourceRootTranslator(_mockLogger.Object, new FileSystem()));
      string sample = Directory.GetFiles(Path.Combine(Directory.GetCurrentDirectory(), "TestAssets"), dllFileName).First();
      var loggerMock = new Mock<ILogger>();
      var instrumenter = new Instrumenter(sample, "_75d9f96508d74def860a568f426ea4a4_instrumented", new CoverageParameters(), loggerMock.Object, instrumentationHelper, partialMockFileSystem.Object, new SourceRootTranslator(loggerMock.Object, new FileSystem()), new CecilSymbolHelper());

      Assert.True(instrumentationHelper.HasPdb(sample, out bool embedded));
      Assert.False(embedded);
      Assert.False(instrumenter.CanInstrument());
      _mockLogger.Verify(l => l.LogVerbose(It.IsAny<string>()));
    }

    [Fact]
    public void TestInstrument_MissingModule()
    {
      var loggerMock = new Mock<ILogger>();

      var instrumentationHelper =
              new InstrumentationHelper(new ProcessExitHandler(), new RetryHelper(), new FileSystem(), new Mock<ILogger>().Object,
                                        new SourceRootTranslator(new Mock<ILogger>().Object, new FileSystem()));

      var instrumenter = new Instrumenter("test", "_test_instrumented", new CoverageParameters(), loggerMock.Object, instrumentationHelper, new FileSystem(), new SourceRootTranslator(loggerMock.Object, new FileSystem()), new CecilSymbolHelper());

      Assert.False(instrumenter.CanInstrument());
      loggerMock.Verify(l => l.LogWarning(It.IsAny<string>()));
    }

    [Fact]
    public void CanInstrumentFSharpAssemblyWithAnonymousRecord()
    {
      var loggerMock = new Mock<ILogger>();

      string sample = Directory.GetFiles(Directory.GetCurrentDirectory(), "coverlet.tests.projectsample.fsharp.dll").First();
      var instrumentationHelper =
          new InstrumentationHelper(new ProcessExitHandler(), new RetryHelper(), new FileSystem(), new Mock<ILogger>().Object,
              new SourceRootTranslator(Assembly.GetExecutingAssembly().Location, new Mock<ILogger>().Object, new FileSystem(), new AssemblyAdapter()));

      var instrumenter = new Instrumenter(sample, "_coverlet_tests_projectsample_fsharp", new CoverageParameters(), loggerMock.Object, instrumentationHelper,
          new FileSystem(), new SourceRootTranslator(Assembly.GetExecutingAssembly().Location, loggerMock.Object, new FileSystem(), new AssemblyAdapter()), new CecilSymbolHelper());

      Assert.True(instrumentationHelper.HasPdb(sample, out bool embedded));
      Assert.False(embedded);
      Assert.True(instrumenter.CanInstrument());
    }

    [Fact]
    public void CanInstrument_AssemblySearchTypeNone_ReturnsTrue()
    {
      var loggerMock = new Mock<ILogger>();
      var instrumentationHelper = new Mock<IInstrumentationHelper>();
      bool embeddedPdb;
      instrumentationHelper.Setup(x => x.HasPdb(It.IsAny<string>(), out embeddedPdb)).Returns(true);

      var instrumenter = new Instrumenter(It.IsAny<string>(), It.IsAny<string>(), new CoverageParameters { ExcludeAssembliesWithoutSources = "None" },
          loggerMock.Object, instrumentationHelper.Object, new Mock<IFileSystem>().Object, new Mock<ISourceRootTranslator>().Object, new CecilSymbolHelper());

      Assert.True(instrumenter.CanInstrument());
    }

    [Theory]
    [InlineData("NotAMatch", new string[] { }, false)]
    [InlineData("ExcludeFromCoverageAttribute", new string[] { }, true)]
    [InlineData("ExcludeFromCodeCoverageAttribute", new string[] { }, true)]
    [InlineData("CustomExclude", new string[] { "CustomExclude" }, true)]
    [InlineData("CustomExcludeAttribute", new string[] { "CustomExclude" }, true)]
    [InlineData("CustomExcludeAttribute", new string[] { "CustomExcludeAttribute" }, true)]
    public void TestInstrument_AssemblyMarkedAsExcludeFromCodeCoverage(string attributeName, string[] excludedAttributes, bool expectedExcludes)
    {
      string EmitAssemblyToInstrument(string outputFolder)
      {
        SyntaxTree attributeClassSyntaxTree = CSharpSyntaxTree.ParseText("[System.AttributeUsage(System.AttributeTargets.Assembly)]public class " + attributeName + ":System.Attribute{}");
        SyntaxTree instrumentableClassSyntaxTree = CSharpSyntaxTree.ParseText($@"
[assembly:{attributeName}]
namespace coverlet.tests.projectsample.excludedbyattribute{{
public class SampleClass
{{
    public int SampleMethod()
    {{
        return new System.Random().Next();
    }}
}}

}}
");
        CSharpCompilation compilation = CSharpCompilation.Create(attributeName, new List<SyntaxTree>
                {
                    attributeClassSyntaxTree,instrumentableClassSyntaxTree
                }).AddReferences(
            MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location)).
        WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, false));

        string dllPath = Path.Combine(outputFolder, $"{attributeName}.dll");
        string pdbPath = Path.Combine(outputFolder, $"{attributeName}.pdb");

        using (FileStream outputStream = File.Create(dllPath))
        using (FileStream pdbStream = File.Create(pdbPath))
        {
          bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
          var emitOptions = new EmitOptions(pdbFilePath: pdbPath);
          EmitResult emitResult = compilation.Emit(outputStream, pdbStream, options: isWindows ? emitOptions : emitOptions.WithDebugInformationFormat(DebugInformationFormat.PortablePdb));
          if (!emitResult.Success)
          {
            string message = "Failure to dynamically create dll";
            foreach (Diagnostic diagnostic in emitResult.Diagnostics)
            {
              message += Environment.NewLine;
              message += diagnostic.GetMessage();
            }
            throw new Xunit.Sdk.XunitException(message);
          }
        }
        return dllPath;
      }

      string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
      Directory.CreateDirectory(tempDirectory);
      _disposeAction = () => Directory.Delete(tempDirectory, true);

      var partialMockFileSystem = new Mock<FileSystem>();
      partialMockFileSystem.CallBase = true;
      partialMockFileSystem.Setup(fs => fs.NewFileStream(It.IsAny<string>(), It.IsAny<FileMode>(), It.IsAny<FileAccess>())).Returns((string path, FileMode mode, FileAccess access) =>
      {
        return new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
      });
      var loggerMock = new Mock<ILogger>();

      string excludedbyattributeDll = EmitAssemblyToInstrument(tempDirectory);

      var instrumentationHelper =
              new InstrumentationHelper(new ProcessExitHandler(), new RetryHelper(), new FileSystem(), new Mock<ILogger>().Object,
                                        new SourceRootTranslator(new Mock<ILogger>().Object, new FileSystem()));
      CoverageParameters parametes = new();
      parametes.ExcludeAttributes = excludedAttributes;
      var instrumenter = new Instrumenter(excludedbyattributeDll, "_xunit_excludedbyattribute", parametes, loggerMock.Object, instrumentationHelper, partialMockFileSystem.Object, new SourceRootTranslator(loggerMock.Object, new FileSystem()), new CecilSymbolHelper());

      InstrumenterResult result = instrumenter.Instrument();
      Assert.Empty(result.Documents);
      if (expectedExcludes) { loggerMock.Verify(l => l.LogVerbose(It.IsAny<string>())); }
    }

    [Fact]
    public void TestInstrument_NetstandardAwareAssemblyResolver_PreserveCompilationContext()
    {
      var netstandardResolver = new NetstandardAwareAssemblyResolver(Assembly.GetExecutingAssembly().Location, _mockLogger.Object);
      AssemblyDefinition asm = netstandardResolver.TryWithCustomResolverOnDotNetCore(new AssemblyNameReference("Microsoft.Extensions.Logging.Abstractions", new Version("2.2.0")));
      Assert.NotNull(asm);
    }

    [Fact]
    public void TestReachabilityHelper()
    {
      int[] allInstrumentableLines =
          new[]
          {
                    // Throws
                    7, 8,
                    // NoBranches
                    12, 13, 14, 15, 16,
                    // If
                    19, 20, 22, 23, 24, 25, 26, 27, 29, 30,
                    // Switch
                    33, 34, 36, 39, 40, 41, 42, 44, 45, 49, 50, 52, 53, 55, 56, 58, 59, 61, 62, 64, 65, 68, 69,
                    // Subtle
                    72, 73, 75, 78, 79, 80, 82, 83, 86, 87, 88, 91, 92, 95, 96, 98, 99, 101, 102, 103,
                    // UnreachableBranch
                    106, 107, 108, 110, 111, 112, 113, 114,
                    // ThrowsGeneric
                    118, 119,
                    // CallsGenericMethodDoesNotReturn
                    124, 125, 126, 127, 128,
                    // AlsoThrows
                    134, 135,
                    // CallsGenericClassDoesNotReturn
                    140, 141, 142, 143, 144,
                    // WithLeave
                    147, 149, 150, 151, 152, 153, 154, 155, 156, 159, 161, 163, 166, 167, 168,
                    // FiltersAndFinallies
                    171, 173, 174, 175, 176, 177, 179, 180, 181, 182, 183, 184, 185, 186, 187, 188, 189, 190, 192, 193, 194, 195, 196, 197
          };
      int[] notReachableLines =
          new[]
          {
                    // NoBranches
                    15, 16,
                    // If
                    26, 27,
                    // Switch
                    41, 42,
                    // Subtle
                    79, 80, 88, 96, 98, 99,
                    // UnreachableBranch
                    110, 111, 112, 113, 114,
                    // CallsGenericMethodDoesNotReturn
                    127, 128,
                    // CallsGenericClassDoesNotReturn
                    143, 144,
                    // WithLeave
                    163, 164,
                    // FiltersAndFinallies
                    176, 177, 183, 184, 189, 190, 195, 196, 197
          };

      int[] expectedToBeInstrumented = allInstrumentableLines.Except(notReachableLines).ToArray();

      InstrumenterTest instrumenterTest = CreateInstrumentor();
      InstrumenterResult result = instrumenterTest.Instrumenter.Instrument();

      Document doc = result.Documents.Values.FirstOrDefault(d => Path.GetFileName(d.Path) == "Instrumentation.DoesNotReturn.cs");

      // check for instrumented lines
      doc.AssertNonInstrumentedLines(BuildConfiguration.Debug, notReachableLines);
      doc.AssertInstrumentLines(BuildConfiguration.Debug, expectedToBeInstrumented);

      instrumenterTest.Directory.Delete(true);
    }

    [Fact]
    public void Instrumenter_MethodsWithoutReferenceToSource_AreSkipped()
    {
      var loggerMock = new Mock<ILogger>();

      string module = Directory.GetFiles(Directory.GetCurrentDirectory(), "coverlet.tests.projectsample.vbmynamespace.dll").First();
      string pdb = Path.Combine(Path.GetDirectoryName(module), Path.GetFileNameWithoutExtension(module) + ".pdb");

      DirectoryInfo directory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));

      File.Copy(module, Path.Combine(directory.FullName, Path.GetFileName(module)), true);
      File.Copy(pdb, Path.Combine(directory.FullName, Path.GetFileName(pdb)), true);

      var instrumentationHelper =
          new InstrumentationHelper(new ProcessExitHandler(), new RetryHelper(), new FileSystem(), new Mock<ILogger>().Object,
              new SourceRootTranslator(module, new Mock<ILogger>().Object, new FileSystem(), new AssemblyAdapter()));

      CoverageParameters parameters = new();

      var instrumenter = new Instrumenter(Path.Combine(directory.FullName, Path.GetFileName(module)), "_coverlet_tests_projectsample_vbmynamespace", parameters,
          loggerMock.Object, instrumentationHelper, new FileSystem(), new SourceRootTranslator(Path.Combine(directory.FullName, Path.GetFileName(module)), loggerMock.Object, new FileSystem(), new AssemblyAdapter()), new CecilSymbolHelper());

      instrumentationHelper.BackupOriginalModule(Path.Combine(directory.FullName, Path.GetFileName(module)), "_coverlet_tests_projectsample_vbmynamespace");

      InstrumenterResult result = instrumenter.Instrument();

      Assert.False(result.Documents.ContainsKey(string.Empty));

      directory.Delete(true);
    }
    [Fact]
    public void RuntimeConfigurationReaderSingleFrameworkCheck()
    {
      var reader = new RuntimeConfigurationReader(@"TestAssets/single.framework.runtimeconfig.json");

      IEnumerable<(string Name, string Version)> referencedFrameworks = reader.GetFrameworks();

      Assert.Single(referencedFrameworks);
      Assert.Collection(referencedFrameworks, item => Assert.Equal("Microsoft.NETCore.App", item.Name));
      Assert.Collection(referencedFrameworks, item => Assert.Equal("6.0.0", item.Version));

    }

    [Fact]
    public void RuntimeConfigurationReaderMultipleFrameworkCheck()
    {
      var reader = new RuntimeConfigurationReader(@"TestAssets/multiple.frameworks.runtimeconfig.json");

      (string Name, string Version)[] referencedFrameworks = reader.GetFrameworks().ToArray();

      Assert.Equal(2, referencedFrameworks.Length);
      Assert.Equal("Microsoft.NETCore.App", referencedFrameworks[0].Name);
      Assert.Equal("6.0.0", referencedFrameworks[0].Version);
      Assert.Equal("Microsoft.AspNetCore.App", referencedFrameworks[1].Name);
      Assert.Equal("6.0.0", referencedFrameworks[1].Version);
    }
  }
}
