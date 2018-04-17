using System;
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
            foreach (var module in result.Modules)
            {
                foreach (var doc in module.Value)
                {
                    lcov.Add("SF:" + doc.Key);
                    foreach (var @class in doc.Value)
                    {
                        foreach (var method in @class.Value)
                        {
                            foreach (var line in method.Value)
                            {
                                lcov.Add($"DA:{line.Key},{line.Value.Hits}");
                            }
                        }
                    }

                    lcov.Add("end_of_record");
                }
            }

            return string.Join(Environment.NewLine, lcov);
        }
    }
}