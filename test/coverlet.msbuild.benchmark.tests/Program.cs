using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.NoEmit;

namespace coverlet.msbuild.benchmark.tests
{
  public class Program
  {
    public static void Main(string[] args)
    {
      var config = DefaultConfig.Instance
             .WithOptions(ConfigOptions.JoinSummary)
             .AddJob(Job
               .ShortRun
               .WithLaunchCount(1)
               .WithToolchain(InProcessNoEmitToolchain.Instance));
      var summary = BenchmarkRunner.Run(new[]{
            BenchmarkConverter.TypeToBenchmarks( typeof(CoverageResultTaskBenchmarks), config),
            //BenchmarkConverter.TypeToBenchmarks( typeof(InstrumentationTaskBenchmarks), config)
            });

      // Use this to select benchmarks from the console:
      //var summaries = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);
    }
  }
}
