using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml;
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
            documents.Add("doc.cs", classes);

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
    }
}