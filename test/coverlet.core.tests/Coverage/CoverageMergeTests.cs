// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using Xunit;

namespace Coverlet.Core.Tests
{
  public class CoverageMergeTests
  {
    [Fact]
    public void Merge_AddsNewModules()
    {
      // Arrange
      var initialModules = new Modules
            {
                { "Module1", new Documents() }
            };
      var newModules = new Modules
            {
                { "Module2", new Documents() }
            };
      var coverageResult = new CoverageResult
      {
        Modules = initialModules
      };

      // Act
      coverageResult.Merge(newModules);

      // Assert
      Assert.Equal(2, coverageResult.Modules.Count);
      Assert.Contains("Module1", coverageResult.Modules.Keys);
      Assert.Contains("Module2", coverageResult.Modules.Keys);
    }

    [Fact]
    public void Merge_MergesDocumentsInExistingModules()
    {
      // Arrange
      var initialModules = new Modules
            {
                { "Module1", new Documents { { "Doc1", new Classes() } } }
            };
      var newModules = new Modules
            {
                { "Module1", new Documents { { "Doc2", new Classes() } } }
            };
      var coverageResult = new CoverageResult
      {
        Modules = initialModules
      };

      // Act
      coverageResult.Merge(newModules);

      // Assert
      Assert.Single(coverageResult.Modules);
      Assert.Equal(2, coverageResult.Modules["Module1"].Count);
      Assert.Contains("Doc1", coverageResult.Modules["Module1"].Keys);
      Assert.Contains("Doc2", coverageResult.Modules["Module1"].Keys);
    }

    [Fact]
    public void Merge_MergesClassesInExistingDocuments()
    {
      // Arrange
      var initialModules = new Modules
            {
                { "Module1", new Documents { { "Doc1", new Classes { { "Class1", new Methods() } } } } }
            };
      var newModules = new Modules
            {
                { "Module1", new Documents { { "Doc1", new Classes { { "Class2", new Methods() } } } } }
            };
      var coverageResult = new CoverageResult
      {
        Modules = initialModules
      };

      // Act
      coverageResult.Merge(newModules);

      // Assert
      Assert.Single(coverageResult.Modules);
      Assert.Single(coverageResult.Modules["Module1"]);
      Assert.Equal(2, coverageResult.Modules["Module1"]["Doc1"].Count);
      Assert.Contains("Class1", coverageResult.Modules["Module1"]["Doc1"].Keys);
      Assert.Contains("Class2", coverageResult.Modules["Module1"]["Doc1"].Keys);
    }

    [Fact]
    public void Merge_MergesMethodsInExistingClasses()
    {
      // Arrange
      var initialModules = new Modules
            {
                { "Module1", new Documents { { "Doc1", new Classes { { "Class1", new Methods { { "Method1", new Method() } } } } } } }
            };
      var newModules = new Modules
            {
                { "Module1", new Documents { { "Doc1", new Classes { { "Class1", new Methods { { "Method2", new Method() } } } } } } }
            };
      var coverageResult = new CoverageResult
      {
        Modules = initialModules
      };

      // Act
      coverageResult.Merge(newModules);

      // Assert
      Assert.Single(coverageResult.Modules);
      Assert.Single(coverageResult.Modules["Module1"]);
      Assert.Single(coverageResult.Modules["Module1"]["Doc1"]);
      Assert.Equal(2, coverageResult.Modules["Module1"]["Doc1"]["Class1"].Count);
      Assert.Contains("Method1", coverageResult.Modules["Module1"]["Doc1"]["Class1"].Keys);
      Assert.Contains("Method2", coverageResult.Modules["Module1"]["Doc1"]["Class1"].Keys);
    }

    [Fact]
    public void Merge_MergesLinesInExistingMethods()
    {
      // Arrange
      var initialModules = new Modules
            {
                { "Module1", new Documents { { "Doc1", new Classes { { "Class1", new Methods { { "Method1", new Method { Lines = new Lines { { 1, 1 } } } } } } } } } }
            };
      var newModules = new Modules
            {
                { "Module1", new Documents { { "Doc1", new Classes { { "Class1", new Methods { { "Method1", new Method { Lines = new Lines { { 1, 2 }, { 2, 1 } } } } } } } } } }
            };
      var coverageResult = new CoverageResult
      {
        Modules = initialModules
      };

      // Act
      coverageResult.Merge(newModules);

      // Assert
      Assert.Single(coverageResult.Modules);
      Assert.Single(coverageResult.Modules["Module1"]);
      Assert.Single(coverageResult.Modules["Module1"]["Doc1"]);
      Assert.Single(coverageResult.Modules["Module1"]["Doc1"]["Class1"]);
      Assert.Equal(2, coverageResult.Modules["Module1"]["Doc1"]["Class1"]["Method1"].Lines.Count);
      Assert.Equal(3, coverageResult.Modules["Module1"]["Doc1"]["Class1"]["Method1"].Lines[1]);
      Assert.Equal(1, coverageResult.Modules["Module1"]["Doc1"]["Class1"]["Method1"].Lines[2]);
    }

    [Fact]
    public void Merge_MergesBranchesInExistingMethods()
    {
      // Arrange
      var initialModules = new Modules
            {
                { "Module1", new Documents { { "Doc1", new Classes { { "Class1", new Methods { { "Method1", new Method { Branches = new Branches { new BranchInfo { Line = 1, Hits = 1, Offset = 1, Path = 0, Ordinal = 1 } } } } } } } } } }
            };
      var newModules = new Modules
            {
                { "Module1", new Documents { { "Doc1", new Classes { { "Class1", new Methods { { "Method1", new Method { Branches = new Branches { new BranchInfo { Line = 1, Hits = 1, Offset = 1, Path = 1, Ordinal = 2 } } } } } } } } } }
            };
      var coverageResult = new CoverageResult
      {
        Modules = initialModules
      };

      // Act
      coverageResult.Merge(newModules);

      // Assert
      Assert.Single(coverageResult.Modules);
      Assert.Single(coverageResult.Modules["Module1"]);
      Assert.Single(coverageResult.Modules["Module1"]["Doc1"]);
      Assert.Single(coverageResult.Modules["Module1"]["Doc1"]["Class1"]);
      Assert.Equal(2, coverageResult.Modules["Module1"]["Doc1"]["Class1"]["Method1"].Branches.Count);
      Assert.Equal(1, coverageResult.Modules["Module1"]["Doc1"]["Class1"]["Method1"].Branches.First(b => b.Line == 1 && b.Offset == 1 && b.Path == 0 && b.Ordinal == 1).Hits);
      Assert.Equal(1, coverageResult.Modules["Module1"]["Doc1"]["Class1"]["Method1"].Branches.First(b => b.Line == 1 && b.Offset == 1 && b.Path == 1 && b.Ordinal == 2).Hits);
    }
  }
}

