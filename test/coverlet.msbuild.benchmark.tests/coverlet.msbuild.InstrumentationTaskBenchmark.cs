// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

//using Coverlet.Core.Abstractions;
//using Coverlet.Core.Helpers;
using Coverlet.MSbuild.Tasks;

using BenchmarkDotNet.Attributes;
//using System.IO;

namespace coverlet.msbuild.benchmark.tests
{
  [MemoryDiagnoser]
  public class InstrumentationTaskBenchmarks
  {
    InstrumentationTask? _instrumentationTask;

    [GlobalSetup(Target = nameof(InstrumentationTaskBenchmarkSingle))]
    public void InstrumentationTaskBenchmarkSingleSetup()
    {
      _instrumentationTask = new InstrumentationTask
      {
        //IncludeDirectory = "*",
        SingleHit = false,
        UseSourceLink = false,
        SkipAutoProps = true,
        DeterministicReport = false
      };
    }

    [GlobalSetup(Target = nameof(InstrumentationTaskBenchmarkInitialTargetsOuterAndInner))]
    public void PreprocessorBenchmarkInitialTargetsOuterAndInnerSetup()
    {
      //ProjectRootElement xml1 = ProjectRootElement.Create("p1");
      //xml1.InitialTargets = "i1";
      //xml1.AddImport("p2");
      //ProjectRootElement xml2 = ProjectRootElement.Create("p2");
      //xml2.InitialTargets = "i2";

      _instrumentationTask = new InstrumentationTask
      {
        //IncludeDirectory = "*",
        SingleHit = false,
        UseSourceLink = false,
        SkipAutoProps = true,
        DeterministicReport = false
      };
    }

    [Benchmark]
    public void InstrumentationTaskBenchmarkSingle()
    {
      _instrumentationTask!.Execute();
    }

    [Benchmark]
    public void InstrumentationTaskBenchmarkInitialTargetsOuterAndInner()
    {
      _instrumentationTask!.Execute();
    }
  }
}
