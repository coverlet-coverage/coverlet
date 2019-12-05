using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Coverlet.Core;
using Xunit.Sdk;

namespace Coverlet.Integration.Tests
{
    internal static class AssertHelper
    {
        public static Classes Document(this Modules modules, string docName)
        {
            if (docName is null)
            {
                throw new ArgumentNullException(nameof(docName));
            }


            foreach (KeyValuePair<string, Documents> module in modules)
            {
                foreach (KeyValuePair<string, Classes> document in module.Value)
                {
                    if (Path.GetFileName(document.Key) == docName)
                    {
                        return document.Value;
                    }
                }
            }

            throw new XunitException($"Document not found '{docName}'");
        }

        public static Methods Class(this Classes classes, string className)
        {
            if (className is null)
            {
                throw new ArgumentNullException(nameof(className));
            }


            foreach (KeyValuePair<string, Methods> @class in classes)
            {
                if (@class.Key == className)
                {
                    return @class.Value;
                }
            }

            throw new XunitException($"Document not found '{className}'");
        }

        public static Method Method(this Methods methods, string methodName)
        {
            if (methodName is null)
            {
                throw new ArgumentNullException(nameof(methodName));
            }


            foreach (KeyValuePair<string, Method> method in methods)
            {
                if (method.Key.Contains(methodName))
                {
                    return method.Value;
                }
            }

            throw new XunitException($"Document not found '{methodName}'");
        }

        public static void AssertLinesCovered(this Method method, params (int line, int hits)[] lines)
        {
            if (lines is null)
            {
                throw new ArgumentNullException(nameof(lines));
            }

            List<int> linesToCover = new List<int>(lines.Select(l => l.line));

            foreach (KeyValuePair<int, int> line in method.Lines)
            {
                foreach ((int lineToCheck, int expectedHits) in lines)
                {
                    if (line.Key == lineToCheck)
                    {
                        linesToCover.Remove(line.Key);
                        if (line.Value != expectedHits)
                        {
                            throw new XunitException($"Unexpected hits expected line: {lineToCheck} hits: {expectedHits} actual hits: {line.Value}");
                        }
                    }
                }
            }

            if (linesToCover.Count != 0)
            {
                throw new XunitException($"Not all requested line found, {linesToCover.Select(l => l.ToString()).Aggregate((a, b) => $"{a}, {b}")}");
            }
        }
    }
}
