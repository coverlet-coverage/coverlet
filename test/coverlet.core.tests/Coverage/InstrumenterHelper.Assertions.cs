using Coverlet.Core.Instrumentation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Xunit.Sdk;

namespace Coverlet.Core.Tests
{
    [Flags]
    public enum BuildConfiguration
    {
        Debug = 1,
        Release = 2
    }

    static class TestInstrumentationAssert
    {
        public static CoverageResult GenerateReport(this CoverageResult coverageResult, [CallerMemberName]string directory = "", bool show = false)
        {
            if (coverageResult is null)
            {
                throw new ArgumentNullException(nameof(coverageResult));
            }

            TestInstrumentationHelper.GenerateHtmlReport(coverageResult, directory: directory);

            if (show && Debugger.IsAttached)
            {
                Process.Start("cmd", "/C " + Path.GetFullPath(Path.Combine(directory, "index.htm")));
            }

            return coverageResult;
        }

        public static bool IsPresent(this CoverageResult coverageResult, string docName)
        {
            if (docName is null)
            {
                throw new ArgumentNullException(nameof(docName));
            }

            foreach (InstrumenterResult instrumenterResult in coverageResult.InstrumentedResults)
            {
                foreach (KeyValuePair<string, Document> document in instrumenterResult.Documents)
                {
                    if (Path.GetFileName(document.Key) == docName)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static Document Document(this CoverageResult coverageResult, string docName)
        {
            if (docName is null)
            {
                throw new ArgumentNullException(nameof(docName));
            }

            foreach (InstrumenterResult instrumenterResult in coverageResult.InstrumentedResults)
            {
                foreach (KeyValuePair<string, Document> document in instrumenterResult.Documents)
                {
                    if (Path.GetFileName(document.Key) == docName)
                    {
                        return document.Value;
                    }
                }
            }

            throw new XunitException($"Document not found '{docName}'");
        }

        public static Document AssertBranchesCovered(this Document document, params (int line, int ordinal, int hits)[] lines)
        {
            return AssertBranchesCovered(document, BuildConfiguration.Debug | BuildConfiguration.Release, lines);
        }

        public static Document ExpectedTotalNumberOfBranches(this Document document, BuildConfiguration configuration, int totalExpectedBranch)
        {
            if (document is null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            BuildConfiguration buildConfiguration = GetAssemblyBuildConfiguration();

            if ((buildConfiguration & configuration) != buildConfiguration)
            {
                return document;
            }

            int totalBranch = document.Branches.GroupBy(g => g.Key.Line).Count();

            if (totalBranch != totalExpectedBranch)
            {
                throw new XunitException($"Expected total branch is '{totalExpectedBranch}', actual '{totalBranch}'");
            }

            return document;
        }

        public static string ToStringBranches(this Document document)
        {
            if (document is null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            StringBuilder builder = new StringBuilder();
            foreach (KeyValuePair<BranchKey, Branch> branch in document.Branches)
            {
                builder.AppendLine($"({branch.Value.Number}, {branch.Value.Ordinal}, {branch.Value.Hits}),");
            }
            return builder.ToString();
        }

        public static Document AssertBranchesCovered(this Document document, BuildConfiguration configuration, params (int line, int ordinal, int hits)[] lines)
        {
            if (document is null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            BuildConfiguration buildConfiguration = GetAssemblyBuildConfiguration();

            if ((buildConfiguration & configuration) != buildConfiguration)
            {
                return document;
            }

            List<string> branchesToCover = new List<string>(lines.Select(b => $"[line {b.line} ordinal {b.ordinal}]"));
            foreach (KeyValuePair<BranchKey, Branch> branch in document.Branches)
            {
                foreach ((int lineToCheck, int ordinalToCheck, int expectedHits) in lines)
                {
                    if (branch.Value.Number == lineToCheck)
                    {
                        if (branch.Value.Ordinal == ordinalToCheck)
                        {
                            branchesToCover.Remove($"[line {branch.Value.Number} ordinal {branch.Value.Ordinal}]");

                            if (branch.Value.Hits != expectedHits)
                            {
                                throw new XunitException($"Unexpected hits expected line: {lineToCheck} ordinal {ordinalToCheck} hits: {expectedHits} actual hits: {branch.Value.Hits}");
                            }
                        }
                    }
                }
            }

            if (branchesToCover.Count != 0)
            {
                throw new XunitException($"Not all requested branch found, {branchesToCover.Select(l => l.ToString()).Aggregate((a, b) => $"{a}, {b}")}");
            }

            return document;
        }

        public static Document AssertLinesCovered(this Document document, params (int line, int hits)[] lines)
        {
            return AssertLinesCovered(document, BuildConfiguration.Debug | BuildConfiguration.Release, lines);
        }

        public static Document AssertLinesCoveredAllBut(this Document document, BuildConfiguration configuration, params int[] linesNumber)
        {
            if (document is null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            BuildConfiguration buildConfiguration = GetAssemblyBuildConfiguration();

            if ((buildConfiguration & configuration) != buildConfiguration)
            {
                return document;
            }

            foreach (KeyValuePair<int, Line> line in document.Lines)
            {
                bool skip = false;
                foreach (int number in linesNumber)
                {
                    if (line.Value.Number == number)
                    {
                        skip = true;
                        if (line.Value.Hits > 0)
                        {
                            throw new XunitException($"Hits not expected for line {line.Value.Number}");
                        }
                    }
                }

                if (skip)
                    continue;

                if (line.Value.Hits == 0)
                {
                    throw new XunitException($"Hits expected for line: {line.Value.Number}");
                }
            }

            return document;
        }

        public static Document AssertLinesCovered(this Document document, BuildConfiguration configuration, params (int line, int hits)[] lines)
        {
            if (document is null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            BuildConfiguration buildConfiguration = GetAssemblyBuildConfiguration();

            if ((buildConfiguration & configuration) != buildConfiguration)
            {
                return document;
            }

            List<int> linesToCover = new List<int>(lines.Select(l => l.line));
            foreach (KeyValuePair<int, Line> line in document.Lines)
            {
                foreach ((int lineToCheck, int expectedHits) in lines)
                {
                    if (line.Value.Number == lineToCheck)
                    {
                        linesToCover.Remove(line.Value.Number);
                        if (line.Value.Hits != expectedHits)
                        {
                            throw new XunitException($"Unexpected hits expected line: {lineToCheck} hits: {expectedHits} actual hits: {line.Value.Hits}");
                        }
                    }
                }
            }

            if (linesToCover.Count != 0)
            {
                throw new XunitException($"Not all requested line found, {linesToCover.Select(l => l.ToString()).Aggregate((a, b) => $"{a}, {b}")}");
            }

            return document;
        }

        public static Document AssertNonInstrumentedLines(this Document document, BuildConfiguration configuration, int from, int to)
        {
            if (document is null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            BuildConfiguration buildConfiguration = GetAssemblyBuildConfiguration();

            if ((buildConfiguration & configuration) != buildConfiguration)
            {
                return document;
            }

            int[] lineRange = Enumerable.Range(from, to - from + 1).ToArray();

            if (document.Lines.Select(l => l.Value.Number).Intersect(lineRange).Count() > 0)
            {
                throw new XunitException($"Unexpected instrumented lines, '{string.Join(',', lineRange)}'");
            }

            return document;
        }

        private static BuildConfiguration GetAssemblyBuildConfiguration()
        {
#if DEBUG
            return BuildConfiguration.Debug;
#endif
#if RELEASE
            return BuildConfiguration.Release;
#endif
            throw new NotSupportedException($"Build configuration not supported");
        }
    }
}
