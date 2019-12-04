using System;
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
                Assert.All(doc.Descendants().Attributes().Where(attr => attr.Name.LocalName.EndsWith("-rate")).Select(attr => attr.Value),
                value =>
                {
                    Assert.DoesNotContain(",", value);
                    Assert.Contains(".", value);
                    Assert.Equal(0.5, double.Parse(value, CultureInfo.InvariantCulture));
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
        public void TestReportWithTwoDifferentWindowsRoots()
        {
            CoverageResult result = new CoverageResult();
            result.Identifier = Guid.NewGuid().ToString();

            string absolutePath1;
            string absolutePath2;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                absolutePath1 = @"C:\projA\file.cs";
                absolutePath2 = @"E:\projB\file.cs";
            }
            else
            {
                absolutePath1 = @"/projA/file.cs";
                absolutePath2 = @"/projB/file.cs";
            }

            var classes = new Classes { { "Class", new Methods() } };
            var documents = new Documents { { absolutePath1, classes }, { absolutePath2, classes } };

            result.Modules = new Modules { { "Module", documents } };

            CoberturaReporter reporter = new CoberturaReporter();
            string report = reporter.Report(result);

            var doc = XDocument.Load(new MemoryStream(Encoding.UTF8.GetBytes(report)));
            var rootPaths = doc.Descendants().Elements().Where(tag => tag.Name.LocalName.Equals("source"))
                .Select(elem => elem.Value).ToList();
            var relativePaths = doc.Descendants().Elements().Where(tag => tag.Name.LocalName.Equals("class"))
                .Attributes("filename").Select(attr => attr.Value).ToList();

            Assert.Equal(absolutePath1, Path.Combine(rootPaths[0], relativePaths[0]));
            Assert.Equal(absolutePath2, Path.Combine(rootPaths[1], relativePaths[1]));
        }

        [Fact]
        public void TestReportWithSourcelinkPaths()
        {
            CoverageResult result = new CoverageResult { UseSourceLink = true, Identifier = Guid.NewGuid().ToString() };

            var absolutePath = @"https://raw.githubusercontent.com/johndoe/Coverlet/02c09baa8bfdee3b6cdf4be89bd98c8157b0bc08/Demo.cs";

            var classes = new Classes { { "Class", new Methods() } };
            var documents = new Documents { { absolutePath, classes } };

            result.Modules = new Modules { { "Module", documents } };

            CoberturaReporter reporter = new CoberturaReporter();
            string report = reporter.Report(result);

            var doc = XDocument.Load(new MemoryStream(Encoding.UTF8.GetBytes(report)));
            var fileNames = doc.Descendants().Elements().Where(tag => tag.Name.LocalName.Equals("class"))
                .Attributes("filename").Select(attr => attr.Value).ToList();

            Assert.Equal(absolutePath, fileNames[0]);
        }
    }
}