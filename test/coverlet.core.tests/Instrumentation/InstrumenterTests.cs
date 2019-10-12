using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Coverlet.Core.Helpers;
using Coverlet.Core.Logging;
using Coverlet.Core.Samples.Tests;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Mono.Cecil;
using Moq;
using Xunit;


namespace Coverlet.Core.Instrumentation.Tests
{
    public class InstrumenterTests
    {
        private readonly InstrumentationHelper _instrumentationHelper = new InstrumentationHelper(new ProcessExitHandler(), new RetryHelper(), new FileSystem());
        private readonly Mock<ILogger> _mockLogger = new Mock<ILogger>();

        [Fact(Skip = "To be used only validating System.Private.CoreLib instrumentation")]
        public void TestCoreLibInstrumentation()
        {
            // Attention: to run this test adjust the paths and copy the IL only version of corelib
            const string OriginalFilesDir = @"c:\s\tmp\Coverlet-CoreLib\Original\";
            const string TestFilesDir = @"c:\s\tmp\Coverlet-CoreLib\Test\";

            Directory.CreateDirectory(TestFilesDir);

            string[] files = new[]
            {
                "System.Private.CoreLib.dll",
                "System.Private.CoreLib.pdb"
            };

            foreach (var file in files)
                File.Copy(Path.Combine(OriginalFilesDir, file), Path.Combine(TestFilesDir, file), overwrite: true);

            Instrumenter instrumenter = new Instrumenter(Path.Combine(TestFilesDir, files[0]), "_coverlet_instrumented", Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(), false, _mockLogger.Object, _instrumentationHelper, new FileSystem());
            Assert.True(instrumenter.CanInstrument());
            var result = instrumenter.Instrument();
            Assert.NotNull(result);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestInstrument(bool singleHit)
        {
            var instrumenterTest = CreateInstrumentor(singleHit: singleHit);

            var result = instrumenterTest.Instrumenter.Instrument();

            Assert.Equal(Path.GetFileNameWithoutExtension(instrumenterTest.Module), result.Module);
            Assert.Equal(instrumenterTest.Module, result.ModulePath);

            instrumenterTest.Directory.Delete(true);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestInstrumentCoreLib(bool singleHit)
        {
            var instrumenterTest = CreateInstrumentor(fakeCoreLibModule: true, singleHit: singleHit);

            var result = instrumenterTest.Instrumenter.Instrument();

            Assert.Equal(Path.GetFileNameWithoutExtension(instrumenterTest.Module), result.Module);
            Assert.Equal(instrumenterTest.Module, result.ModulePath);

            instrumenterTest.Directory.Delete(true);
        }

        [Theory]
        [InlineData(typeof(ClassExcludedByCodeAnalysisCodeCoverageAttr))]
        [InlineData(typeof(ClassExcludedByCoverletCodeCoverageAttr))]
        public void TestInstrument_ClassesWithExcludeAttributeAreExcluded(Type excludedType)
        {
            var instrumenterTest = CreateInstrumentor();
            var result = instrumenterTest.Instrumenter.Instrument();

            var doc = result.Documents.Values.FirstOrDefault(d => Path.GetFileName(d.Path) == "Samples.cs");
            Assert.NotNull(doc);

            var found = doc.Lines.Values.Any(l => l.Class == excludedType.FullName);
            Assert.False(found, "Class decorated with with exclude attribute should be excluded");

            instrumenterTest.Directory.Delete(true);
        }

        [Theory]
        [InlineData(nameof(ObsoleteAttribute))]
        [InlineData("Obsolete")]
        public void TestInstrument_ClassesWithCustomExcludeAttributeAreExcluded(string excludedAttribute)
        {
            var instrumenterTest = CreateInstrumentor(attributesToIgnore: new string[] { excludedAttribute });
            var result = instrumenterTest.Instrumenter.Instrument();

            var doc = result.Documents.Values.FirstOrDefault(d => Path.GetFileName(d.Path) == "Samples.cs");
            Assert.NotNull(doc);
#pragma warning disable CS0612 // Type or member is obsolete
            var found = doc.Lines.Values.Any(l => l.Class.Equals(nameof(ClassExcludedByObsoleteAttr)));
#pragma warning restore CS0612 // Type or member is obsolete
            Assert.False(found, "Class decorated with with exclude attribute should be excluded");

            instrumenterTest.Directory.Delete(true);
        }

        [Theory]
        [InlineData(nameof(ObsoleteAttribute))]
        [InlineData("Obsolete")]
        public void TestInstrument_ClassesWithMethodWithCustomExcludeAttributeAreExcluded(string excludedAttribute)
        {
            var instrumenterTest = CreateInstrumentor(attributesToIgnore: new string[] { excludedAttribute });
            var result = instrumenterTest.Instrumenter.Instrument();

            var doc = result.Documents.Values.FirstOrDefault(d => Path.GetFileName(d.Path) == "Samples.cs");
            Assert.NotNull(doc);
#pragma warning disable CS0612 // Type or member is obsolete
            var found = doc.Lines.Values.Any(l => l.Method.Equals("System.String Coverlet.Core.Samples.Tests.ClassWithMethodExcludedByObsoleteAttr::Method(System.String)"));
#pragma warning restore CS0612 // Type or member is obsolete
            Assert.False(found, "Method decorated with with exclude attribute should be excluded");

            instrumenterTest.Directory.Delete(true);
        }

        [Theory]
        [InlineData(nameof(ObsoleteAttribute))]
        [InlineData("Obsolete")]
        public void TestInstrument_ClassesWithPropertyWithCustomExcludeAttributeAreExcluded(string excludedAttribute)
        {
            var instrumenterTest = CreateInstrumentor(attributesToIgnore: new string[] { excludedAttribute });
            var result = instrumenterTest.Instrumenter.Instrument();

            var doc = result.Documents.Values.FirstOrDefault(d => Path.GetFileName(d.Path) == "Samples.cs");
            Assert.NotNull(doc);
#pragma warning disable CS0612 // Type or member is obsolete
            var getFound = doc.Lines.Values.Any(l => l.Method.Equals("System.String Coverlet.Core.Samples.Tests.ClassWithPropertyExcludedByObsoleteAttr::get_Property()"));
#pragma warning restore CS0612 // Type or member is obsolete
            Assert.False(getFound, "Property getter decorated with with exclude attribute should be excluded");

#pragma warning disable CS0612 // Type or member is obsolete
            var setFound = doc.Lines.Values.Any(l => l.Method.Equals("System.String Coverlet.Core.Samples.Tests.ClassWithPropertyExcludedByObsoleteAttr::set_Property()"));
#pragma warning restore CS0612 // Type or member is obsolete
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

            module = Path.Combine(directory.FullName, destModule);
            Instrumenter instrumenter = new Instrumenter(module, identifier, Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(), attributesToIgnore, false, _mockLogger.Object, _instrumentationHelper, new FileSystem());
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
            NetstandardAwareAssemblyResolver netstandardResolver = new NetstandardAwareAssemblyResolver();

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
            CSharpCompilation compilation = CSharpCompilation.Create(
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

            NetstandardAwareAssemblyResolver netstandardResolver = new NetstandardAwareAssemblyResolver();
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
            string xunitDll = Directory.GetFiles(Directory.GetCurrentDirectory(), "xunit.*.dll").First();
            var loggerMock = new Mock<ILogger>();
            Instrumenter instrumenter = new Instrumenter(xunitDll, "_xunit_instrumented", Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(), false, loggerMock.Object, _instrumentationHelper, new FileSystem());
            Assert.True(_instrumentationHelper.HasPdb(xunitDll, out bool embedded));
            Assert.True(embedded);
            Assert.False(instrumenter.CanInstrument());
            loggerMock.Verify(l => l.LogVerbose(It.IsAny<string>()));

            // Default case
            string sample = Directory.GetFiles(Directory.GetCurrentDirectory(), "coverlet.tests.projectsample.empty.dll").First();
            instrumenter = new Instrumenter(sample, "_coverlet_tests_projectsample_empty", Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(), false, loggerMock.Object, _instrumentationHelper, new FileSystem());
            Assert.True(_instrumentationHelper.HasPdb(sample, out embedded));
            Assert.False(embedded);
            Assert.True(instrumenter.CanInstrument());
            loggerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public void SkipPpdbWithoutLocalSource()
        {
            string dllFileName = "75d9f96508d74def860a568f426ea4a4.dll";
            string pdbFileName = "75d9f96508d74def860a568f426ea4a4.pdb";

            // We test only on win because sample dll/pdb were build on it
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Mock<FileSystem> partialMockFileSystem = new Mock<FileSystem>();
                partialMockFileSystem.CallBase = true;
                partialMockFileSystem.Setup(fs => fs.NewFileStream(It.IsAny<string>(), It.IsAny<FileMode>())).Returns((string path, FileMode mode) =>
                {
                    if (Path.GetFileName(path) == pdbFileName)
                    {
                        return new FileStream(Path.Combine(Path.Combine(Directory.GetCurrentDirectory(), "TestAssets"), pdbFileName), mode);
                    }
                    else
                    {
                        return new FileStream(path, mode);
                    }
                });
                partialMockFileSystem.Setup(fs => fs.Exists(It.IsAny<string>())).Returns((string path) =>
                {
                    if (Path.GetFileName(path) == pdbFileName)
                    {
                        return File.Exists(Path.Combine(Path.Combine(Directory.GetCurrentDirectory(), "TestAssets"), pdbFileName));
                    }
                    else
                    {
                        return File.Exists(path);
                    }
                });

                InstrumentationHelper instrumentationHelper = new InstrumentationHelper(new ProcessExitHandler(), new RetryHelper(), partialMockFileSystem.Object);
                string sample = Directory.GetFiles(Path.Combine(Directory.GetCurrentDirectory(), "TestAssets"), dllFileName).First();
                var loggerMock = new Mock<ILogger>();
                Instrumenter instrumenter = new Instrumenter(sample, "_75d9f96508d74def860a568f426ea4a4_instrumented", Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(), false, loggerMock.Object, instrumentationHelper, partialMockFileSystem.Object);
                Assert.True(instrumentationHelper.HasPdb(sample, out bool embedded));
                Assert.False(embedded);
                Assert.False(instrumenter.CanInstrument());
                loggerMock.Verify(l => l.LogVerbose(It.IsAny<string>()));
            }
        }

        [Fact]
        public void TestInstrument_MissingModule()
        {
            var loggerMock = new Mock<ILogger>();
            var instrumenter = new Instrumenter("test", "_test_instrumented", Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(), false, loggerMock.Object, _instrumentationHelper, new FileSystem());
            Assert.False(instrumenter.CanInstrument());
            loggerMock.Verify(l => l.LogWarning(It.IsAny<string>()));
        }

        [Fact]
        public void TestInstrument_AssemblyMarkedAsExcludeFromCodeCoverage()
        {
            Mock<FileSystem> partialMockFileSystem = new Mock<FileSystem>();
            partialMockFileSystem.CallBase = true;
            partialMockFileSystem.Setup(fs => fs.NewFileStream(It.IsAny<string>(), It.IsAny<FileMode>(), It.IsAny<FileAccess>())).Returns((string path, FileMode mode, FileAccess access) =>
            {
                return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            });
            var loggerMock = new Mock<ILogger>();

            string excludedbyattributeDll = Directory.GetFiles(Path.Combine(Directory.GetCurrentDirectory(), "TestAssets"), "coverlet.tests.projectsample.excludedbyattribute.dll").First();
            Instrumenter instrumenter = new Instrumenter(excludedbyattributeDll, "_xunit_excludedbyattribute", Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(), false, loggerMock.Object, _instrumentationHelper, partialMockFileSystem.Object);
            InstrumenterResult result = instrumenter.Instrument();
            Assert.Empty(result.Documents);
            loggerMock.Verify(l => l.LogVerbose(It.IsAny<string>()));
        }
    }
}
