using System;
using Xunit;

namespace Coverlet.Core.Reporters.Tests
{
    public class ReporterFactoryTests
    {
        [Fact]
        public void TestCreateReportersWithSingleFormat()
        {
            Assert.Collection(
                new ReporterFactory("json").CreateReporters(),
                reporter => Assert.IsType<JsonReporter>(reporter));
            Assert.Collection(
                new ReporterFactory("lcov").CreateReporters(),
                reporter => Assert.IsType<LcovReporter>(reporter));
            Assert.Collection(
                new ReporterFactory("opencover").CreateReporters(),
                reporter => Assert.IsType<OpenCoverReporter>(reporter));
            Assert.Collection(
                new ReporterFactory("cobertura").CreateReporters(),
                reporter => Assert.IsType<CoberturaReporter>(reporter));
            Assert.Empty(new ReporterFactory("").CreateReporters());
        }

        [Fact]
        public void TestCreateReportersWithMultipleFormats()
        {
            Assert.Collection(
                new ReporterFactory("json,lcov").CreateReporters(),
                reporter => Assert.IsType<JsonReporter>(reporter),
                reporter => Assert.IsType<LcovReporter>(reporter));
            Assert.Collection(
                new ReporterFactory("json,lcov,opencover").CreateReporters(),
                reporter => Assert.IsType<JsonReporter>(reporter),
                reporter => Assert.IsType<LcovReporter>(reporter),
                reporter => Assert.IsType<OpenCoverReporter>(reporter));
            Assert.Collection(
                new ReporterFactory("json,lcov,opencover,cobertura").CreateReporters(),
                reporter => Assert.IsType<JsonReporter>(reporter),
                reporter => Assert.IsType<LcovReporter>(reporter),
                reporter => Assert.IsType<OpenCoverReporter>(reporter),
                reporter => Assert.IsType<CoberturaReporter>(reporter));
        }
    }
}