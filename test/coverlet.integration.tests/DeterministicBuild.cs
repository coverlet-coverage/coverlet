// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Coverlet.Core;
using Coverlet.Tests.Utils;
using Newtonsoft.Json;
using Xunit;

namespace Coverlet.Integration.Tests
{
  public class DeterministicBuild : BaseTest, IDisposable
  {
    private static readonly string s_projectName = "coverlet.integration.determisticbuild";
    private readonly string _buildTargetFramework;
    private string[] _testProjectTfms = [];
    private readonly string _testProjectPath = TestUtils.GetTestProjectPath(s_projectName);
    private readonly string _testBinaryPath = TestUtils.GetTestBinaryPath(s_projectName);
    private readonly string _testResultsPath = TestUtils.GetTestResultsPath();
    private const string PropsFileName = "DeterministicTest.props";
    private readonly string _buildConfiguration;
    private readonly ITestOutputHelper _output;
    private readonly Type _type;
    private readonly FieldInfo? _testMember;
    private readonly string _artifactsPivot;

    public DeterministicBuild(ITestOutputHelper output)
    {
      _buildConfiguration = TestUtils.GetAssemblyBuildConfiguration().ToString();
      _buildTargetFramework = TestUtils.GetAssemblyTargetFramework();
      _artifactsPivot = _buildConfiguration + "_" + _buildTargetFramework;
      _output = output;
      _type = output.GetType();
      _testMember = _type.GetField("test", BindingFlags.Instance | BindingFlags.NonPublic);
    }

    private void CreateDeterministicTestPropsFile()
    {
      string propsFile = Path.Combine(_testProjectPath, PropsFileName);
      File.Delete(propsFile);

      XDocument deterministicTestProps = new();
      deterministicTestProps.Add(
              new XElement("Project",
                  new XElement("PropertyGroup",
                      new XElement("coverletMsbuildVersion", GetPackageVersion("*msbuild*.nupkg")),
                      new XElement("coverletCollectorsVersion", GetPackageVersion("*collector*.nupkg")))));
      _testProjectTfms = XElement.Load(Path.Combine(_testProjectPath, "coverlet.integration.determisticbuild.csproj"))!
                         .Descendants("PropertyGroup")!
                         .Single()
                         .Element("TargetFrameworks")!
                         .Value
                         .Split(';');

      Assert.Contains(_buildTargetFramework, _testProjectTfms);

      deterministicTestProps.Save(Path.Combine(propsFile));
    }

    private protected void AssertCoverage(string standardOutput = "", string reportName = "", bool checkDeterministicReport = true)
    {
      if (_buildConfiguration == "Debug")
      {
        bool coverageChecked = false;
        string reportFilePath = "";
        foreach (string coverageFile in Directory.GetFiles(GetReportPath(standardOutput, reportName), reportName, SearchOption.AllDirectories))
        {
          Classes? document = JsonConvert.DeserializeObject<Modules>(File.ReadAllText(coverageFile))?.Document("DeepThought.cs");
          if (document != null)
          {
            document.Class("Coverlet.Integration.DeterministicBuild.DeepThought")
                .Method("System.Int32 Coverlet.Integration.DeterministicBuild.DeepThought::AnswerToTheUltimateQuestionOfLifeTheUniverseAndEverything()")
                .AssertLinesCovered((6, 1), (7, 1), (8, 1));
            coverageChecked = true;
            reportFilePath = coverageFile;
          }
        }
        Assert.True(coverageChecked, $"Coverage check fail\n{standardOutput}");
        File.Delete(reportFilePath);
        Assert.False(File.Exists(reportFilePath));

        if (checkDeterministicReport)
        {
          string newName = reportName.Replace("json", "cobertura.xml");
          // Verify deterministic report
          foreach (string coverageFile in Directory.GetFiles(GetReportPath(standardOutput, newName), newName, SearchOption.AllDirectories))
          {
            Assert.Contains("/_/test/coverlet.integration.determisticbuild/DeepThought.cs", File.ReadAllText(coverageFile));
            File.Delete(coverageFile);
          }
        }
      }
    }

    [Fact]
    public void Msbuild()
    {
      string testResultPath = Path.Join(_testResultsPath, $"{TestContext.Current.TestClass?.TestClassName}.{TestContext.Current.TestMethod?.MethodName}");
      string logFilename = $"{TestContext.Current.TestClass?.TestClassName}.{TestContext.Current.TestMethod?.MethodName}.binlog";
      CreateDeterministicTestPropsFile();

      DotnetCli($"build -c {_buildConfiguration} -f {_buildTargetFramework} -bl:build.{logFilename} /p:DeterministicSourcePaths=true", out string buildOutput, out string buildError, _testProjectPath);
      if (!string.IsNullOrEmpty(buildError))
      {
        _output.WriteLine(buildError);
      }
      else
      {
        _output.WriteLine(buildOutput);
      }
      Assert.Contains("Build succeeded.", buildOutput);
      string sourceRootMappingFilePath = Path.Combine(_testBinaryPath, _artifactsPivot.ToLowerInvariant(), "CoverletSourceRootsMapping_coverletsample.integration.determisticbuild");
      Assert.True(File.Exists(sourceRootMappingFilePath), $"File not found: {sourceRootMappingFilePath}");
      Assert.False(string.IsNullOrEmpty(File.ReadAllText(sourceRootMappingFilePath)));
      Assert.Contains("=/_/", File.ReadAllText(sourceRootMappingFilePath));

      string cmdArgument = $"test -c {_buildConfiguration} -f {_buildTargetFramework} --no-build /p:CollectCoverage=true /p:DeterministicReport=true /p:CoverletOutputFormat=\"cobertura%2cjson\" /p:Include=\"[coverletsample.integration.determisticbuild]*DeepThought\" /p:IncludeTestAssembly=true --results-directory:{testResultPath}";
      _output.WriteLine($"Command: dotnet {cmdArgument}");
      int result = DotnetCli(cmdArgument, out string standardOutput, out string standardError, _testProjectPath);
      if (!string.IsNullOrEmpty(standardError))
      {
        _output.WriteLine(standardError);
      }
      else
      {
        _output.WriteLine(standardOutput);
      }
      Assert.Equal(0, result);
      Assert.Contains("Passed!", standardOutput);
      Assert.Contains("| coverletsample.integration.determisticbuild | 100% | 100%   | 100%   |", standardOutput);
      string testResultFile = Path.Join(_testProjectPath, $"coverage.{_buildTargetFramework}.json");
      Assert.True(File.Exists(testResultFile), $"File '{testResultFile}' does not exist");
      AssertCoverage(standardOutput, $"coverage.{_buildTargetFramework}.json");

      CleanupBuildOutput();
    }

    [Fact]
    public void Msbuild_SourceLink()
    {
      string testResultPath = Path.Join(_testResultsPath, $"{TestContext.Current.TestClass?.TestClassName}.{TestContext.Current.TestMethod?.MethodName}");
      string logFilename = $"{TestContext.Current.TestClass?.TestClassName}.{TestContext.Current.TestMethod?.MethodName}.binlog";
      CreateDeterministicTestPropsFile();

      DotnetCli($"build -c {_buildConfiguration} -f {_buildTargetFramework} -bl:build.{logFilename} --verbosity normal /p:DeterministicSourcePaths=true", out string buildOutput, out string buildError, _testProjectPath);
      if (!string.IsNullOrEmpty(buildError))
      {
        _output.WriteLine(buildError);
      }
      else
      {
        _output.WriteLine(buildOutput);
      }
      Assert.Contains("Build succeeded.", buildOutput);
      string sourceRootMappingFilePath = Path.Combine(_testBinaryPath, _artifactsPivot.ToLowerInvariant(), "CoverletSourceRootsMapping_coverletsample.integration.determisticbuild");

      Assert.True(File.Exists(sourceRootMappingFilePath), $"File not found: {sourceRootMappingFilePath}");
      Assert.False(string.IsNullOrEmpty(File.ReadAllText(sourceRootMappingFilePath)));
      Assert.Contains("=/_/", File.ReadAllText(sourceRootMappingFilePath));

      string cmdArgument = $"test -c {_buildConfiguration} -f {_buildTargetFramework} --no-build /p:CollectCoverage=true /p:CoverletOutputFormat=\"cobertura%2cjson\" /p:UseSourceLink=true /p:Include=\"[coverletsample.integration.determisticbuild]*DeepThought\" /p:IncludeTestAssembly=true --results-directory:{testResultPath}";
      _output.WriteLine($"Command: dotnet {cmdArgument}");
      int result = DotnetCli(cmdArgument, out string standardOutput, out string standardError, _testProjectPath);
      if (!string.IsNullOrEmpty(standardError))
      {
        _output.WriteLine(standardError);
      }
      else
      {
        _output.WriteLine(standardOutput);
      }
      Assert.Equal(0, result);
      Assert.Contains("Passed!", standardOutput);
      Assert.Contains("| coverletsample.integration.determisticbuild | 100% | 100%   | 100%   |", standardOutput);
      string testResultFile = Path.Join(_testProjectPath, $"coverage.{_buildTargetFramework}.json");
      Assert.True(File.Exists(testResultFile), $"File '{testResultFile}' does not exist");
      Assert.Contains("raw.githubusercontent.com", File.ReadAllText(testResultFile));
      AssertCoverage(standardOutput, $"coverage.{_buildTargetFramework}.json", checkDeterministicReport: false);

      CleanupBuildOutput();
    }

    [Fact]
    public void Collectors()
    {
      string testResultPath = Path.Join(_testResultsPath, $"{TestContext.Current.TestClass?.TestClassName}.{TestContext.Current.TestMethod?.MethodName}");
      string testLogFilesPath = Path.Join(_testResultsPath, $"{TestContext.Current.TestClass?.TestClassName}.{TestContext.Current.TestMethod?.MethodName}", "log");
      string logFilename = $"{TestContext.Current.TestClass?.TestClassName}.{TestContext.Current.TestMethod?.MethodName}.binlog";

      CreateDeterministicTestPropsFile();
      DeleteLogFiles(testLogFilesPath);
      DeleteCoverageFiles(testResultPath);

      DotnetCli($"build -c {_buildConfiguration} -f {_buildTargetFramework} -bl:build.{logFilename} --verbosity normal /p:DeterministicSourcePaths=true", out string buildOutput, out string buildError, _testProjectPath);
      if (!string.IsNullOrEmpty(buildError))
      {
        _output.WriteLine(buildError);
      }
      else
      {
        _output.WriteLine(buildOutput);
      }
      Assert.Contains("Build succeeded.", buildOutput);
      string sourceRootMappingFilePath = Path.Combine(_testBinaryPath, _artifactsPivot.ToLowerInvariant(), "CoverletSourceRootsMapping_coverletsample.integration.determisticbuild");

      Assert.True(File.Exists(sourceRootMappingFilePath), $"File not found: {sourceRootMappingFilePath}");
      Assert.NotEmpty(File.ReadAllText(sourceRootMappingFilePath));
      Assert.Contains("=/_/", File.ReadAllText(sourceRootMappingFilePath));

      string runSettingsPath = AddCollectorRunsettingsFile(_testProjectPath, "[coverletsample.integration.determisticbuild]*DeepThought", deterministicReport: true);
      string cmdArgument = $"test -c {_buildConfiguration} -f {_buildTargetFramework} --no-build --collect:\"XPlat Code Coverage\" --results-directory:\"{testResultPath}\" --settings \"{runSettingsPath}\" --diag:{Path.Combine(testLogFilesPath, "log.txt")}";
      _output.WriteLine($"Command: dotnet {cmdArgument}");
      int result = DotnetCli(cmdArgument, out string standardOutput, out string standardError, _testProjectPath);
      if (!string.IsNullOrEmpty(standardError))
      {
        _output.WriteLine(standardError);
      }
      else
      {
        _output.WriteLine(standardOutput);
      }
      Assert.Equal(0, result);
      Assert.Contains("Passed!", standardOutput);
      AssertCoverage(standardOutput, "coverage.json");

      // delete irrelevant generated files
      DeleteTestIntermediateFiles(testResultPath);

      // Check out/in process collectors injection
      string dataCollectorLogContent = File.ReadAllText(Directory.GetFiles(testLogFilesPath, "log.datacollector.*.txt").Single());
      Assert.Contains("[coverlet]Initializing CoverletCoverageDataCollector with configuration:", dataCollectorLogContent);
      Assert.Contains("[coverlet]Initialize CoverletInProcDataCollector", File.ReadAllText(Directory.GetFiles(testLogFilesPath, "log.host.*.txt").Single()));
      Assert.Contains("[coverlet]Mapping resolved", dataCollectorLogContent);

      CleanupBuildOutput();
    }

    [Fact]
    public void Collectors_SourceLink()
    {
      string testResultPath = Path.Join(_testResultsPath, $"{TestContext.Current.TestClass?.TestClassName}.{TestContext.Current.TestMethod?.MethodName}");
      string testLogFilesPath = Path.Join(_testResultsPath, $"{TestContext.Current.TestClass?.TestClassName}.{TestContext.Current.TestMethod?.MethodName}", "log");
      string logFilename = $"{TestContext.Current.TestClass?.TestClassName}.{TestContext.Current.TestMethod?.MethodName}.binlog";

      CreateDeterministicTestPropsFile();
      DeleteLogFiles(testLogFilesPath);
      DeleteCoverageFiles(testResultPath);

      DotnetCli($"build -c {_buildConfiguration} -f {_buildTargetFramework} -bl:build.{logFilename} --verbosity normal /p:DeterministicSourcePaths=true", out string buildOutput, out string buildError, _testProjectPath);
      if (!string.IsNullOrEmpty(buildError))
      {
        _output.WriteLine(buildError);
      }
      else
      {
        _output.WriteLine(buildOutput);
      }
      Assert.Contains("Build succeeded.", buildOutput);
      string sourceRootMappingFilePath = Path.Combine(_testBinaryPath, _artifactsPivot.ToLowerInvariant(), "CoverletSourceRootsMapping_coverletsample.integration.determisticbuild");

      Assert.True(File.Exists(sourceRootMappingFilePath), $"File not found: {sourceRootMappingFilePath}");
      Assert.NotEmpty(File.ReadAllText(sourceRootMappingFilePath));
      Assert.Contains("=/_/", File.ReadAllText(sourceRootMappingFilePath));

      string runSettingsPath = AddCollectorRunsettingsFile(_testProjectPath, "[coverletsample.integration.determisticbuild]*DeepThought", sourceLink: true);
      string cmdArgument = $"test -c {_buildConfiguration} -f {_buildTargetFramework} --no-build --collect:\"XPlat Code Coverage\" --results-directory:\"{testResultPath}\" --settings \"{runSettingsPath}\" --diag:{Path.Combine(testLogFilesPath, "log.txt")}";
      _output.WriteLine($"Command: dotnet {cmdArgument}");
      int result = DotnetCli(cmdArgument, out string standardOutput, out string standardError, _testProjectPath);
      if (!string.IsNullOrEmpty(standardError))
      {
        _output.WriteLine(standardError);
      }
      else
      {
        _output.WriteLine(standardOutput);
      }
      Assert.Equal(0, result);
      Assert.Contains("Passed!", standardOutput);
      AssertCoverage(standardOutput, "coverage.json", checkDeterministicReport: false);

      // delete irrelevant generated files
      DeleteTestIntermediateFiles(testResultPath);

      string[] fileList = Directory.GetFiles(testResultPath, "coverage.cobertura.xml", SearchOption.AllDirectories);
      if (fileList.Length > 1)
      {
        _output.WriteLine("multiple coverage.cobertura.xml exist: ");
        foreach (string file in fileList)
        {
          _output.WriteLine(file);
        }
      }

      Assert.Single(fileList);
      Assert.Contains("raw.githubusercontent.com", File.ReadAllText(fileList[0]));

      // Check out/in process collectors injection
      string dataCollectorLogContent = File.ReadAllText(Directory.GetFiles(testLogFilesPath, "log.datacollector.*.txt").Single());
      Assert.Contains("[coverlet]Initializing CoverletCoverageDataCollector with configuration:", dataCollectorLogContent);
      Assert.Contains("[coverlet]Initialize CoverletInProcDataCollector", File.ReadAllText(Directory.GetFiles(testLogFilesPath, "log.host.*.txt").Single()));
      Assert.Contains("[coverlet]Mapping resolved", dataCollectorLogContent);

      CleanupBuildOutput();
    }

    private static void DeleteTestIntermediateFiles(string testResultsPath)
    {
      if (Directory.Exists(testResultsPath))
      {
        DirectoryInfo hdDirectory = new DirectoryInfo(testResultsPath);

        // search for directory "In" which has second copy e.g. '_fv-az365-374_2023-10-10_14_26_42\In\fv-az365-374\coverage.json'
        DirectoryInfo[] intermediateFolder = hdDirectory.GetDirectories("In", SearchOption.AllDirectories);
        foreach (DirectoryInfo foundDir in intermediateFolder)
        {
          DirectoryInfo? parentDir = Directory.GetParent(foundDir.FullName);
          Directory.Delete(parentDir!.FullName, true);
        }
      }
    }

    private static void DeleteLogFiles(string directory)
    {
      if (Directory.Exists(directory))
      {
        DirectoryInfo hdDirectory = new DirectoryInfo(directory);
        FileInfo[] filesInDir = hdDirectory.GetFiles("log.*.txt");

        foreach (FileInfo foundFile in filesInDir)
        {
          string fullName = foundFile.FullName;
          File.Delete(fullName);
        }
      }

    }
    private void CleanupBuildOutput()
    {
      if (Directory.Exists(_testBinaryPath))
      {
        Directory.Delete(_testBinaryPath, recursive: true);
      }

      string intermediateBuildOutput = _testBinaryPath.Replace("bin", "obj");
      if (Directory.Exists(intermediateBuildOutput))
      {
        Directory.Delete(intermediateBuildOutput, recursive: true);
      }
    }
    private static void DeleteCoverageFiles(string directory)
    {
      if (Directory.Exists(directory))
      {
        DirectoryInfo hdDirectory = new DirectoryInfo(directory);
        FileInfo[] filesInDir = hdDirectory.GetFiles("coverage.cobertura.xml");

        foreach (FileInfo foundFile in filesInDir)
        {
          string fullName = foundFile.FullName;
          File.Delete(fullName);
        }

        DirectoryInfo[] dirsInDir = hdDirectory.GetDirectories();

        foreach (DirectoryInfo foundDir in dirsInDir)
        {
          foreach (FileInfo foundFile in foundDir.GetFiles("coverage.cobertura.xml"))
          {
            string fullName = foundFile.FullName;
            File.Delete(fullName);
          }
        }
      }
    }

    private string GetReportPath(string standardOutput, string reportFileName = "")
    {
      string reportPath = "";
      if (standardOutput.Contains(reportFileName))
      {
        reportPath = standardOutput.Split('\n').FirstOrDefault(line => line.Contains(reportFileName))!.TrimStart();
        reportPath = reportPath[reportPath.IndexOf(Directory.GetDirectoryRoot(_testProjectPath))..];
        reportPath = reportPath[..reportPath.IndexOf(reportFileName)];
      }
      return reportPath;
    }
    public void Dispose()
    {
      File.Delete(Path.Combine(_testProjectPath, PropsFileName));
    }
  }
}
