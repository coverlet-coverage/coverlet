// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

#nullable disable

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
            string actual = FileSystem.EscapeFileName(fileName);

            Assert.Equal(expected, actual);
        }
    }
}