// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

//using System.Diagnostics.Tracing;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
//using BenchmarkDotNet.Diagnostics.Windows;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Exporters.Xml;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.NoEmit;
//using Microsoft.Diagnostics.NETCore.Client;
//using Microsoft.Diagnostics.Tracing.Parsers;

namespace coverlet.core.benchmark.tests
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
              .WithToolchain(InProcessNoEmitToolchain.Instance))
            .AddExporter(CsvExporter.Default, CsvMeasurementsExporter.Default, RPlotExporter.Default, HtmlExporter.Default, JsonExporter.Default, MarkdownExporter.GitHub, XmlExporter.Default)
            .AddDiagnoser(MemoryDiagnoser.Default, ThreadingDiagnoser.Default, ExceptionDiagnoser.Default)
            //.AddDiagnoser(new InliningDiagnoser(), new EtwProfiler()) // only windows platform, requires elevated privileges
            //.AddDiagnoser(new EventPipeProfiler(EventPipeProfile.CpuSampling)) // stops collecting results ???
            ;
      var summary = BenchmarkRunner.Run(new[]{
            BenchmarkConverter.TypeToBenchmarks( typeof(CoverageBenchmarks), config),
            BenchmarkConverter.TypeToBenchmarks( typeof(InstrumenterBenchmarks), config),
            BenchmarkConverter.TypeToBenchmarks( typeof(InstrumentationHelperBenchmarks), config),
            });

      // Use this to select benchmarks from the console and execute with additional options e.g. 'coverlet.core.benchmark.tests.exe --profiler EP'
      //var summaries = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
  }
}
