using System;
using System.IO;
using Coverlet.Core.Abstracts;
using Coverlet.Core.Instrumentation;

namespace Instrumentor
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0 || !File.Exists(args[0]))
            {
                Console.WriteLine("Invalid lib to instrument");
                return;
            }

            ICoverageEngineFactory coverageFactory = new BuildInCoverageEngineFactory();
            InstrumentationOptions options = new InstrumentationOptions();
            options.Module = args[0];
            options.IncludeTestAssembly = true;
            ICoverageEngine coverageEngine = coverageFactory.CreateEngine(options);
            using (Stream stream = coverageEngine.PrepareModules(),
                          fs = File.OpenWrite("instrumentationResult"))
            {
                stream.CopyTo(fs);
            }

            Console.WriteLine($"Instrumentation result saved '{Path.GetFullPath("instrumentationResult")}'");
            Console.WriteLine("Instrumentor active, click any button to restore instrumented libraries");
            Console.ReadKey();
        }
    }
}
