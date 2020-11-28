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
        public static CoverageResult GenerateReport(this CoverageResult coverageResult, [CallerMemberName] string directory = "", bool show = false)
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

        public static Document Method(this Document document, string methodName)
        {
            var methodDoc = new Document { Path = document.Path, Index = document.Index };

            if (!document.Lines.Any() && !document.Branches.Any())
            {
                return methodDoc;
            }

            if (document.Lines.Values.All(l => l.Method != methodName) && document.Branches.Values.All(l => l.Method != methodName))
            {
                var methods = document.Lines.Values.Select(l => $"'{l.Method}'")
                    .Concat(document.Branches.Values.Select(b => $"'{b.Method}'"))
                    .Distinct();
                throw new XunitException($"Method '{methodName}' not found. Methods in document: {string.Join(", ", methods)}");
            }

            foreach (var line in document.Lines.Where(l => l.Value.Method == methodName))
            {
                methodDoc.Lines[line.Key] = line.Value;
            }

            foreach (var branch in document.Branches.Where(b => b.Value.Method == methodName))
            {
                methodDoc.Branches[branch.Key] = branch.Value;
            }

            return methodDoc;
        }

        public static Document AssertBranchesCovered(this Document document, params (int line, int ordinal, int hits)[] lines)
        {
            return AssertBranchesCovered(document, BuildConfiguration.Debug | BuildConfiguration.Release, lines);
        }

        public static Document ExpectedTotalNumberOfBranches(this Document document, int totalExpectedBranch)
        {
            return ExpectedTotalNumberOfBranches(document, BuildConfiguration.Debug | BuildConfiguration.Release, totalExpectedBranch);
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

        public static Document AssertLinesCoveredFromTo(this Document document, BuildConfiguration configuration, int from, int to)
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

            if (to < from)
            {
                throw new ArgumentException("to cannot be lower than from");
            }

            List<int> lines = new List<int>();
            foreach (KeyValuePair<int, Line> line in document.Lines)
            {
                if (line.Value.Number >= from && line.Value.Number <= to && line.Value.Hits > 0)
                {
                    lines.Add(line.Value.Number);
                }
            }

            if (!lines.OrderBy(l => l).SequenceEqual(Enumerable.Range(from, to - from + 1)))
            {
                throw new XunitException($"Unexpected lines covered");
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

        public static Document AssertLinesCovered(this Document document, BuildConfiguration configuration, params int[] lines)
        {
            return AssertLinesCoveredInternal(document, configuration, true, lines);
        }

        public static Document AssertLinesNotCovered(this Document document, BuildConfiguration configuration, params int[] lines)
        {
            return AssertLinesCoveredInternal(document, configuration, false, lines);
        }

        private static Document AssertLinesCoveredInternal(this Document document, BuildConfiguration configuration, bool covered, params int[] lines)
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

            List<int> linesToCover = new List<int>(lines);
            foreach (KeyValuePair<int, Line> line in document.Lines)
            {
                foreach (int lineToCheck in lines)
                {
                    if (line.Value.Number == lineToCheck)
                    {
                        if (covered && line.Value.Hits > 0)
                        {
                            linesToCover.Remove(line.Value.Number);
                        }
                        if (!covered && line.Value.Hits == 0)
                        {
                            linesToCover.Remove(line.Value.Number);
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

            return AssertNonInstrumentedLines(document, configuration, lineRange);
        }

        public static Document AssertNonInstrumentedLines(this Document document, BuildConfiguration configuration, params int[] lines)
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

            var unexpectedlyInstrumented = document.Lines.Select(l => l.Value.Number).Intersect(lines);

            if (unexpectedlyInstrumented.Any())
            {
                throw new XunitException($"Unexpected instrumented lines, '{string.Join(',', unexpectedlyInstrumented)}'");
            }

            return document;
        }

        public static Document AssertInstrumentLines(this Document document, BuildConfiguration configuration, params int[] lines)
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

            var instrumentedLines = document.Lines.Select(l => l.Value.Number).ToHashSet();

            var missing = lines.Where(l => !instrumentedLines.Contains(l));

            if (missing.Any())
            {
                throw new XunitException($"Expected lines to be instrumented, '{string.Join(',', missing)}'");
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
