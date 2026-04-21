// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Xml.Linq;
using Coverlet.Core.Abstractions;
using Coverlet.Core.Reporters;
using Moq;
using Xunit;

namespace Coverlet.Core.Tests.Reporters
{
  public class CoberturaReporterTests
  {
    [Fact]
    public void TestReport()
    {
      var result = new CoverageResult();
      result.Identifier = Guid.NewGuid().ToString();

      var lines = new Lines
      {
        { 1, 1 },
        { 2, 0 }
      };

      var branches = new Branches
      {
        new BranchInfo { Line = 1, Hits = 1, Offset = 23, EndOffset = 24, Path = 0, Ordinal = 1 },
        new BranchInfo { Line = 1, Hits = 0, Offset = 23, EndOffset = 27, Path = 1, Ordinal = 2 }
      };

      var methods = new Methods();
      string methodString = "System.Void Coverlet.Core.Reporters.Tests.CoberturaReporterTests::TestReport()";
      methods.Add(methodString, new Method());
      methods[methodString].Lines = lines;
      methods[methodString].Branches = branches;

      var classes = new Classes
      {
        { "Coverlet.Core.Reporters.Tests.CoberturaReporterTests", methods }
      };

      var documents = new Documents();

      if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
      {
        documents.Add(@"C:\doc.cs", classes);
      }
      else
      {
        documents.Add(@"/doc.cs", classes);
      }

      result.Modules = new Modules
      {
        { "module", documents }
      };
      result.Parameters = new CoverageParameters();

      CultureInfo currentCulture = Thread.CurrentThread.CurrentCulture;
      Thread.CurrentThread.CurrentCulture = new CultureInfo("it-IT");
      try
      {
        // Assert conversion behavior to be sure to be in a Italian culture context
        // where decimal char is comma.
        Assert.Equal("1,5", (1.5).ToString());

        var reporter = new CoberturaReporter();
        string report = reporter.Report(result, new Mock<ISourceRootTranslator>().Object);

        Assert.NotEmpty(report);

        var doc = XDocument.Load(new StringReader(report));

        IEnumerable<XAttribute> matchingRateAttributes = doc.Descendants().Attributes().Where(attr => attr.Name.LocalName.EndsWith("-rate"));
        IEnumerable<string> rateParentNodeNames = matchingRateAttributes.Select(attr => attr.Parent.Name.LocalName);
        Assert.Contains("package", rateParentNodeNames);
        Assert.Contains("class", rateParentNodeNames);
        Assert.Contains("method", rateParentNodeNames);
        Assert.All(matchingRateAttributes.Select(attr => attr.Value),
        value =>
        {
          Assert.DoesNotContain(",", value);
          Assert.Contains(".", value);
          Assert.Equal(0.5, double.Parse(value, CultureInfo.InvariantCulture));
        });

        IEnumerable<XAttribute> matchingComplexityAttributes = doc.Descendants().Attributes().Where(attr => attr.Name.LocalName.Equals("complexity"));
        IEnumerable<string> complexityParentNodeNames = matchingComplexityAttributes.Select(attr => attr.Parent.Name.LocalName);
        Assert.Contains("package", complexityParentNodeNames);
        Assert.Contains("class", complexityParentNodeNames);
        Assert.Contains("method", complexityParentNodeNames);
        Assert.All(matchingComplexityAttributes.Select(attr => attr.Value),
        value =>
        {
          Assert.Equal(branches.Count, int.Parse(value, CultureInfo.InvariantCulture));
        });
      }
      finally
      {
        Thread.CurrentThread.CurrentCulture = currentCulture;
      }
    }

    [Fact]
    public void CoberturaTestReportDoesNotContainBom()
    {
      var result = new CoverageResult { Parameters = new CoverageParameters(), Identifier = Guid.NewGuid().ToString() };
      var documents = new Documents();
      var classes = new Classes { { "Class", new Methods() } };

      documents.Add(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? @"C:\doc.cs" : @"/doc.cs", classes);

      result.Modules = new Modules { { "Module", documents } };

      var reporter = new CoberturaReporter();
      string report = reporter.Report(result, new Mock<ISourceRootTranslator>().Object);

      byte[] preamble = Encoding.UTF8.GetBytes(report)[..3];
      Assert.NotEqual(Encoding.UTF8.GetPreamble(), preamble);
    }

    [Theory]
    [InlineData(
        "Test.SearchRequest::pb::Google.Protobuf.IMessage.get_Descriptor()",
        "Google.Protobuf.IMessage.get_Descriptor",
        "()")]
    [InlineData(
        "Test.SearchRequest::pb::Google.Protobuf.IMessage.get_Descriptor(int i)",
        "Google.Protobuf.IMessage.get_Descriptor",
        "(int i)")]
    public void TestEnsureParseMethodStringCorrectly(
        string methodString,
        string expectedMethodName,
        string expectedSignature)
    {
      var result = new CoverageResult();
      result.Parameters = new CoverageParameters();
      result.Identifier = Guid.NewGuid().ToString();

      var lines = new Lines
      {
        { 1, 1 }
      };

      var branches = new Branches
      {
        new BranchInfo { Line = 1, Hits = 1, Offset = 23, EndOffset = 24, Path = 0, Ordinal = 1 }
      };

      var methods = new Methods
      {
        { methodString, new Method() }
      };
      methods[methodString].Lines = lines;
      methods[methodString].Branches = branches;

      var classes = new Classes
      {
        { "Google.Protobuf.Reflection.MessageDescriptor", methods }
      };

      var documents = new Documents();
      if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
      {
        documents.Add(@"C:\doc.cs", classes);
      }
      else
      {
        documents.Add(@"/doc.cs", classes);
      }

      result.Modules = new Modules
      {
        { "module", documents }
      };

      var reporter = new CoberturaReporter();
      string report = reporter.Report(result, new Mock<ISourceRootTranslator>().Object);

      Assert.NotEmpty(report);

      var doc = XDocument.Load(new StringReader(report));

      var methodAttrs = doc.Descendants()
          .Where(o => o.Name.LocalName == "method")
          .Attributes()
          .ToDictionary(o => o.Name.LocalName, o => o.Value);
      Assert.Equal(expectedMethodName, methodAttrs["name"]);
      Assert.Equal(expectedSignature, methodAttrs["signature"]);
    }

    [Fact]
    public void TestReportWithDifferentDirectories()
    {
      var result = new CoverageResult();
      result.Parameters = new CoverageParameters();
      result.Identifier = Guid.NewGuid().ToString();

      string absolutePath1;
      string absolutePath2;
      string absolutePath3;
      string absolutePath4;
      string absolutePath5;
      string absolutePath6;
      string absolutePath7;
      string absolutePath8;
      string absolutePath9;

      if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
      {
        absolutePath1 = @"C:\projA\dir1\dir10\file1.cs";
        absolutePath2 = @"C:\projA\dir1\dir10\file2.cs";
        absolutePath3 = @"C:\projA\dir1\file3.cs";
        absolutePath4 = @"E:\projB\dir1\dir10\file4.cs";
        absolutePath5 = @"E:\projB\dir2\file5.cs";
        absolutePath6 = @"F:\file6.cs";
        absolutePath7 = @"F:\";
        absolutePath8 = @"c:\git\coverletissue\localpackagetest\deterministicbuild\ClassLibrary1\Class1.cs";
        absolutePath9 = @"c:\git\coverletissue\localpackagetest\deterministicbuild\ClassLibrary2\Class1.cs";
      }
      else
      {
        absolutePath1 = @"/projA/dir1/dir10/file1.cs";
        absolutePath2 = @"/projA/dir1/file2.cs";
        absolutePath3 = @"/projA/dir1/file3.cs";
        absolutePath4 = @"/projA/dir2/file4.cs";
        absolutePath5 = @"/projA/dir2/file5.cs";
        absolutePath6 = @"/file1.cs";
        absolutePath7 = @"/";
        absolutePath8 = @"/git/coverletissue/localpackagetest/deterministicbuild/ClassLibrary1/Class1.cs";
        absolutePath9 = @"/git/coverletissue/localpackagetest/deterministicbuild/ClassLibrary2/Class1.cs";
      }

      var classes = new Classes { { "Class", new Methods() } };
      var documents = new Documents {
                                            { absolutePath1, classes },
                                            { absolutePath2, classes },
                                            { absolutePath3, classes },
                                            { absolutePath4, classes },
                                            { absolutePath5, classes },
                                            { absolutePath6, classes },
                                            { absolutePath7, classes },
                                            { absolutePath8, classes },
                                            { absolutePath9, classes }
            };

      result.Modules = new Modules { { "Module", documents } };

      var reporter = new CoberturaReporter();
      string report = reporter.Report(result, new Mock<ISourceRootTranslator>().Object);

      var doc = XDocument.Load(new StringReader(report));

      var basePaths = doc.Element("coverage").Element("sources").Elements().Select(e => e.Value).ToList();
      var relativePaths = doc.Element("coverage").Element("packages").Element("package")
          .Element("classes").Elements().Select(e => e.Attribute("filename").Value).ToList();

      // After the path-normalization fix, basePaths use forward slashes on all platforms.
      // Reconstruct using string concatenation (basePath always ends with '/') and
      // compare against forward-slash-normalized absolute paths.
      var possiblePaths = new List<string>();
      foreach (string basePath in basePaths)
      {
        foreach (string relativePath in relativePaths)
        {
          possiblePaths.Add(basePath + relativePath);
        }
      }

      Assert.Contains(absolutePath1.Replace('\\', '/'), possiblePaths);
      Assert.Contains(absolutePath2.Replace('\\', '/'), possiblePaths);
      Assert.Contains(absolutePath3.Replace('\\', '/'), possiblePaths);
      Assert.Contains(absolutePath4.Replace('\\', '/'), possiblePaths);
      Assert.Contains(absolutePath5.Replace('\\', '/'), possiblePaths);
      Assert.Contains(absolutePath6.Replace('\\', '/'), possiblePaths);
      Assert.Contains(absolutePath7.Replace('\\', '/'), possiblePaths);
      Assert.Contains(absolutePath8.Replace('\\', '/'), possiblePaths);
      Assert.Contains(absolutePath9.Replace('\\', '/'), possiblePaths);
    }

    [Fact]
    public void TestReportWithSourcelinkPaths()
    {
      var result = new CoverageResult { Parameters = new CoverageParameters() { UseSourceLink = true }, Identifier = Guid.NewGuid().ToString() };

      string absolutePath =
          @"https://raw.githubusercontent.com/johndoe/Coverlet/02c09baa8bfdee3b6cdf4be89bd98c8157b0bc08/Demo.cs";

      var classes = new Classes { { "Class", new Methods() } };
      var documents = new Documents { { absolutePath, classes } };

      result.Modules = new Modules { { "Module", documents } };

      var reporter = new CoberturaReporter();
      string report = reporter.Report(result, new Mock<ISourceRootTranslator>().Object);

      var doc = XDocument.Load(new StringReader(report));

      string fileName = doc.Element("coverage").Element("packages").Element("package").Element("classes").Elements()
          .Select(e => e.Attribute("filename").Value).Single();

      Assert.Equal(absolutePath, fileName);
    }

    // ── Issue #1723: path-separator normalization ──────────────────────────────

    private static CoverageResult BuildResult(
        IEnumerable<(string path, string className)> documents,
        CoverageParameters parameters = null)
    {
      var methods = new Methods();
      string methodKey = "System.Void Coverlet.Core.Reporters.Tests::TestMethod()";
      methods.Add(methodKey, new Method());
      methods[methodKey].Lines = new Lines { { 1, 1 } };

      var docs = new Documents();
      foreach ((string path, string className) in documents)
      {
        var classes = new Classes { { className, methods } };
        docs[path] = classes;
      }

      return new CoverageResult
      {
        Identifier = Guid.NewGuid().ToString(),
        Modules = new Modules { { "Module", docs } },
        Parameters = parameters ?? new CoverageParameters()
      };
    }

    [Fact]
    public void Report_NonSourceLink_WindowsStylePaths_FilenameAttributeUsesForwardSlashes()
    {
      // Windows-style paths simulate coverage data collected on Windows.
      // The reporter must emit forward slashes regardless of the host OS.
      CoverageResult result = BuildResult([
        (@"C:\projA\src\Foo.cs", "Foo"),
        (@"C:\projA\src\Sub\Bar.cs", "Bar")
      ]);

      string report = new CoberturaReporter().Report(result, new Mock<ISourceRootTranslator>().Object);

      var doc = XDocument.Load(new StringReader(report));
      IEnumerable<string> fileNames = doc.Descendants("class").Select(c => c.Attribute("filename").Value);

      Assert.All(fileNames, fn => Assert.DoesNotContain("\\", fn));
    }

    [Fact]
    public void Report_NonSourceLink_WindowsStylePaths_SourceElementUsesForwardSlashes()
    {
      CoverageResult result = BuildResult([
        (@"C:\projA\src\Foo.cs", "Foo"),
        (@"C:\projA\src\Sub\Bar.cs", "Bar")
      ]);

      string report = new CoberturaReporter().Report(result, new Mock<ISourceRootTranslator>().Object);

      var doc = XDocument.Load(new StringReader(report));
      IEnumerable<string> sources = doc.Descendants("source").Select(s => s.Value);

      Assert.All(sources, s => Assert.DoesNotContain("\\", s));
    }

    [Fact]
    public void Report_NonSourceLink_SingleWindowsStylePath_FilenameUsesForwardSlashes()
    {
      // Single document hits the splittedPaths.Count == 1 branch in GetBasePaths.
      CoverageResult result = BuildResult([(@"C:\projA\Foo.cs", "Foo")]);

      string report = new CoberturaReporter().Report(result, new Mock<ISourceRootTranslator>().Object);

      var doc = XDocument.Load(new StringReader(report));
      string fileName = doc.Descendants("class").Select(c => c.Attribute("filename").Value).Single();

      Assert.DoesNotContain("\\", fileName);
    }

    [Fact]
    public void Report_NonSourceLink_MultipleWindowsRoots_AllSourceElementsUseForwardSlashes()
    {
      // Documents under two different drive roots produce two <source> elements.
      CoverageResult result = BuildResult([
        (@"C:\projA\Foo.cs", "Foo"),
        (@"E:\projB\Bar.cs", "Bar")
      ]);

      string report = new CoberturaReporter().Report(result, new Mock<ISourceRootTranslator>().Object);

      var doc = XDocument.Load(new StringReader(report));
      IEnumerable<string> sources = doc.Descendants("source").Select(s => s.Value);

      Assert.All(sources, s => Assert.DoesNotContain("\\", s));
    }

    [Fact]
    public void Report_NonSourceLink_WindowsStylePaths_ReconstructedPathMatchesDocument()
    {
      // basePath + relativePath must reconstruct the original document path (forward-slash normalized).
      const string documentPath = @"C:\projA\src\Sub\Foo.cs";
      CoverageResult result = BuildResult([
        (documentPath, "Foo"),
        (@"C:\projA\src\Bar.cs", "Bar")
      ]);

      string report = new CoberturaReporter().Report(result, new Mock<ISourceRootTranslator>().Object);

      var doc = XDocument.Load(new StringReader(report));
      string source = doc.Descendants("source").Select(s => s.Value).Single();
      string fileName = doc.Descendants("class")
          .Where(c => c.Attribute("filename").Value.EndsWith("Foo.cs"))
          .Select(c => c.Attribute("filename").Value).Single();

      Assert.Equal(documentPath.Replace('\\', '/'), source + fileName);
    }

    [Fact]
    public void Report_NonSourceLink_CaseDifferingFragments_FilenameIsRelativeNotAbsolute()
    {
      // When path fragments differ only in casing, OrdinalIgnoreCase must correctly
      // identify the common base and strip it, returning a short relative filename.
      CoverageResult result = BuildResult([
        (@"C:\ProjA\src\File1.cs", "File1"),
        (@"C:\PROJA\src\File2.cs", "File2")
      ]);

      string report = new CoberturaReporter().Report(result, new Mock<ISourceRootTranslator>().Object);

      var doc = XDocument.Load(new StringReader(report));
      IEnumerable<string> fileNames = doc.Descendants("class").Select(c => c.Attribute("filename").Value);

      // Both filenames must be relative (not start with a drive letter).
      Assert.All(fileNames, fn => Assert.DoesNotContain(":", fn));
    }

    [Fact]
    public void Report_UseSourceLink_FilenamePassesThroughUnchanged()
    {
      // SourceLink paths are URLs — the reporter must return them as-is.
      const string sourceUrl = "https://raw.githubusercontent.com/org/repo/abc123/src/Foo.cs";
      CoverageResult result = BuildResult(
          [(sourceUrl, "Foo")],
          new CoverageParameters { UseSourceLink = true });

      string report = new CoberturaReporter().Report(result, new Mock<ISourceRootTranslator>().Object);

      var doc = XDocument.Load(new StringReader(report));
      string fileName = doc.Descendants("class").Select(c => c.Attribute("filename").Value).Single();

      Assert.Equal(sourceUrl, fileName);
    }

    [Fact]
    public void Report_DeterministicReport_FilenameFromTranslator()
    {
      // When DeterministicReport = true, the filename must come from ResolveDeterministicPath.
      const string documentPath = @"C:\projA\src\Foo.cs";
      const string deterministicPath = "/_/src/Foo.cs";

      var mockTranslator = new Mock<ISourceRootTranslator>();
      mockTranslator.Setup(t => t.ResolveDeterministicPath(documentPath)).Returns(deterministicPath);

      CoverageResult result = BuildResult(
          [(documentPath, "Foo")],
          new CoverageParameters { DeterministicReport = true });

      string report = new CoberturaReporter().Report(result, mockTranslator.Object);

      var doc = XDocument.Load(new StringReader(report));
      string fileName = doc.Descendants("class").Select(c => c.Attribute("filename").Value).Single();

      Assert.Equal(deterministicPath, fileName);
      mockTranslator.Verify(t => t.ResolveDeterministicPath(documentPath), Times.Once);
    }
  }
}
