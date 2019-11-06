using System;
using System.IO;
using Coverlet.Core.Helpers;
using Coverlet.Core.Abstracts;
using Moq;
using Xunit;

namespace Coverlet.Core.Tests
{
    public class ExclusionsFromFileHelperTests
    {
        private readonly ExclusionsFromFileHelper _sut;
        private readonly Mock<IFileSystem> _filesSystemMock = new Mock<IFileSystem>();
        private readonly Mock<ILogger> _loggerMock = new Mock<ILogger>();
        
        public ExclusionsFromFileHelperTests()
        {
            _sut = new ExclusionsFromFileHelper(_filesSystemMock.Object);
            _sut.Init(_loggerMock.Object);
        }

        [Fact]
        public void TestImportExclusionsFromFile()
        {
            var expected = new[] { "../dir1/class1.cs", "../dir2/*.cs", "../dir3/**/*.cs" };
            _filesSystemMock.Setup(m => m.ReadAllLines(It.IsAny<string>())).Returns(expected);
            var path = Guid.NewGuid().ToString();

            var actual = _sut.ImportExclusionsFromFile(path);

            Assert.Equal(expected, actual);
        }

        [Theory]
        [MemberData(nameof(Data))]
        public void TestImportExclusionsFromFileReturnsEmptyWhenFileSystemThrowsException<T>(T exception) where T: Exception
        {
            var expected = new string[0];
            _filesSystemMock.Setup(m => m.ReadAllLines(It.IsAny<string>())).Throws(exception);
            var path = Guid.NewGuid().ToString();

            var actual = _sut.ImportExclusionsFromFile(path);

            Assert.Equal(expected, actual);
        }

        public static TheoryData<Exception> Data => new TheoryData<Exception>
        {
            new ArgumentException(),
            new ArgumentNullException(),
            new PathTooLongException(),
            new DirectoryNotFoundException(),
            new IOException(),
            new UnauthorizedAccessException(),
            new FileNotFoundException(),
            new NotSupportedException(),
            new System.Security.SecurityException()
        };
    }
}
