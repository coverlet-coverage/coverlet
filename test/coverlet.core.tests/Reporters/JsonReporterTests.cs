using System;
using Xunit;

namespace Coverlet.Core.Reporters.Tests
{
    public class JsonReporterTests
    {
        [Fact]
        public void TestReport()
        {
            CoverageResult result = new CoverageResult();
            result.Identifier = Guid.NewGuid().ToString();

            Lines lines = new Lines();
            lines.Add(1, 1);
            lines.Add(2, 0);

            Methods methods = new Methods();
            var methodString = "System.Void Coverlet.Core.Reporters.Tests.JsonReporterTests.TestReport()";
            methods.Add(methodString, new Method());
            methods[methodString].Lines = lines;

            Classes classes = new Classes();
            classes.Add("Coverlet.Core.Reporters.Tests.JsonReporterTests", methods);

            Documents documents = new Documents();
            documents.Add("doc.cs", classes);

            result.Modules = new Modules();
            result.Modules.Add("module", documents);

            JsonReporter reporter = new JsonReporter();
            Assert.NotEqual("{\n}", reporter.Report(result));
            Assert.NotEqual(string.Empty, reporter.Report(result));
        }
    }
}