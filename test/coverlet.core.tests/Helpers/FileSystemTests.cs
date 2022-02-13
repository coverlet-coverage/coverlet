using Coverlet.Core.Helpers;
using Xunit;

namespace Coverlet.Core.Helpers.Tests
{
    public class FileSystemTests
    {
        [Theory]
        [InlineData(null, null)]
        [InlineData("", "")]
        [InlineData("filename.cs", "filename.cs")]
        [InlineData("filename{T}.cs", "filename{{T}}.cs")]
        public void TestEscapeFileName(string fileName, string expected)
        {
            var actual = FileSystem.EscapeFileName(fileName);

            Assert.Equal(expected, actual);
        }
    }
}