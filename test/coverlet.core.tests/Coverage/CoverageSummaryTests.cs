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
      var lines = new Lines
      {
        { 1, 1 }
      };
      for (int i = 2; i <= 6; i++)
      {
        lines.Add(i, 0);
      }
      var branches = new Branches
      {
        new BranchInfo { Line = 1, Hits = 1, Offset = 1, Path = 0, Ordinal = 1 }
      };
      for (int i = 2; i <= 6; i++)
      {
        branches.Add(new BranchInfo { Line = 1, Hits = 0, Offset = 1, Path = 1, Ordinal = (uint)i });
      }

      var methods = new Methods();
      string methodString = "System.Void Coverlet.Core.Tests.CoverageSummaryTests::TestCalculateSummary()";
      methods.Add(methodString, new Method());
      methods[methodString].Lines = lines;
      methods[methodString].Branches = branches;

      var classes = new Classes
      {
        { "Coverlet.Core.Tests.CoverageSummaryTests", methods }
      };

      var documents = new Documents
      {
        { "doc.cs", classes }
      };

      _moduleArithmeticPrecision = new Modules
      {
        { "module", documents }
      };
    }

    private void SetupDataSingleModule()
    {
      var lines = new Lines
      {
        { 1, 1 },
        { 2, 0 }
      };
      var branches = new Branches
      {
        new BranchInfo { Line = 1, Hits = 1, Offset = 1, Path = 0, Ordinal = 1 },
        new BranchInfo { Line = 1, Hits = 1, Offset = 1, Path = 1, Ordinal = 2 }
      };

      var methods = new Methods();
      string methodString = "System.Void Coverlet.Core.Tests.CoverageSummaryTests::TestCalculateSummary()";
      methods.Add(methodString, new Method());
      methods[methodString].Lines = lines;
      methods[methodString].Branches = branches;

      var classes = new Classes
      {
        { "Coverlet.Core.Tests.CoverageSummaryTests", methods }
      };

      var documents = new Documents
      {
        { "doc.cs", classes }
      };

      _averageCalculationSingleModule = new Modules
      {
        { "module", documents }
      };
    }

    private void SetupDataMultipleModule()
    {
      var lines = new Lines
            {
                { 1, 1 }, // covered
                { 2, 0 }, // not covered
                { 3, 0 } // not covered
            };

      var branches = new Branches
            {
                new BranchInfo { Line = 1, Hits = 1, Offset = 1, Path = 0, Ordinal = 1 }, // covered
                new BranchInfo { Line = 1, Hits = 1, Offset = 1, Path = 1, Ordinal = 2 }, // covered
                new BranchInfo { Line = 1, Hits = 0, Offset = 1, Path = 1, Ordinal = 2 } // not covered
            };

      var methods = new Methods();
      string[] methodString = {
                "System.Void Coverlet.Core.Tests.CoverageSummaryTests::TestCalculateSummary()", // covered
                "System.Void Coverlet.Core.Tests.CoverageSummaryTests::TestAdditionalCalculateSummary()" // not covered
            };
      methods.Add(methodString[0], new Method());
      methods[methodString[0]].Lines = lines;
      methods[methodString[0]].Branches = branches;

      methods.Add(methodString[1], new Method());
      methods[methodString[1]].Lines = new Lines
            {
                { 1, 0 } // not covered
            };

      var classes = new Classes
            {
                { "Coverlet.Core.Tests.CoverageSummaryTests", methods }
            };

      var documents = new Documents
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
      var modules = new Modules();

      Assert.Equal(0, CoverageSummary.CalculateLineCoverage(modules).Percent);
      Assert.Equal(0, CoverageSummary.CalculateLineCoverage(modules).AverageModulePercent);
      Assert.Equal(0, CoverageSummary.CalculateBranchCoverage(modules).Percent);
      Assert.Equal(0, CoverageSummary.CalculateBranchCoverage(modules).AverageModulePercent);
      Assert.Equal(0, CoverageSummary.CalculateMethodCoverage(modules).Percent);
      Assert.Equal(0, CoverageSummary.CalculateMethodCoverage(modules).AverageModulePercent);
    }

    [Fact]
    public void TestCalculateLineCoverage_SingleModule()
    {
      System.Collections.Generic.KeyValuePair<string, Documents> module = _averageCalculationSingleModule.First();
      System.Collections.Generic.KeyValuePair<string, Classes> document = module.Value.First();
      System.Collections.Generic.KeyValuePair<string, Methods> @class = document.Value.First();
      System.Collections.Generic.KeyValuePair<string, Method> method = @class.Value.First();

      Assert.Equal(50, CoverageSummary.CalculateLineCoverage(_averageCalculationSingleModule).AverageModulePercent);
      Assert.Equal(50, CoverageSummary.CalculateLineCoverage(module.Value).Percent);
      Assert.Equal(50, CoverageSummary.CalculateLineCoverage(document.Value).Percent);
      Assert.Equal(50, CoverageSummary.CalculateLineCoverage(@class.Value).Percent);
      Assert.Equal(50, CoverageSummary.CalculateLineCoverage(method.Value.Lines).Percent);
    }

    [Fact]
    public void TestCalculateLineCoverage_MultiModule()
    {
      Documents documentsFirstModule = _averageCalculationMultiModule["module"];
      Documents documentsSecondModule = _averageCalculationMultiModule["additionalModule"];

      Assert.Equal(37.5, CoverageSummary.CalculateLineCoverage(_averageCalculationMultiModule).AverageModulePercent);
      Assert.Equal(50, CoverageSummary.CalculateLineCoverage(documentsFirstModule.First().Value).Percent);

      Assert.Equal(33.33, CoverageSummary.CalculateLineCoverage(documentsSecondModule.First().Value.First().Value.ElementAt(0).Value.Lines).Percent); // covered 1 of 3
      Assert.Equal(0, CoverageSummary.CalculateLineCoverage(documentsSecondModule.First().Value.First().Value.ElementAt(1).Value.Lines).Percent); // covered 0 of 1
      Assert.Equal(25, CoverageSummary.CalculateLineCoverage(documentsSecondModule.First().Value).Percent); // covered 1 of 4 lines
    }

    [Fact]
    public void TestCalculateBranchCoverage_SingleModule()
    {
      System.Collections.Generic.KeyValuePair<string, Documents> module = _averageCalculationSingleModule.First();
      System.Collections.Generic.KeyValuePair<string, Classes> document = module.Value.First();
      System.Collections.Generic.KeyValuePair<string, Methods> @class = document.Value.First();
      System.Collections.Generic.KeyValuePair<string, Method> method = @class.Value.First();

      Assert.Equal(100, CoverageSummary.CalculateBranchCoverage(_averageCalculationSingleModule).AverageModulePercent);
      Assert.Equal(100, CoverageSummary.CalculateBranchCoverage(module.Value).Percent);
      Assert.Equal(100, CoverageSummary.CalculateBranchCoverage(document.Value).Percent);
      Assert.Equal(100, CoverageSummary.CalculateBranchCoverage(@class.Value).Percent);
      Assert.Equal(100, CoverageSummary.CalculateBranchCoverage(method.Value.Branches).Percent);
    }

    [Fact]
    public void TestCalculateBranchCoverage_MultiModule()
    {
      Documents documentsFirstModule = _averageCalculationMultiModule["module"];
      Documents documentsSecondModule = _averageCalculationMultiModule["additionalModule"];

      Assert.Equal(83.33, CoverageSummary.CalculateBranchCoverage(_averageCalculationMultiModule).AverageModulePercent);
      Assert.Equal(100, CoverageSummary.CalculateBranchCoverage(documentsFirstModule.First().Value).Percent);
      Assert.Equal(66.66, CoverageSummary.CalculateBranchCoverage(documentsSecondModule.First().Value).Percent);
    }

    [Fact]
    public void TestCalculateMethodCoverage_SingleModule()
    {
      System.Collections.Generic.KeyValuePair<string, Documents> module = _averageCalculationSingleModule.First();
      System.Collections.Generic.KeyValuePair<string, Classes> document = module.Value.First();
      System.Collections.Generic.KeyValuePair<string, Methods> @class = document.Value.First();
      System.Collections.Generic.KeyValuePair<string, Method> method = @class.Value.First();

      Assert.Equal(100, CoverageSummary.CalculateMethodCoverage(_averageCalculationSingleModule).AverageModulePercent);
      Assert.Equal(100, CoverageSummary.CalculateMethodCoverage(module.Value).Percent);
      Assert.Equal(100, CoverageSummary.CalculateMethodCoverage(document.Value).Percent);
      Assert.Equal(100, CoverageSummary.CalculateMethodCoverage(@class.Value).Percent);
      Assert.Equal(100, CoverageSummary.CalculateMethodCoverage(method.Value.Lines).Percent);
    }

    [Fact]
    public void TestCalculateMethodCoverage_MultiModule()
    {
      Documents documentsFirstModule = _averageCalculationMultiModule["module"];
      Documents documentsSecondModule = _averageCalculationMultiModule["additionalModule"];

      Assert.Equal(75, CoverageSummary.CalculateMethodCoverage(_averageCalculationMultiModule).AverageModulePercent);
      Assert.Equal(100, CoverageSummary.CalculateMethodCoverage(documentsFirstModule.First().Value).Percent);
      Assert.Equal(50, CoverageSummary.CalculateMethodCoverage(documentsSecondModule.First().Value).Percent);
    }

    [Fact]
    public void TestCalculateLineCoveragePercentage_ArithmeticPrecisionCheck()
    {
      System.Collections.Generic.KeyValuePair<string, Documents> module = _moduleArithmeticPrecision.First();
      System.Collections.Generic.KeyValuePair<string, Classes> document = module.Value.First();
      System.Collections.Generic.KeyValuePair<string, Methods> @class = document.Value.First();
      System.Collections.Generic.KeyValuePair<string, Method> method = @class.Value.First();

      Assert.Equal(16.66, CoverageSummary.CalculateLineCoverage(_moduleArithmeticPrecision).AverageModulePercent);
      Assert.Equal(16.66, CoverageSummary.CalculateLineCoverage(module.Value).Percent);
      Assert.Equal(16.66, CoverageSummary.CalculateLineCoverage(document.Value).Percent);
      Assert.Equal(16.66, CoverageSummary.CalculateLineCoverage(@class.Value).Percent);
      Assert.Equal(16.66, CoverageSummary.CalculateLineCoverage(method.Value.Lines).Percent);
    }

    [Fact]
    public void TestCalculateBranchCoveragePercentage_ArithmeticPrecisionCheck()
    {
      System.Collections.Generic.KeyValuePair<string, Documents> module = _moduleArithmeticPrecision.First();
      System.Collections.Generic.KeyValuePair<string, Classes> document = module.Value.First();
      System.Collections.Generic.KeyValuePair<string, Methods> @class = document.Value.First();
      System.Collections.Generic.KeyValuePair<string, Method> method = @class.Value.First();

      Assert.Equal(16.66, CoverageSummary.CalculateBranchCoverage(_moduleArithmeticPrecision).AverageModulePercent);
      Assert.Equal(16.66, CoverageSummary.CalculateBranchCoverage(module.Value).Percent);
      Assert.Equal(16.66, CoverageSummary.CalculateBranchCoverage(document.Value).Percent);
      Assert.Equal(16.66, CoverageSummary.CalculateBranchCoverage(@class.Value).Percent);
      Assert.Equal(16.66, CoverageSummary.CalculateBranchCoverage(method.Value.Branches).Percent);
    }

    [Fact]
    public void TestCalculateBranchCoverage_IfWithoutElse_OneBranchCovered()
    {
      var branches = new Branches
      {
        new BranchInfo { Line = 45, Hits = 1, Offset = 1, Path = 0, Ordinal = 0 },
        new BranchInfo { Line = 45, Hits = 0, Offset = 1, Path = 1, Ordinal = 1 }
      };

      var methods = new Methods();
      string methodString = "System.Int32 Coverlet.Core.CoverageSamples.Tests.SelectionStatements::IfWithoutElse(System.Boolean)";
      methods.Add(methodString, new Method());
      methods[methodString].Branches = branches;

      var classes = new Classes
      {
        { "Coverlet.Core.CoverageSamples.Tests.SelectionStatements", methods }
      };

      var documents = new Documents
      {
        { "Instrumentation.SelectionStatements.cs", classes }
      };

      var modules = new Modules
      {
        { "module", documents }
      };

      Assert.Equal(50, CoverageSummary.CalculateBranchCoverage(modules).AverageModulePercent);
      Assert.Equal(50, CoverageSummary.CalculateBranchCoverage(documents).Percent);
      Assert.Equal(50, CoverageSummary.CalculateBranchCoverage(classes).Percent);
      Assert.Equal(50, CoverageSummary.CalculateBranchCoverage(methods).Percent);
      Assert.Equal(50, CoverageSummary.CalculateBranchCoverage(branches).Percent);
    }

    [Fact]
    public void BuildCoverageSummaryTable_EmptyModules_ContainsTotalAndAverageRowsWithZeroPercent()
    {
      var modules = new Modules();

      string result = CoverageSummary.BuildCoverageSummaryTable(modules);

      Assert.Contains("| Module", result);
      Assert.Contains("| Total", result);
      Assert.Contains("| Average", result);
      Assert.Contains("0%", result);
    }

    [Fact]
    public void BuildCoverageSummaryTable_SingleModule_ContainsModuleNameAndPercentages()
    {
      string result = CoverageSummary.BuildCoverageSummaryTable(_averageCalculationSingleModule);

      // Module name without extension appears in the per-module table
      Assert.Contains("module", result);

      // Header columns
      Assert.Contains("| Module", result);
      Assert.Contains("Line", result);
      Assert.Contains("Branch", result);
      Assert.Contains("Method", result);

      // Summary rows
      Assert.Contains("| Total", result);
      Assert.Contains("| Average", result);

      // Two table sections are present (the separator between them)
      Assert.Contains("50%", result);
      Assert.Contains("100%", result);
    }

    [Fact]
    public void BuildCoverageSummaryTable_MultipleModules_ContainsAllModuleNamesAndSummaryRows()
    {
      string result = CoverageSummary.BuildCoverageSummaryTable(_averageCalculationMultiModule);

      // Both module names appear in the output
      Assert.Contains("module", result);
      Assert.Contains("additionalModule", result);

      // Summary rows are present
      Assert.Contains("| Total", result);
      Assert.Contains("| Average", result);
    }

    [Fact]
    public void CalculateBranchCoverage_AllBranchesReachable_CountsAllBranches()
    {
      // Arrange - All branches are reachable
      var branches = new Branches
      {
        new BranchInfo { Line = 1, Hits = 1, Offset = 1, Path = 0, Ordinal = 1, IsReachable = true },
        new BranchInfo { Line = 1, Hits = 0, Offset = 1, Path = 1, Ordinal = 2, IsReachable = true }
      };

      // Act
      var result = CoverageSummary.CalculateBranchCoverage(branches);

      // Assert - Both branches count: 1 covered, 2 total = 50%
      Assert.Equal(2, result.Total);
      Assert.Equal(1, result.Covered);
      Assert.Equal(50, result.Percent);
    }

    [Fact]
    public void CalculateBranchCoverage_OneUnreachableBranch_ExcludesUnreachable()
    {
      // Arrange - One branch is unreachable (e.g., dead code after return)
      var branches = new Branches
      {
        new BranchInfo { Line = 1, Hits = 1, Offset = 1, Path = 0, Ordinal = 1, IsReachable = true },
        new BranchInfo { Line = 1, Hits = 0, Offset = 1, Path = 1, Ordinal = 2, IsReachable = false }
      };

      // Act
      var result = CoverageSummary.CalculateBranchCoverage(branches);

      // Assert - Only reachable branch counts: 1 covered, 1 total = 100%
      Assert.Equal(1, result.Total);
      Assert.Equal(1, result.Covered);
      Assert.Equal(100, result.Percent);
    }

    [Fact]
    public void CalculateBranchCoverage_AllBranchesUnreachable_ReturnsZero()
    {
      // Arrange - All branches are unreachable (edge case)
      var branches = new Branches
      {
        new BranchInfo { Line = 1, Hits = 1, Offset = 1, Path = 0, Ordinal = 1, IsReachable = false },
        new BranchInfo { Line = 1, Hits = 0, Offset = 1, Path = 1, Ordinal = 2, IsReachable = false }
      };

      // Act
      var result = CoverageSummary.CalculateBranchCoverage(branches);

      // Assert - No reachable branches: 0 total, 0 covered, 100% (no branches to measure)
      Assert.Equal(0, result.Total);
      Assert.Equal(0, result.Covered);
      Assert.Equal(100, result.Percent);
    }

    [Fact]
    public void CalculateBranchCoverage_EmptyBranches_ReturnsZero()
    {
      // Arrange
      var branches = new Branches();

      // Act
      var result = CoverageSummary.CalculateBranchCoverage(branches);

      // Assert
      Assert.Equal(0, result.Total);
      Assert.Equal(0, result.Covered);
      Assert.Equal(100, result.Percent);
    }

    [Fact]
    public void CalculateBranchCoverage_Issue2036_UnreachablePathNotCountedAsPartial()
    {
      // Arrange - Simulates issue #2036: if-return pattern where false branch is reachable
      // (both branches ARE reachable per control flow, but this test shows unreachable case)
      var branches = new Branches
      {
        // True branch: executes, returns (reachable, covered)
        new BranchInfo { Line = 91, Hits = 3, Offset = 103, Path = 0, Ordinal = 1, IsReachable = true },
        // False branch: unreachable (e.g., dead code alternative in nested condition)
        new BranchInfo { Line = 91, Hits = 0, Offset = 103, Path = 1, Ordinal = 2, IsReachable = false }
      };

      // Act
      var result = CoverageSummary.CalculateBranchCoverage(branches);

      // Assert - False branch excluded: 1 covered, 1 total = 100%
      // NOT "50% (1/2)" which was the original bug
      Assert.Equal(1, result.Total);
      Assert.Equal(1, result.Covered);
      Assert.Equal(100, result.Percent);
    }

    [Fact]
    public void BuildCoverageSummaryTable_SingleModule_TwoSectionsSeparatedByNewLine()
    {
      string result = CoverageSummary.BuildCoverageSummaryTable(_averageCalculationSingleModule);

      // The two tables are joined by Environment.NewLine, so at least two table sections exist
      int firstTableEnd = result.IndexOf("| Total", System.StringComparison.Ordinal);
      int moduleHeaderPos = result.IndexOf("| Module", System.StringComparison.Ordinal);

      Assert.True(moduleHeaderPos >= 0, "Module table header not found");
      Assert.True(firstTableEnd > moduleHeaderPos, "Summary table must follow the module table");
    }
  }
}
