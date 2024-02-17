// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using Xunit;

namespace Coverlet.Core.Tests
{
  public class CoverageSummaryTests
  {
    private Modules _averageCalculationSingleModule;
    private Modules _averageCalculationMultiModule;
    private Modules _moduleArithmeticPrecision;

    public CoverageSummaryTests()
    {
      SetupDataSingleModule();
      SetupDataMultipleModule();
      SetupDataForArithmeticPrecision();
    }

    private void SetupDataForArithmeticPrecision()
    {
      Lines lines = new();
      lines.Add(1, 1);
      for (int i = 2; i <= 6; i++)
      {
        lines.Add(i, 0);
      }
      Branches branches = new();
      branches.Add(new BranchInfo { Line = 1, Hits = 1, Offset = 1, Path = 0, Ordinal = 1 });
      for (int i = 2; i <= 6; i++)
      {
        branches.Add(new BranchInfo { Line = 1, Hits = 0, Offset = 1, Path = 1, Ordinal = (uint)i });
      }

      Methods methods = new();
      string methodString = "System.Void Coverlet.Core.Tests.CoverageSummaryTests::TestCalculateSummary()";
      methods.Add(methodString, new Method());
      methods[methodString].Lines = lines;
      methods[methodString].Branches = branches;

      Classes classes = new();
      classes.Add("Coverlet.Core.Tests.CoverageSummaryTests", methods);

      Documents documents = new();
      documents.Add("doc.cs", classes);

      _moduleArithmeticPrecision = new Modules();
      _moduleArithmeticPrecision.Add("module", documents);
    }

    private void SetupDataSingleModule()
    {
      Lines lines = new();
      lines.Add(1, 1);
      lines.Add(2, 0);
      Branches branches = new();
      branches.Add(new BranchInfo { Line = 1, Hits = 1, Offset = 1, Path = 0, Ordinal = 1 });
      branches.Add(new BranchInfo { Line = 1, Hits = 1, Offset = 1, Path = 1, Ordinal = 2 });

      Methods methods = new();
      string methodString = "System.Void Coverlet.Core.Tests.CoverageSummaryTests::TestCalculateSummary()";
      methods.Add(methodString, new Method());
      methods[methodString].Lines = lines;
      methods[methodString].Branches = branches;

      Classes classes = new();
      classes.Add("Coverlet.Core.Tests.CoverageSummaryTests", methods);

      Documents documents = new();
      documents.Add("doc.cs", classes);

      _averageCalculationSingleModule = new Modules();
      _averageCalculationSingleModule.Add("module", documents);
    }

    private void SetupDataMultipleModule()
    {
      Lines lines = new()
      {
                { 1, 1 }, // covered
                { 2, 0 }, // not covered
                { 3, 0 } // not covered
            };

      Branches branches = new()
      {
                new BranchInfo { Line = 1, Hits = 1, Offset = 1, Path = 0, Ordinal = 1 }, // covered
                new BranchInfo { Line = 1, Hits = 1, Offset = 1, Path = 1, Ordinal = 2 }, // covered
                new BranchInfo { Line = 1, Hits = 0, Offset = 1, Path = 1, Ordinal = 2 } // not covered
            };

      Methods methods = new();
      string[] methodString = [
                "System.Void Coverlet.Core.Tests.CoverageSummaryTests::TestCalculateSummary()", // covered
                "System.Void Coverlet.Core.Tests.CoverageSummaryTests::TestAdditionalCalculateSummary()" // not covered
            ];
      methods.Add(methodString[0], new Method());
      methods[methodString[0]].Lines = lines;
      methods[methodString[0]].Branches = branches;

      methods.Add(methodString[1], new Method());
      methods[methodString[1]].Lines = new Lines
            {
                { 1, 0 } // not covered
            };

      Classes classes = new()
      {
                { "Coverlet.Core.Tests.CoverageSummaryTests", methods }
            };

      Documents documents = new()
      {
                { "doc.cs", classes }
            };

      _averageCalculationMultiModule = new Modules
            {
                { "module", _averageCalculationSingleModule["module"] },
                { "additionalModule", documents }
            };
    }

    [Fact]
    public void TestCalculateLineCoverage_NoModules()
    {
      CoverageSummary summary = new();
      Modules modules = new();

      Assert.Equal(0, summary.CalculateLineCoverage(modules).Percent);
      Assert.Equal(0, summary.CalculateLineCoverage(modules).AverageModulePercent);
      Assert.Equal(0, summary.CalculateBranchCoverage(modules).Percent);
      Assert.Equal(0, summary.CalculateBranchCoverage(modules).AverageModulePercent);
      Assert.Equal(0, summary.CalculateMethodCoverage(modules).Percent);
      Assert.Equal(0, summary.CalculateMethodCoverage(modules).AverageModulePercent);
    }

    [Fact]
    public void TestCalculateLineCoverage_SingleModule()
    {
      CoverageSummary summary = new();

      System.Collections.Generic.KeyValuePair<string, Documents> module = _averageCalculationSingleModule.First();
      System.Collections.Generic.KeyValuePair<string, Classes> document = module.Value.First();
      System.Collections.Generic.KeyValuePair<string, Methods> @class = document.Value.First();
      System.Collections.Generic.KeyValuePair<string, Method> method = @class.Value.First();

      Assert.Equal(50, summary.CalculateLineCoverage(_averageCalculationSingleModule).AverageModulePercent);
      Assert.Equal(50, summary.CalculateLineCoverage(module.Value).Percent);
      Assert.Equal(50, summary.CalculateLineCoverage(document.Value).Percent);
      Assert.Equal(50, summary.CalculateLineCoverage(@class.Value).Percent);
      Assert.Equal(50, summary.CalculateLineCoverage(method.Value.Lines).Percent);
    }

    [Fact]
    public void TestCalculateLineCoverage_MultiModule()
    {
      CoverageSummary summary = new();
      Documents documentsFirstModule = _averageCalculationMultiModule["module"];
      Documents documentsSecondModule = _averageCalculationMultiModule["additionalModule"];

      Assert.Equal(37.5, summary.CalculateLineCoverage(_averageCalculationMultiModule).AverageModulePercent);
      Assert.Equal(50, summary.CalculateLineCoverage(documentsFirstModule.First().Value).Percent);

      Assert.Equal(33.33, summary.CalculateLineCoverage(documentsSecondModule.First().Value.First().Value.ElementAt(0).Value.Lines).Percent); // covered 1 of 3
      Assert.Equal(0, summary.CalculateLineCoverage(documentsSecondModule.First().Value.First().Value.ElementAt(1).Value.Lines).Percent); // covered 0 of 1
      Assert.Equal(25, summary.CalculateLineCoverage(documentsSecondModule.First().Value).Percent); // covered 1 of 4 lines
    }

    [Fact]
    public void TestCalculateBranchCoverage_SingleModule()
    {
      CoverageSummary summary = new();

      System.Collections.Generic.KeyValuePair<string, Documents> module = _averageCalculationSingleModule.First();
      System.Collections.Generic.KeyValuePair<string, Classes> document = module.Value.First();
      System.Collections.Generic.KeyValuePair<string, Methods> @class = document.Value.First();
      System.Collections.Generic.KeyValuePair<string, Method> method = @class.Value.First();

      Assert.Equal(100, summary.CalculateBranchCoverage(_averageCalculationSingleModule).AverageModulePercent);
      Assert.Equal(100, summary.CalculateBranchCoverage(module.Value).Percent);
      Assert.Equal(100, summary.CalculateBranchCoverage(document.Value).Percent);
      Assert.Equal(100, summary.CalculateBranchCoverage(@class.Value).Percent);
      Assert.Equal(100, summary.CalculateBranchCoverage(method.Value.Branches).Percent);
    }

    [Fact]
    public void TestCalculateBranchCoverage_MultiModule()
    {
      CoverageSummary summary = new();
      Documents documentsFirstModule = _averageCalculationMultiModule["module"];
      Documents documentsSecondModule = _averageCalculationMultiModule["additionalModule"];

      Assert.Equal(83.33, summary.CalculateBranchCoverage(_averageCalculationMultiModule).AverageModulePercent);
      Assert.Equal(100, summary.CalculateBranchCoverage(documentsFirstModule.First().Value).Percent);
      Assert.Equal(66.66, summary.CalculateBranchCoverage(documentsSecondModule.First().Value).Percent);
    }

    [Fact]
    public void TestCalculateMethodCoverage_SingleModule()
    {
      CoverageSummary summary = new();

      System.Collections.Generic.KeyValuePair<string, Documents> module = _averageCalculationSingleModule.First();
      System.Collections.Generic.KeyValuePair<string, Classes> document = module.Value.First();
      System.Collections.Generic.KeyValuePair<string, Methods> @class = document.Value.First();
      System.Collections.Generic.KeyValuePair<string, Method> method = @class.Value.First();

      Assert.Equal(100, summary.CalculateMethodCoverage(_averageCalculationSingleModule).AverageModulePercent);
      Assert.Equal(100, summary.CalculateMethodCoverage(module.Value).Percent);
      Assert.Equal(100, summary.CalculateMethodCoverage(document.Value).Percent);
      Assert.Equal(100, summary.CalculateMethodCoverage(@class.Value).Percent);
      Assert.Equal(100, summary.CalculateMethodCoverage(method.Value.Lines).Percent);
    }

    [Fact]
    public void TestCalculateMethodCoverage_MultiModule()
    {
      CoverageSummary summary = new();
      Documents documentsFirstModule = _averageCalculationMultiModule["module"];
      Documents documentsSecondModule = _averageCalculationMultiModule["additionalModule"];

      Assert.Equal(75, summary.CalculateMethodCoverage(_averageCalculationMultiModule).AverageModulePercent);
      Assert.Equal(100, summary.CalculateMethodCoverage(documentsFirstModule.First().Value).Percent);
      Assert.Equal(50, summary.CalculateMethodCoverage(documentsSecondModule.First().Value).Percent);
    }

    [Fact]
    public void TestCalculateLineCoveragePercentage_ArithmeticPrecisionCheck()
    {
      CoverageSummary summary = new();

      System.Collections.Generic.KeyValuePair<string, Documents> module = _moduleArithmeticPrecision.First();
      System.Collections.Generic.KeyValuePair<string, Classes> document = module.Value.First();
      System.Collections.Generic.KeyValuePair<string, Methods> @class = document.Value.First();
      System.Collections.Generic.KeyValuePair<string, Method> method = @class.Value.First();

      Assert.Equal(16.66, summary.CalculateLineCoverage(_moduleArithmeticPrecision).AverageModulePercent);
      Assert.Equal(16.66, summary.CalculateLineCoverage(module.Value).Percent);
      Assert.Equal(16.66, summary.CalculateLineCoverage(document.Value).Percent);
      Assert.Equal(16.66, summary.CalculateLineCoverage(@class.Value).Percent);
      Assert.Equal(16.66, summary.CalculateLineCoverage(method.Value.Lines).Percent);
    }

    [Fact]
    public void TestCalculateBranchCoveragePercentage_ArithmeticPrecisionCheck()
    {
      CoverageSummary summary = new();

      System.Collections.Generic.KeyValuePair<string, Documents> module = _moduleArithmeticPrecision.First();
      System.Collections.Generic.KeyValuePair<string, Classes> document = module.Value.First();
      System.Collections.Generic.KeyValuePair<string, Methods> @class = document.Value.First();
      System.Collections.Generic.KeyValuePair<string, Method> method = @class.Value.First();

      Assert.Equal(16.66, summary.CalculateBranchCoverage(_moduleArithmeticPrecision).AverageModulePercent);
      Assert.Equal(16.66, summary.CalculateBranchCoverage(module.Value).Percent);
      Assert.Equal(16.66, summary.CalculateBranchCoverage(document.Value).Percent);
      Assert.Equal(16.66, summary.CalculateBranchCoverage(@class.Value).Percent);
      Assert.Equal(16.66, summary.CalculateBranchCoverage(method.Value.Branches).Percent);
    }
  }
}
