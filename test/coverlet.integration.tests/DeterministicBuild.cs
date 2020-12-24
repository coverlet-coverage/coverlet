using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

using Coverlet.Core;
using Newtonsoft.Json;
using Xunit;

namespace Coverlet.Integration.Tests
{
    public class DeterministicBuild : BaseTest, IDisposable
    {
        private readonly string _testProjectPath = Path.GetFullPath("../../../../coverlet.integration.determisticbuild");
        private string? _testProjectTfm;
        private const string PropsFileName = "DeterministicTest.props";
        private readonly string _buildConfiguration;

        public DeterministicBuild()
        {
            _buildConfiguration = GetAssemblyBuildConfiguration().ToString();
        }

        private void CreateDeterministicTestPropsFile()
        {
            XDocument deterministicTestProps = new XDocument();
            deterministicTestProps.Add(
                    new XElement("Project",
                        new XElement("PropertyGroup",
                            new XElement("coverletMsbuilVersion", GetPackageVersion("*msbuild*.nupkg")),
                            new XElement("coverletCollectorsVersion", GetPackageVersion("*collector*.nupkg")))));
            _testProjectTfm = XElement.Load(Path.Combine(_testProjectPath, "coverlet.integration.determisticbuild.csproj"))!.
                             Descendants("PropertyGroup")!.Single().Element("TargetFramework")!.Value;

            deterministicTestProps.Save(Path.Combine(_testProjectPath, PropsFileName));
        }

        private protected void AssertCoverage(string standardOutput = "")
        {
            if (_buildConfiguration == "Debug")
            {
                bool coverageChecked = false;
                string reportFilePath = "";
                foreach (string coverageFile in Directory.GetFiles(_testProjectPath, "coverage.json", SearchOption.AllDirectories))
                {
                    JsonConvert.DeserializeObject<Modules>(File.ReadAllText(coverageFile))
                    .Document("DeepThought.cs")
                    .Class("Coverlet.Integration.DeterministicBuild.DeepThought")
                    .Method("System.Int32 Coverlet.Integration.DeterministicBuild.DeepThought::AnswerToTheUltimateQuestionOfLifeTheUniverseAndEverything()")
                    .AssertLinesCovered((6, 1), (7, 1), (8, 1));
                    coverageChecked = true;
                    reportFilePath = coverageFile;
                }
                Assert.True(coverageChecked, $"Coverage check fail\n{standardOutput}");
                File.Delete(reportFilePath);
                Assert.False(File.Exists(reportFilePath));
            }
        }

        [Fact]
        public void Msbuild()
        {
            CreateDeterministicTestPropsFile();
            DotnetCli($"build -c {_buildConfiguration} /p:DeterministicSourcePaths=true", out string standardOutput, out string _, _testProjectPath);
            Assert.Contains("Build succeeded.", standardOutput);
            string sourceRootMappingFilePath = Path.Combine(_testProjectPath, "bin", _buildConfiguration, _testProjectTfm!, "CoverletSourceRootsMapping");
            Assert.True(File.Exists(sourceRootMappingFilePath), sourceRootMappingFilePath);
            Assert.True(!string.IsNullOrEmpty(File.ReadAllText(sourceRootMappingFilePath)), "Empty CoverletSourceRootsMapping file");
            Assert.Contains("=/_/", File.ReadAllText(sourceRootMappingFilePath));

            DotnetCli($"test -c {_buildConfiguration} --no-build /p:CollectCoverage=true /p:Include=\"[coverletsample.integration.determisticbuild]*DeepThought\" /p:IncludeTestAssembly=true", out standardOutput, out _, _testProjectPath);
            Assert.Contains("Passed!", standardOutput);
            Assert.Contains("| coverletsample.integration.determisticbuild | 100% | 100%   | 100%   |", standardOutput);
            Assert.True(File.Exists(Path.Combine(_testProjectPath, "coverage.json")));
            AssertCoverage(standardOutput);

            // Process exits hang on clean seem that process doesn't close, maybe some mbuild node reuse? btw manually tested
            // DotnetCli("clean", out standardOutput, out standardError, _fixture.TestProjectPath);
            // Assert.False(File.Exists(sourceRootMappingFilePath));
            RunCommand("git", "clean -fdx", out _, out _, _testProjectPath);
        }

        [Fact]
        public void Msbuild_SourceLink()
        {
            CreateDeterministicTestPropsFile();
            DotnetCli($"build -c {_buildConfiguration} /p:DeterministicSourcePaths=true", out string standardOutput, out string _, _testProjectPath);
            Assert.Contains("Build succeeded.", standardOutput);
            string sourceRootMappingFilePath = Path.Combine(_testProjectPath, "bin", _buildConfiguration, _testProjectTfm!, "CoverletSourceRootsMapping");
            Assert.True(File.Exists(sourceRootMappingFilePath), sourceRootMappingFilePath);
            Assert.True(!string.IsNullOrEmpty(File.ReadAllText(sourceRootMappingFilePath)), "Empty CoverletSourceRootsMapping file");
            Assert.Contains("=/_/", File.ReadAllText(sourceRootMappingFilePath));

            DotnetCli($"test -c {_buildConfiguration} --no-build /p:CollectCoverage=true /p:UseSourceLink=true /p:Include=\"[coverletsample.integration.determisticbuild]*DeepThought\" /p:IncludeTestAssembly=true", out standardOutput, out _, _testProjectPath);
            Assert.Contains("Passed!", standardOutput);
            Assert.Contains("| coverletsample.integration.determisticbuild | 100% | 100%   | 100%   |", standardOutput);
            Assert.True(File.Exists(Path.Combine(_testProjectPath, "coverage.json")));
            Assert.Contains("raw.githubusercontent.com", File.ReadAllText(Path.Combine(_testProjectPath, "coverage.json")));
            AssertCoverage(standardOutput);

            // Process exits hang on clean seem that process doesn't close, maybe some mbuild node reuse? btw manually tested
            // DotnetCli("clean", out standardOutput, out standardError, _fixture.TestProjectPath);
            // Assert.False(File.Exists(sourceRootMappingFilePath));
            RunCommand("git", "clean -fdx", out _, out _, _testProjectPath);
        }

        [Fact]
        public void Collectors()
        {
            CreateDeterministicTestPropsFile();
            DotnetCli($"build -c {_buildConfiguration} /p:DeterministicSourcePaths=true", out string standardOutput, out string _, _testProjectPath);
            Assert.Contains("Build succeeded.", standardOutput);
            string sourceRootMappingFilePath = Path.Combine(_testProjectPath, "bin", GetAssemblyBuildConfiguration().ToString(), _testProjectTfm!, "CoverletSourceRootsMapping");
            Assert.True(File.Exists(sourceRootMappingFilePath), sourceRootMappingFilePath);
            Assert.NotEmpty(File.ReadAllText(sourceRootMappingFilePath));
            Assert.Contains("=/_/", File.ReadAllText(sourceRootMappingFilePath));

            string runSettingsPath = AddCollectorRunsettingsFile(_testProjectPath, "[coverletsample.integration.determisticbuild]*DeepThought");
            Assert.True(DotnetCli($"test -c {_buildConfiguration} --no-build \"{_testProjectPath}\" --collect:\"XPlat Code Coverage\" --settings \"{runSettingsPath}\" --diag:{Path.Combine(_testProjectPath, "log.txt")}", out standardOutput, out _), standardOutput);
            Assert.Contains("Passed!", standardOutput);
            AssertCoverage(standardOutput);

            // Check out/in process collectors injection
            string dataCollectorLogContent = File.ReadAllText(Directory.GetFiles(_testProjectPath, "log.datacollector.*.txt").Single());
            Assert.Contains("[coverlet]Initializing CoverletCoverageDataCollector with configuration:", dataCollectorLogContent);
            Assert.Contains("[coverlet]Initialize CoverletInProcDataCollector", File.ReadAllText(Directory.GetFiles(_testProjectPath, "log.host.*.txt").Single()));
            Assert.Contains("[coverlet]Mapping resolved", dataCollectorLogContent);

            // Process exits hang on clean seem that process doesn't close, maybe some mbuild node reuse? btw manually tested
            // DotnetCli("clean", out standardOutput, out standardError, _fixture.TestProjectPath);
            // Assert.False(File.Exists(sourceRootMappingFilePath));
            RunCommand("git", "clean -fdx", out _, out _, _testProjectPath);
        }

        [Fact]
        public void Collectors_SourceLink()
        {
            CreateDeterministicTestPropsFile();
            DotnetCli($"build -c {_buildConfiguration} /p:DeterministicSourcePaths=true", out string standardOutput, out string _, _testProjectPath);
            Assert.Contains("Build succeeded.", standardOutput);
            string sourceRootMappingFilePath = Path.Combine(_testProjectPath, "bin", GetAssemblyBuildConfiguration().ToString(), _testProjectTfm!, "CoverletSourceRootsMapping");
            Assert.True(File.Exists(sourceRootMappingFilePath), sourceRootMappingFilePath);
            Assert.NotEmpty(File.ReadAllText(sourceRootMappingFilePath));
            Assert.Contains("=/_/", File.ReadAllText(sourceRootMappingFilePath));

            string runSettingsPath = AddCollectorRunsettingsFile(_testProjectPath, "[coverletsample.integration.determisticbuild]*DeepThought", sourceLink: true);
            Assert.True(DotnetCli($"test -c {_buildConfiguration} --no-build \"{_testProjectPath}\" --collect:\"XPlat Code Coverage\" --settings \"{runSettingsPath}\" --diag:{Path.Combine(_testProjectPath, "log.txt")}", out standardOutput, out _), standardOutput);
            Assert.Contains("Passed!", standardOutput);
            AssertCoverage(standardOutput);
            Assert.Contains("raw.githubusercontent.com", File.ReadAllText(Directory.GetFiles(_testProjectPath, "coverage.cobertura.xml", SearchOption.AllDirectories).Single()));

            // Check out/in process collectors injection
            string dataCollectorLogContent = File.ReadAllText(Directory.GetFiles(_testProjectPath, "log.datacollector.*.txt").Single());
            Assert.Contains("[coverlet]Initializing CoverletCoverageDataCollector with configuration:", dataCollectorLogContent);
            Assert.Contains("[coverlet]Initialize CoverletInProcDataCollector", File.ReadAllText(Directory.GetFiles(_testProjectPath, "log.host.*.txt").Single()));
            Assert.Contains("[coverlet]Mapping resolved", dataCollectorLogContent);

            // Process exits hang on clean seem that process doesn't close, maybe some mbuild node reuse? btw manually tested
            // DotnetCli("clean", out standardOutput, out standardError, _fixture.TestProjectPath);
            // Assert.False(File.Exists(sourceRootMappingFilePath));
            RunCommand("git", "clean -fdx", out _, out _, _testProjectPath);
        }

        public void Dispose()
        {
            File.Delete(Path.Combine(_testProjectPath, PropsFileName));
        }
    }
}
