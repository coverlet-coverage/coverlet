using System;
using Xunit;

namespace Coverlet.Core.Reporters.Tests
{
    public class JsonReporterTests
    {
        private CoverageResult _result;

        public JsonReporterTests()
        {
            _result = new CoverageResult();
            _result.Identifier = Guid.NewGuid().ToString();
            Lines lines = new Lines();
            lines.Add(1, new HitInfo { Hits = 1 });
            lines.Add(2, new HitInfo { Hits = 0 });
            Methods methods = new Methods();
            var methodString = "System.Void Coverlet.Core.Reporters.Tests.JsonReporterTests.TestReport()";
            methods.Add(methodString, new Method());
            methods[methodString].Lines = lines;
            Classes classes = new Classes();
            classes.Add("Coverlet.Core.Reporters.Tests.JsonReporterTests", methods);
            Documents documents = new Documents();
            documents.Add("doc.cs", classes);
            _result.Modules = new Modules();
            _result.Modules.Add("module", documents);
        }

        [Fact]
        public void TestReport()
        {
            JsonReporter reporter = new JsonReporter();
            Assert.NotEqual("{\n}", reporter.Report(_result));
            Assert.NotEqual(string.Empty, reporter.Report(_result));
        }

        [Fact]
        public void TestRead()
        {
            JsonReporter reporter = new JsonReporter();

            var json = reporter.Report(_result);
            var result = reporter.Read(json);

            Assert.Equal(1, result.Modules["module"]["doc.cs"]["Coverlet.Core.Reporters.Tests.JsonReporterTests"]["System.Void Coverlet.Core.Reporters.Tests.JsonReporterTests.TestReport()"].Lines[1].Hits);
            Assert.Equal(0, result.Modules["module"]["doc.cs"]["Coverlet.Core.Reporters.Tests.JsonReporterTests"]["System.Void Coverlet.Core.Reporters.Tests.JsonReporterTests.TestReport()"].Lines[2].Hits);
            Assert.NotEqual(_result.Identifier, result.Identifier);
        }
    }
}