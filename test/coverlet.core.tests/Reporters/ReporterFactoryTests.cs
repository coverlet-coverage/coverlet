using Coverlet.Core.Helpers;
using Coverlet.Core.Reporters;
using Xunit;

namespace Coverlet.Core.Reporters.Tests
{
    public class ReporterFactoryTests
    {
        [Fact]
        public void TestCreateReporter()
        {
            var filePathHelper = new FilePathHelper();
            Assert.Equal(typeof(JsonReporter), new ReporterFactory("json", filePathHelper).CreateReporter().GetType());
            Assert.Equal(typeof(LcovReporter), new ReporterFactory("lcov", filePathHelper).CreateReporter().GetType());
            Assert.Equal(typeof(OpenCoverReporter), new ReporterFactory("opencover", filePathHelper).CreateReporter().GetType());
            Assert.Equal(typeof(CoberturaReporter), new ReporterFactory("cobertura", filePathHelper).CreateReporter().GetType());
            Assert.Equal(typeof(TeamCityReporter), new ReporterFactory("teamcity", filePathHelper).CreateReporter().GetType());
            Assert.Null(new ReporterFactory("", filePathHelper).CreateReporter());
        }
    }
}