using Coverlet.Core.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Xunit;

namespace Coverlet.Core.Helpers.Tests
{
    public class FilePathHelperTests
    {


        [Fact]
        public void TestGetBasePaths_UseSourceLink_False()
        {
            var absolutePaths = new List<string>();
            var expectedBasePaths = new List<string>();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                absolutePaths.Add(@"C:\projA\dir1\dir10\file1.cs");
                absolutePaths.Add(@"C:\projA\dir1\dir10\file2.cs");
                absolutePaths.Add(@"C:\projA\dir1\file3.cs");
                absolutePaths.Add(@"E:\projB\dir1\dir10\file4.cs");
                absolutePaths.Add(@"E:\projB\dir2\file5.cs");
                absolutePaths.Add(@"F:\file6.cs");
                absolutePaths.Add(@"F:\");
                absolutePaths.Add(@"c:\git\coverletissue\localpackagetest\deterministicbuild\ClassLibrary1\Class1.cs");
                absolutePaths.Add(@"c:\git\coverletissue\localpackagetest\deterministicbuild\ClassLibrary2\Class1.cs");

                expectedBasePaths.Add(@"C:\projA\dir1\");
                expectedBasePaths.Add(@"E:\projB\");
                expectedBasePaths.Add(@"F:\");
                expectedBasePaths.Add(@"c:\git\coverletissue\localpackagetest\deterministicbuild\");
            }
            else
            {
                absolutePaths.Add(@"/projA/dir1/dir10/file1.cs");
                absolutePaths.Add(@"/projA/dir1/file2.cs");
                absolutePaths.Add(@"/projA/dir1/file3.cs");
                absolutePaths.Add(@"/projA/dir2/file4.cs");
                absolutePaths.Add(@"/projA/dir2/file5.cs");
                absolutePaths.Add(@"/file1.cs");
                absolutePaths.Add(@"/");
                absolutePaths.Add(@"/git/coverletissue/localpackagetest/deterministicbuild/ClassLibrary1/Class1.cs");
                absolutePaths.Add(@"/git/coverletissue/localpackagetest/deterministicbuild/ClassLibrary2/Class1.cs");


                expectedBasePaths.Add(@"/");
            }

            var filePathHelper = new FilePathHelper();

            var basePaths = filePathHelper.GetBasePaths(absolutePaths, false);

            Assert.Equal(expectedBasePaths.OrderBy(x => x), basePaths.OrderBy(x => x));
        }

        [Fact]
        public void TestGetBasePaths_UseSourceLink_True()
        {
            var absolutePaths = new List<string>();
            var expectedBasePaths = new List<string>() { string.Empty };

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                absolutePaths.Add(@"C:\projA\dir1\dir10\file1.cs");
                absolutePaths.Add(@"E:\projB\dir1\dir10\file4.cs");
                absolutePaths.Add(@"F:\");
                absolutePaths.Add(@"c:\git\coverletissue\localpackagetest\deterministicbuild\ClassLibrary1\Class1.cs");
            }
            else
            {
                absolutePaths.Add(@"/projA/dir1/dir10/file1.cs");
                absolutePaths.Add(@"/file1.cs");
                absolutePaths.Add(@"/");
                absolutePaths.Add(@"/git/coverletissue/localpackagetest/deterministicbuild/ClassLibrary1/Class1.cs");
            }

            var filePathHelper = new FilePathHelper();

            var basePaths = filePathHelper.GetBasePaths(absolutePaths, true);

            Assert.Equal(expectedBasePaths, basePaths);

        }

    }
}
