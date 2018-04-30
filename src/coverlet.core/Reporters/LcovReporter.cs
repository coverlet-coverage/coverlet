using System;
using System.Linq;
using System.Collections.Generic;

namespace Coverlet.Core.Reporters
{
    public class LcovReporter : IReporter
    {
        public string Format => "lcov";

        public string Extension => "info";

        public string Report(CoverageResult result)
        {
            List<string> lcov = new List<string>();
            int numSequencePoints = 0, numBranchPoints = 0, numMethods = 0, numBlockBranch = 1;
            int visitedSequencePoints = 0, visitedBranchPoints = 0, visitedMethods = 0;

            foreach (var module in result.Modules)
            {
                foreach (var doc in module.Value)
                {
                    numMethods = 0;
                    visitedMethods = 0;
                    numSequencePoints = 0;
                    visitedSequencePoints = 0;
                    numBranchPoints = 0;
                    visitedBranchPoints = 0;
                    lcov.Add("SF:" + doc.Key);
                    foreach (var @class in doc.Value)
                    {
                        bool methodVisited = false;
                        foreach (var method in @class.Value)
                        {
                            // Skip all methods with no lines
                            if (method.Value.Lines.Count == 0)
                                continue;

                            lcov.Add($"FN:{method.Value.Lines.First().Key - 1},{method.Key}");
                            lcov.Add($"FNDA:{method.Value.Lines.First().Value.Hits},{method.Key}");

                            foreach (var line in method.Value.Lines)
                            {
                                lcov.Add($"DA:{line.Key},{line.Value.Hits}");
                                numSequencePoints++;

                                if (line.Value.Hits > 0)
                                {
                                    visitedSequencePoints++;
                                    methodVisited = true;
                                }
                            }

                            foreach (var branchs in method.Value.Branches)
                            {
                                foreach (var branch in branchs.Value)
                                {
                                    lcov.Add($"BRDA:{branchs.Key},{branch.Offset},{branch.Path},{branch.Hits}");
                                    numBlockBranch++;
                                    numBranchPoints++;
                                    if (branch.Hits > 0)
                                    {
                                        visitedBranchPoints++;
                                    }
                                }
                            }

                            numMethods++;
                            if (methodVisited)
                                visitedMethods++;
                        }
                    }

                    lcov.Add($"LH:{visitedSequencePoints}");
                    lcov.Add($"LF:{numSequencePoints}");

                    lcov.Add($"BRF:{numBranchPoints}");
                    lcov.Add($"BRH:{visitedBranchPoints}");

                    lcov.Add($"FNF:{numMethods}");
                    lcov.Add($"FNH:{visitedMethods}");

                    lcov.Add("end_of_record");
                }
            }

            return string.Join(Environment.NewLine, lcov);
        }
    }
}