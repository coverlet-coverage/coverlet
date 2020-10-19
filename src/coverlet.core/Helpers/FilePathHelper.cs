using Coverlet.Core.Abstractions;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;


namespace Coverlet.Core.Helpers
{
    internal class FilePathHelper : IFilePathHelper
    {
        public IEnumerable<string> GetBasePaths(IEnumerable<string> paths, bool useSourceLink)
        {
            /*
                 Workflow

                 Path1 c:\dir1\dir2\file1.cs
                 Path2 c:\dir1\file2.cs
                 Path3 e:\dir1\file2.cs

                 1) Search for root dir 
                    c:\ ->	c:\dir1\dir2\file1.cs
                            c:\dir1\file2.cs
                    e:\ ->	e:\dir1\file2.cs

                 2) Split path on directory separator i.e. for record c:\ ordered ascending by fragment elements
                     Path1 = [c:|dir1|file2.cs]
                     Path2 = [c:|dir1|dir2|file1.cs]

                 3)  Find longest shared path comparing indexes		 
                     Path1[0]    = Path2[0], ..., PathY[0]     -> add to final fragment list
                     Path1[n]    = Path2[n], ..., PathY[n]     -> add to final fragment list
                     Path1[n+1] != Path2[n+1], ..., PathY[n+1] -> break, Path1[n] was last shared fragment 		 

                 4) Concat created fragment list
            */
            if (useSourceLink)
            {
                return new[] { string.Empty };
            }

            return paths.GroupBy(Directory.GetDirectoryRoot).Select(group =>
            {
                var splittedPaths = group.Select(absolutePath => absolutePath.Split(Path.DirectorySeparatorChar))
                                         .OrderBy(absolutePath => absolutePath.Length).ToList();
                if (splittedPaths.Count == 1)
                {
                    return group.Key;
                }

                var basePathFragments = new List<string>();
                bool stopSearch = false;
                splittedPaths[0].Select((value, index) => (value, index)).ToList().ForEach(fragmentIndexPair =>
                {
                    if (stopSearch)
                    {
                        return;
                    }

                    if (splittedPaths.All(sp => fragmentIndexPair.value.Equals(sp[fragmentIndexPair.index])))
                    {
                        basePathFragments.Add(fragmentIndexPair.value);
                    }
                    else
                    {
                        stopSearch = true;
                    }
                });
                return string.Concat(string.Join(Path.DirectorySeparatorChar.ToString(), basePathFragments), Path.DirectorySeparatorChar);
            });
        }

        public string GetRelativePathFromBase(IEnumerable<string> basePaths, string path, bool useSourceLink)
        {
            if (useSourceLink)
            {
                return path;
            }

            foreach (var basePath in basePaths)
            {
                if (path.StartsWith(basePath))
                {
                    return path.Substring(basePath.Length);
                }
            }

            Debug.Assert(false, "Unexpected, we should find at least one path starts with one pre-build roots list");

            return path;
        }
    }
}
