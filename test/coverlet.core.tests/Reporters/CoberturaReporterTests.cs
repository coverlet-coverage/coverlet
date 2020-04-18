using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Xml.Linq;
using Xunit;

namespace Coverlet.Core.Reporters.Tests
{
    public class CoberturaReporterTests
    {
        [Fact]
        public void TestReport()
        {
            CoverageResult result = new CoverageResult();
            result.Identifier = Guid.NewGuid().ToString();

            Lines lines = new Lines();
            lines.Add(1, 1);
            lines.Add(2, 0);

            Branches branches = new Branches();
            branches.Add(new BranchInfo { Line = 1, Hits = 1, Offset = 23, EndOffset = 24, Path = 0, Ordinal = 1 });
            branches.Add(new BranchInfo { Line = 1, Hits = 0, Offset = 23, EndOffset = 27, Path = 1, Ordinal = 2 });

            Methods methods = new Methods();
            var methodString = "System.Void Coverlet.Core.Reporters.Tests.CoberturaReporterTests::TestReport()";
            methods.Add(methodString, new Method());
            methods[methodString].Lines = lines;
            methods[methodString].Branches = branches;

            Classes classes = new Classes();
            classes.Add("Coverlet.Core.Reporters.Tests.CoberturaReporterTests", methods);

            Documents documents = new Documents();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                documents.Add(@"C:\doc.cs", classes);
            }
            else
            {
                documents.Add(@"/doc.cs", classes);
            }

            result.Modules = new Modules();
            result.Modules.Add("module", documents);

            CultureInfo currentCulture = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = new CultureInfo("it-IT");
            try
            {
                // Assert conversion behaviour to be sure to be in a Italian culture context
                // where decimal char is comma.
                Assert.Equal("1,5", (1.5).ToString());

                CoberturaReporter reporter = new CoberturaReporter();
                string report = reporter.Report(result);

                Assert.NotEmpty(report);

                var doc = XDocument.Load(new MemoryStream(Encoding.UTF8.GetBytes(report)));

                var matchingRateAttributes = doc.Descendants().Attributes().Where(attr => attr.Name.LocalName.EndsWith("-rate"));
                var rateParentNodeNames = matchingRateAttributes.Select(attr => attr.Parent.Name.LocalName);
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

                var matchingComplexityAttributes = doc.Descendants().Attributes().Where(attr => attr.Name.LocalName.Equals("complexity"));
                var complexityParentNodeNames = matchingComplexityAttributes.Select(attr => attr.Parent.Name.LocalName);
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
            CoverageResult result = new CoverageResult();
            result.Identifier = Guid.NewGuid().ToString();

            Lines lines = new Lines();
            lines.Add(1, 1);

            Branches branches = new Branches();
            branches.Add(new BranchInfo { Line = 1, Hits = 1, Offset = 23, EndOffset = 24, Path = 0, Ordinal = 1 });

            Methods methods = new Methods();
            methods.Add(methodString, new Method());
            methods[methodString].Lines = lines;
            methods[methodString].Branches = branches;

            Classes classes = new Classes();
            classes.Add("Google.Protobuf.Reflection.MessageDescriptor", methods);

            Documents documents = new Documents();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                documents.Add(@"C:\doc.cs", classes);
            }
            else
            {
                documents.Add(@"/doc.cs", classes);
            }

            result.Modules = new Modules();
            result.Modules.Add("module", documents);

            CoberturaReporter reporter = new CoberturaReporter();
            string report = reporter.Report(result);

            Assert.NotEmpty(report);

            var doc = XDocument.Load(new MemoryStream(Encoding.UTF8.GetBytes(report)));
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
            CoverageResult result = new CoverageResult();
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

            CoberturaReporter reporter = new CoberturaReporter();
            string report = reporter.Report(result);

            var doc = XDocument.Load(new MemoryStream(Encoding.UTF8.GetBytes(report)));

            List<string> basePaths = doc.Element("coverage").Element("sources").Elements().Select(e => e.Value).ToList();
            List<string> relativePaths = doc.Element("coverage").Element("packages").Element("package")
                .Element("classes").Elements().Select(e => e.Attribute("filename").Value).ToList();

            List<string> possiblePaths = new List<string>();
            foreach (string basePath in basePaths)
            {
                foreach (string relativePath in relativePaths)
                {
                    possiblePaths.Add(Path.Combine(basePath, relativePath));
                }
            }

            Assert.Contains(absolutePath1, possiblePaths);
            Assert.Contains(absolutePath2, possiblePaths);
            Assert.Contains(absolutePath3, possiblePaths);
            Assert.Contains(absolutePath4, possiblePaths);
            Assert.Contains(absolutePath5, possiblePaths);
            Assert.Contains(absolutePath6, possiblePaths);
            Assert.Contains(absolutePath7, possiblePaths);
            Assert.Contains(absolutePath8, possiblePaths);
            Assert.Contains(absolutePath9, possiblePaths);
        }

        [Fact]
        public void TestReportWithSourcelinkPaths()
        {
            CoverageResult result = new CoverageResult { UseSourceLink = true, Identifier = Guid.NewGuid().ToString() };

            var absolutePath =
                @"https://raw.githubusercontent.com/johndoe/Coverlet/02c09baa8bfdee3b6cdf4be89bd98c8157b0bc08/Demo.cs";

            var classes = new Classes { { "Class", new Methods() } };
            var documents = new Documents { { absolutePath, classes } };

            result.Modules = new Modules { { "Module", documents } };

            CoberturaReporter reporter = new CoberturaReporter();
            string report = reporter.Report(result);

            var doc = XDocument.Load(new MemoryStream(Encoding.UTF8.GetBytes(report)));
            var fileName = doc.Element("coverage").Element("packages").Element("package").Element("classes").Elements()
                .Select(e => e.Attribute("filename").Value).Single();

            Assert.Equal(absolutePath, fileName);
        }
    }
}