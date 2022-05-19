// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using FluentAssertions;
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
            var lines = new Lines();
            lines.Add(1, 1);
            for (int i = 2; i <= 6; i++)
            {
                lines.Add(i, 0);
            }
            var branches = new Branches();
            branches.Add(new BranchInfo { Line = 1, Hits = 1, Offset = 1, Path = 0, Ordinal = 1 });
            for (int i = 2; i <= 6; i++)
            {
                branches.Add(new BranchInfo { Line = 1, Hits = 0, Offset = 1, Path = 1, Ordinal = (uint)i });
            }

            var methods = new Methods();
            string methodString = "System.Void Coverlet.Core.Tests.CoverageSummaryTests::TestCalculateSummary()";
            methods.Add(methodString, new Method());
            methods[methodString].Lines = lines;
            methods[methodString].Branches = branches;

            var classes = new Classes();
            classes.Add("Coverlet.Core.Tests.CoverageSummaryTests", methods);

            var documents = new Documents();
            documents.Add("doc.cs", classes);

            _moduleArithmeticPrecision = new Modules();
            _moduleArithmeticPrecision.Add("module", documents);
        }

        private void SetupDataSingleModule()
        {
            var lines = new Lines();
            lines.Add(1, 1);
            lines.Add(2, 0);
            var branches = new Branches();
            branches.Add(new BranchInfo { Line = 1, Hits = 1, Offset = 1, Path = 0, Ordinal = 1 });
            branches.Add(new BranchInfo { Line = 1, Hits = 1, Offset = 1, Path = 1, Ordinal = 2 });

            var methods = new Methods();
            string methodString = "System.Void Coverlet.Core.Tests.CoverageSummaryTests::TestCalculateSummary()";
            methods.Add(methodString, new Method());
            methods[methodString].Lines = lines;
            methods[methodString].Branches = branches;

            var classes = new Classes();
            classes.Add("Coverlet.Core.Tests.CoverageSummaryTests", methods);

            var documents = new Documents();
            documents.Add("doc.cs", classes);

            _averageCalculationSingleModule = new Modules();
            _averageCalculationSingleModule.Add("module", documents);
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
                "System.Void Coverlet.Core.Tests.CoverageSummaryTests::TestAditionalCalculateSummary()" // not covered
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
                { "aditionalModule", documents }
            };
        }
        
        [Fact]
        public void TestCalculateLineCoverage_NoModules()
        {
            var summary = new CoverageSummary();
            var modules = new Modules();

            summary.CalculateLineCoverage(modules).Percent.Should().Be(0);
            summary.CalculateLineCoverage(modules).AverageModulePercent.Should().Be(0);
            summary.CalculateBranchCoverage(modules).Percent.Should().Be(0);
            summary.CalculateBranchCoverage(modules).AverageModulePercent.Should().Be(0);
            summary.CalculateMethodCoverage(modules).Percent.Should().Be(0);
            summary.CalculateMethodCoverage(modules).AverageModulePercent.Should().Be(0);
        }

        [Fact]
        public void TestCalculateLineCoverage_SingleModule()
        {
            var summary = new CoverageSummary();

            System.Collections.Generic.KeyValuePair<string, Documents> module = _averageCalculationSingleModule.First();
            System.Collections.Generic.KeyValuePair<string, Classes> document = module.Value.First();
            System.Collections.Generic.KeyValuePair<string, Methods> @class = document.Value.First();
            System.Collections.Generic.KeyValuePair<string, Method> method = @class.Value.First();

            summary.CalculateLineCoverage(_averageCalculationSingleModule).AverageModulePercent.Should().Be(50);
            summary.CalculateLineCoverage(module.Value).Percent.Should().Be(50);
            summary.CalculateLineCoverage(document.Value).Percent.Should().Be(50);
            summary.CalculateLineCoverage(@class.Value).Percent.Should().Be(50);
            summary.CalculateLineCoverage(method.Value.Lines).Percent.Should().Be(50);
        }

        [Fact]
        public void TestCalculateLineCoverage_MultiModule()
        {
            var summary = new CoverageSummary();
            Documents documentsFirstModule = _averageCalculationMultiModule["module"];
            Documents documentsSecondModule = _averageCalculationMultiModule["aditionalModule"];

            summary.CalculateLineCoverage(_averageCalculationMultiModule).AverageModulePercent.Should().Be(37.5);
            summary.CalculateLineCoverage(documentsFirstModule.First().Value).Percent.Should().Be(50);

            summary.CalculateLineCoverage(documentsSecondModule.First().Value.First().Value.ElementAt(0).Value.Lines).Percent.Should().Be(33.33); // covered 1 of 3
            summary.CalculateLineCoverage(documentsSecondModule.First().Value.First().Value.ElementAt(1).Value.Lines).Percent.Should().Be(0); // covered 0 of 1
            summary.CalculateLineCoverage(documentsSecondModule.First().Value).Percent.Should().Be(25); // covered 1 of 4 lines
        }

        [Fact]
        public void TestCalculateBranchCoverage_SingleModule()
        {
            var summary = new CoverageSummary();

            System.Collections.Generic.KeyValuePair<string, Documents> module = _averageCalculationSingleModule.First();
            System.Collections.Generic.KeyValuePair<string, Classes> document = module.Value.First();
            System.Collections.Generic.KeyValuePair<string, Methods> @class = document.Value.First();
            System.Collections.Generic.KeyValuePair<string, Method> method = @class.Value.First();

            summary.CalculateBranchCoverage(_averageCalculationSingleModule).AverageModulePercent.Should().Be(100);
            summary.CalculateBranchCoverage(module.Value).Percent.Should().Be(100);
            summary.CalculateBranchCoverage(document.Value).Percent.Should().Be(100);
            summary.CalculateBranchCoverage(@class.Value).Percent.Should().Be(100);
            summary.CalculateBranchCoverage(method.Value.Branches).Percent.Should().Be(100);
        }

        [Fact]
        public void TestCalculateBranchCoverage_MultiModule()
        {
            var summary = new CoverageSummary();
            Documents documentsFirstModule = _averageCalculationMultiModule["module"];
            Documents documentsSecondModule = _averageCalculationMultiModule["aditionalModule"];

            summary.CalculateBranchCoverage(_averageCalculationMultiModule).AverageModulePercent.Should().Be(83.33);
            summary.CalculateBranchCoverage(documentsFirstModule.First().Value).Percent.Should().Be(100);
            summary.CalculateBranchCoverage(documentsSecondModule.First().Value).Percent.Should().Be(66.66);
        }

        [Fact]
        public void TestCalculateMethodCoverage_SingleModule()
        {
            var summary = new CoverageSummary();

            System.Collections.Generic.KeyValuePair<string, Documents> module = _averageCalculationSingleModule.First();
            System.Collections.Generic.KeyValuePair<string, Classes> document = module.Value.First();
            System.Collections.Generic.KeyValuePair<string, Methods> @class = document.Value.First();
            System.Collections.Generic.KeyValuePair<string, Method> method = @class.Value.First();

            summary.CalculateMethodCoverage(_averageCalculationSingleModule).AverageModulePercent.Should().Be(100);
            summary.CalculateMethodCoverage(module.Value).Percent.Should().Be(100);
            summary.CalculateMethodCoverage(document.Value).Percent.Should().Be(100);
            summary.CalculateMethodCoverage(@class.Value).Percent.Should().Be(100);
            summary.CalculateMethodCoverage(method.Value.Lines).Percent.Should().Be(100);
        }

        [Fact]
        public void TestCalculateMethodCoverage_MultiModule()
        {
            var summary = new CoverageSummary();
            Documents documentsFirstModule = _averageCalculationMultiModule["module"];
            Documents documentsSecondModule = _averageCalculationMultiModule["aditionalModule"];

            summary.CalculateMethodCoverage(_averageCalculationMultiModule).AverageModulePercent.Should().Be(75);
            summary.CalculateMethodCoverage(documentsFirstModule.First().Value).Percent.Should().Be(100);
            summary.CalculateMethodCoverage(documentsSecondModule.First().Value).Percent.Should().Be(50);
        }

        [Fact]
        public void TestCalculateLineCoveragePercentage_ArithmeticPrecisionCheck()
        {
            var summary = new CoverageSummary();

            System.Collections.Generic.KeyValuePair<string, Documents> module = _moduleArithmeticPrecision.First();
            System.Collections.Generic.KeyValuePair<string, Classes> document = module.Value.First();
            System.Collections.Generic.KeyValuePair<string, Methods> @class = document.Value.First();
            System.Collections.Generic.KeyValuePair<string, Method> method = @class.Value.First();

            summary.CalculateLineCoverage(_moduleArithmeticPrecision).AverageModulePercent.Should().Be(16.66);
            summary.CalculateLineCoverage(module.Value).Percent.Should().Be(16.66);
            summary.CalculateLineCoverage(document.Value).Percent.Should().Be(16.66);
            summary.CalculateLineCoverage(@class.Value).Percent.Should().Be(16.66);
            summary.CalculateLineCoverage(method.Value.Lines).Percent.Should().Be(16.66);
        }

        [Fact]
        public void TestCalculateBranchCoveragePercentage_ArithmeticPrecisionCheck()
        {
            var summary = new CoverageSummary();

            System.Collections.Generic.KeyValuePair<string, Documents> module = _moduleArithmeticPrecision.First();
            System.Collections.Generic.KeyValuePair<string, Classes> document = module.Value.First();
            System.Collections.Generic.KeyValuePair<string, Methods> @class = document.Value.First();
            System.Collections.Generic.KeyValuePair<string, Method> method = @class.Value.First();

            summary.CalculateBranchCoverage(_moduleArithmeticPrecision).AverageModulePercent.Should().Be(16.66);
            summary.CalculateBranchCoverage(module.Value).Percent.Should().Be(16.66);
            summary.CalculateBranchCoverage(document.Value).Percent.Should().Be(16.66);
            summary.CalculateBranchCoverage(@class.Value).Percent.Should().Be(16.66);
            summary.CalculateBranchCoverage(method.Value.Branches).Percent.Should().Be(16.66);
        }
    }
}
