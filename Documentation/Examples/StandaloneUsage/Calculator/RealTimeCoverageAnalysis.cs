using System;
using System.IO;
using System.Reflection;

using Coverlet.Core.Abstracts;
using Coverlet.Core.Instrumentation;
using Coverlet.Core.ObjectModel;

namespace Calculator
{
    public class RealTimeCoverageAnalysis
    {
        readonly IInProcessCoverageEngine inProcessEngine;

        public RealTimeCoverageAnalysis(string instrumentationResult)
        {
            ICoverageEngineFactory coverageFactory = new BuildInCoverageEngineFactory();
            inProcessEngine = coverageFactory.CreateInProcessEngine(File.OpenRead(instrumentationResult));
        }

        public void PrintCoverageCurrentState()
        {
            Console.WriteLine();
            Console.WriteLine("***Start live coverage analysis***");
            Console.WriteLine("---List of instrumented assemblies---");
            foreach (Assembly asm in inProcessEngine.GetInstrumentedAssemblies())
            {
                Console.WriteLine(asm);
            }
            Console.WriteLine("---Method lines coverage---");
            CoverageResult? coverageResult = inProcessEngine.ReadCurrentCoverage();
            if (coverageResult != null)
            {
                CoverageSummary summary = new CoverageSummary();
                foreach (var module in coverageResult.Modules)
                {
                    foreach (var document in module.Value)
                    {
                        foreach (var @class in document.Value)
                        {
                            foreach (var method in @class.Value)
                            {
                                var methodLineDetails = summary.CalculateMethodCoverage(method.Value.Lines);
                                Console.WriteLine($"Method '{method.Key}' {methodLineDetails.Percent}%");
                            }
                        }
                    }
                }
                ConsoleColor tmp = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Total modules method lines covered '{summary.CalculateMethodCoverage(coverageResult.Modules).Percent}'");
                Console.ForegroundColor = tmp;
            }
        }
    }
}
