// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

//using System.Diagnostics.Tracing;
using System.Globalization;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
//using BenchmarkDotNet.Diagnostics.Windows;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Exporters.Xml;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;
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
      // InvariantCulture has "," as NumberGroupSeparator, which produces "483,051,510.20 ns"
      // in CSV/summary output. Enforce "." as decimal separator and suppress the thousands
      // separator so results are unambiguous and comparable worldwide.
      var noGroupSeparatorCulture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
      noGroupSeparatorCulture.NumberFormat.NumberDecimalSeparator = ".";
      noGroupSeparatorCulture.NumberFormat.NumberGroupSeparator = string.Empty;
      noGroupSeparatorCulture.NumberFormat.NumberGroupSizes = [0];

      var config = DefaultConfig.Instance
            .WithOptions(ConfigOptions.DisableOptimizationsValidator)
            .WithOptions(ConfigOptions.JoinSummary)
            .WithOption(ConfigOptions.DisableLogFile, true)
            .WithCultureInfo(noGroupSeparatorCulture)
            .WithSummaryStyle(SummaryStyle.Default.WithCultureInfo(noGroupSeparatorCulture))
            .AddJob(Job
              .LongRun
              .WithLaunchCount(1)
              .WithToolchain(InProcessNoEmitToolchain.Instance))
            .AddExporter(new CsvExporter(CsvSeparator.Semicolon), new CsvMeasurementsExporter(CsvSeparator.Semicolon), RPlotExporter.Default, HtmlExporter.Default, JsonExporter.Default, MarkdownExporter.GitHub, XmlExporter.Default)
            .AddDiagnoser(MemoryDiagnoser.Default, ThreadingDiagnoser.Default, ExceptionDiagnoser.Default)
            //.AddDiagnoser(new InliningDiagnoser(), new EtwProfiler()) // only windows platform, requires elevated privileges
            //.AddDiagnoser(new EventPipeProfiler(EventPipeProfile.CpuSampling)) // stops collecting results ???
            ;
#if DEBUG
      config = config.WithOptions(ConfigOptions.DisableOptimizationsValidator);
      System.Diagnostics.Debugger.Launch(); // Optional: force debugger attachment
#endif
      // AutoPropsBenchmarks carries [CPUUsageDiagnoser] which requires an out-of-process
      // toolchain so the VS DiagnosticsHub profiler can attach to the child process and
      // export a .diagsession file. Using InProcessNoEmitToolchain here suppresses that
      // entirely because there is no child process to attach to.
      var vsProfilingConfig = DefaultConfig.Instance
            .WithOptions(ConfigOptions.DisableOptimizationsValidator)
            .WithOptions(ConfigOptions.JoinSummary)
            .WithOption(ConfigOptions.DisableLogFile, true)
            .WithCultureInfo(noGroupSeparatorCulture)
            .WithSummaryStyle(SummaryStyle.Default.WithCultureInfo(noGroupSeparatorCulture))
            .AddJob(Job.LongRun.WithLaunchCount(1)) // out-of-process: no InProcessNoEmitToolchain
            .AddExporter(new CsvExporter(CsvSeparator.Semicolon), JsonExporter.Default, MarkdownExporter.GitHub)
            .AddDiagnoser(MemoryDiagnoser.Default);

      var summary = BenchmarkRunner.Run(new[]{
            BenchmarkConverter.TypeToBenchmarks( typeof(CoverageBenchmarks), config),
            BenchmarkConverter.TypeToBenchmarks( typeof(InstrumenterBenchmarks), config),
            BenchmarkConverter.TypeToBenchmarks( typeof(CoverageWorkflowBenchmark), config),
            BenchmarkConverter.TypeToBenchmarks( typeof(InstrumentationOptionsBenchmarks), config),
            BenchmarkConverter.TypeToBenchmarks( typeof(ReportFormatBenchmarks), config),
            BenchmarkConverter.TypeToBenchmarks( typeof(AutoPropsBenchmarks), vsProfilingConfig),
            });

      // Use this to select benchmarks from the console and execute with additional options e.g. 'coverlet.core.benchmark.tests.exe --profiler EP'
      //var summaries = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
  }
}
