using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.NoEmit;

namespace coverlet.core.benchmark.tests
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var config = DefaultConfig.Instance
             .AddJob(Job
               .MediumRun
               .WithLaunchCount(1)
               .WithToolchain(InProcessNoEmitToolchain.Instance));
        var summary = BenchmarkRunner.Run<CoreBenchmarks>(config, args);

            // Use this to select benchmarks from the console:
            // var summaries = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);
        }
    }
}
