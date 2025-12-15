// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Coverlet.MTP.CommandLine;
using Microsoft.Testing.Platform.CommandLine;

namespace Coverlet.MTP.Configuration;

internal sealed class CoverageConfiguration
{
  private readonly ICommandLineOptions _commandLineOptions;

  public CoverageConfiguration(ICommandLineOptions commandLineOptions)
  {
    _commandLineOptions = commandLineOptions;
  }

  public bool IsCoverageEnabled =>
    _commandLineOptions.IsOptionSet(CoverletCommandLineOptionsProvider.CoverageOptionName);

  public string[] GetOutputFormats()
  {
    if (_commandLineOptions.TryGetOptionArgumentList(
      CoverletCommandLineOptionsProvider.CoverageOutputFormatOptionName,
      out string[]? formats))
    {
      return formats[0].Split(',')
        .Select(f => f.Trim())
        .ToArray();
    }

    return new[] { "json" }; // Default format
  }

  public string GetOutputPath()
  {
    if (_commandLineOptions.TryGetOptionArgumentList(
      CoverletCommandLineOptionsProvider.CoverageOutputOptionName,
      out string[]? outputPath))
    {
      return outputPath[0];
    }

    // Default: TestResults folder next to test assembly
    string testDir = Path.GetDirectoryName(GetTestAssemblyPath()) ?? AppContext.BaseDirectory;
    return Path.Combine(testDir, "TestResults");
  }

  public string[] GetIncludeFilters()
  {
    if (_commandLineOptions.TryGetOptionArgumentList(
      CoverletCommandLineOptionsProvider.CoverageIncludeOptionName,
      out string[]? filters))
    {
      return filters;
    }

    return Array.Empty<string>();
  }

  public string[] GetExcludeFilters()
  {
    if (_commandLineOptions.TryGetOptionArgumentList(
      CoverletCommandLineOptionsProvider.CoverageExcludeOptionName,
      out string[]? filters))
    {
      return filters;
    }

    return Array.Empty<string>();
  }

  public string[] GetExcludeByFileFilters()
  {
    if (_commandLineOptions.TryGetOptionArgumentList(
      CoverletCommandLineOptionsProvider.CoverageExcludeByFileOptionName,
      out string[]? filters))
    {
      return filters;
    }

    return Array.Empty<string>();
  }

  public string[] GetExcludeByAttributeFilters()
  {
    if (_commandLineOptions.TryGetOptionArgumentList(
      CoverletCommandLineOptionsProvider.CoverageExcludeByAttributeOptionName,
      out string[]? filters))
    {
      return filters;
    }

    return Array.Empty<string>();
  }

  public string[] GetIncludeDirectories()
  {
    if (_commandLineOptions.TryGetOptionArgumentList(
      CoverletCommandLineOptionsProvider.CoverageIncludeDirectoryOptionName,
      out string[]? directories))
    {
      return directories;
    }

    return Array.Empty<string>();
  }

  public bool UseSingleHit =>
    _commandLineOptions.IsOptionSet(CoverletCommandLineOptionsProvider.CoverageSingleHitOptionName);

  public bool IncludeTestAssembly =>
    _commandLineOptions.IsOptionSet(CoverletCommandLineOptionsProvider.CoverageIncludeTestAssemblyOptionName);

  public bool SkipAutoProps =>
    _commandLineOptions.IsOptionSet(CoverletCommandLineOptionsProvider.CoverageSkipAutoPropsOptionName);

  public string[] GetDoesNotReturnAttributes()
  {
    if (_commandLineOptions.TryGetOptionArgumentList(
      CoverletCommandLineOptionsProvider.CoverageDoesNotReturnAttributeOptionName,
      out string[]? attributes))
    {
      return attributes;
    }

    return Array.Empty<string>();
  }

  public bool ExcludeAssembliesWithoutSources =>
    _commandLineOptions.IsOptionSet(CoverletCommandLineOptionsProvider.CoverageExcludeAssembliesWithoutSourcesOptionName);

  /// <summary>
  /// Gets the test assembly path using multiple fallback strategies.
  /// </summary>
  public static string GetTestAssemblyPath()
  {
    // Try multiple methods to get the test assembly path
    // 1. Entry assembly (most reliable for test scenarios)
    string? path = System.Reflection.Assembly.GetEntryAssembly()?.Location;
    if (!string.IsNullOrEmpty(path) && File.Exists(path))
    {
      return path;
    }

    // 2. Current process main module
    path = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
    if (!string.IsNullOrEmpty(path) && File.Exists(path))
    {
      return path;
    }

    // 3. Base directory + first command line argument
    string[] args = Environment.GetCommandLineArgs();
    if (args.Length > 0)
    {
      string fullPath = Path.GetFullPath(args[0]);
      if (File.Exists(fullPath))
      {
        return fullPath;
      }
    }

    // 4. Fallback to base directory
    return AppContext.BaseDirectory;
  }
}
