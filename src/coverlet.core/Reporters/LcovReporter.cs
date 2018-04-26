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
                    lcov.Add("SF:" + doc.Key);
                    foreach (var @class in doc.Value)
                    {
                        bool methodVisited = false;
                        foreach (var method in @class.Value)
                        {
                            lcov.Add($"FN:{method.Value.First().Key - 1},{method.Key}");
                            lcov.Add($"FNDA:{method.Value.First().Value.Hits},{method.Key}");

                            foreach (var line in method.Value)
                            {
                                lcov.Add($"DA:{line.Key},{line.Value.Hits}");
                                numSequencePoints++;

                                if (line.Value.IsBranchPoint)
                                {
                                    lcov.Add($"BRDA:{line.Key},{numBlockBranch},{numBlockBranch},{line.Value.Hits}");
                                    numBlockBranch++;
                                    numBranchPoints++;
                                }

                                if (line.Value.Hits > 0)
                                {
                                    visitedSequencePoints++;
                                    methodVisited = true;
                                    if (line.Value.IsBranchPoint)
                                        visitedBranchPoints++;
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