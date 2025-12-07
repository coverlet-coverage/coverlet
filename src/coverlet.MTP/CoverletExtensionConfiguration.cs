// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// see details here: https://learn.microsoft.com/en-us/dotnet/core/testing/microsoft-testing-platform-architecture-extensions#the-itestsessionlifetimehandler-extensions
// Coverlet instrumentation should be done before any test is executed, and the coverage data should be collected after all tests have run.
// Coverlet collects code coverage data and does not need to be aware of the test framework being used. It also does not need test case details or test results.

//using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Testing.Platform.Services;

namespace coverlet.Extension
{
  internal class CoverletExtensionConfiguration
  {
    public string[] IncludePatterns { get; set; } = Array.Empty<string>();
    public string[] ExcludePatterns { get; set; } = Array.Empty<string>();
    public bool IncludeTestAssembly { get; set; }
    public string OutputDirectory { get; set; } = string.Empty;
    public string sourceMappingFile { get; set; } = string.Empty;
    public bool EnableSourceMapping { get; set; }
    public string[] formats { get; set; } = ["json"];

    //public const string PipeName = "TESTINGPLATFORM_COVERLET_PIPENAME";
    //public const string MutexName = "TESTINGPLATFORM_COVERLET_MUTEXNAME";
    //public const string MutexNameSuffix = "TESTINGPLATFORM_COVERLET_MUTEXNAME_SUFFIX";

    //public CoverletExtensionConfiguration(ITestApplicationModuleInfo testApplicationModuleInfo, PipeNameDescription pipeNameDescription, string mutexSuffix)
    //{
    //  PipeNameValue = pipeNameDescription.Name;
    //  PipeNameKey = $"{PipeName}_{FNV_1aHashHelper.ComputeStringHash(testApplicationModuleInfo.GetCurrentTestApplicationFullPath())}_{mutexSuffix}";
    //  MutexSuffix = mutexSuffix;
    //}
    //public string PipeNameKey { get; } = PipeName;

    //public string PipeNameValue { get; }
    //public string MutexSuffix { get; }
    public bool Enable { get; set; } = true;
  }
  public interface ICommandLineOptions
  {
    bool IsOptionSet(string optionName);

    bool TryGetOptionArgumentList(
        string optionName,
        out string[]? arguments);
  }
  internal class GetCommandLineValues
  {
    private readonly IServiceProvider _serviceProvider;
    private readonly ICommandLineOptions _commandLineOptions;

    public GetCommandLineValues(IServiceProvider serviceProvider, ICommandLineOptions commandLineOptions)
    {
      _serviceProvider = serviceProvider;
      _commandLineOptions = commandLineOptions;
    }

    public void InitializeFromCommandLineArgs()
    {
      IServiceCollection serviceCollection = new ServiceCollection();
      ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();
      ICommandLineOptions commandLineOptions = (ICommandLineOptions)_serviceProvider.GetCommandLineOptions();
      CoverletExtensionConfiguration configuration = new CoverletExtensionConfiguration();

      if (commandLineOptions.IsOptionSet("include"))
      {
        if (commandLineOptions.TryGetOptionArgumentList("include", out string[]? includeArgs))
        {
          configuration.IncludePatterns = includeArgs ?? Array.Empty<string>();
        }
        else
        {
          configuration.IncludePatterns = Array.Empty<string>();
        }
      }

      if (commandLineOptions.IsOptionSet("exclude"))
      {
        if (commandLineOptions.TryGetOptionArgumentList("exclude", out string[]? excludeArgs))
        {
          configuration.ExcludePatterns = excludeArgs ?? Array.Empty<string>();
        }
        else
        {
          configuration.ExcludePatterns = Array.Empty<string>();
        }
      }

      if (commandLineOptions.IsOptionSet("output-directory"))
      {
        if (commandLineOptions.TryGetOptionArgumentList("output-directory", out string[]? outputDirectoryArgs))
        {
          configuration.sourceMappingFile = outputDirectoryArgs!.Length > 0 ? outputDirectoryArgs[0] : string.Empty;
        }
        else
        {
          configuration.OutputDirectory = string.Empty;
        }
      }

      if (commandLineOptions.IsOptionSet("source-mapping-file"))
      {
        if (commandLineOptions.TryGetOptionArgumentList("source-mapping-file", out string[]? sourceMappingFileArgs))
        {
          configuration.sourceMappingFile = sourceMappingFileArgs!.Length > 0 ? sourceMappingFileArgs[0] : string.Empty;
        }
        else
        {
          configuration.sourceMappingFile = string.Empty;
        }
      }

      if (commandLineOptions.IsOptionSet("include-test-assembly"))
      {
        configuration.IncludeTestAssembly = true;
      }
    }
  }
}
