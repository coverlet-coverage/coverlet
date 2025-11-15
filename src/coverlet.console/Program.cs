// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConsoleTables;
using Coverlet.Console.Logging;
using Coverlet.Core;
using Coverlet.Core.Abstractions;
using Coverlet.Core.Enums;
using Coverlet.Core.Helpers;
using Coverlet.Core.Reporters;
using Coverlet.Core.Symbols;
using Microsoft.Extensions.DependencyInjection;

namespace Coverlet.Console
{
  public static class Program
  {
    static int s_exitCode;
    static async Task<int> Main(string[] args)
    {
      Argument<string> moduleOrAppDirectory = new("path") { Description = "Path to the test assembly or application directory." };
      Option<string> target = new("--target", "-t") { Description = "Path to the test runner application.", Arity = ArgumentArity.ZeroOrOne, Required = true };
      Option<string> targs = new("--targetargs", "-a") { Description = "Arguments to be passed to the test runner.", Arity = ArgumentArity.ZeroOrOne };
      Option<string> output = new("--output", "-o") { Description = "Output of the generated coverage report", Arity = ArgumentArity.ZeroOrOne };
      Option<LogLevel> verbosity = new("--verbosity", "-v") { DefaultValueFactory = (_) => LogLevel.Normal, Description = "Sets the verbosity level of the command. Allowed values are quiet, minimal, normal, detailed.", Arity = ArgumentArity.ZeroOrOne };
      Option<string[]> formats = new("--format", "-f") { DefaultValueFactory = (_) => new[] { "json" }, Description = "Format of the generated coverage report.", Arity = ArgumentArity.ZeroOrMore, AllowMultipleArgumentsPerToken = true };
      formats.AcceptOnlyFromAmong("json", "lcov", "opencover", "cobertura", "teamcity");
      Option<string> threshold = new("--threshold") { Description = "Exits with error if the coverage % is below value.", Arity = ArgumentArity.ZeroOrOne };
      Option<List<string>> thresholdTypes = new("--threshold-type") { DefaultValueFactory = (_) => ["line", "branch", "method"], Description = "Coverage type to apply the threshold to." };
      thresholdTypes.AcceptOnlyFromAmong("line", "branch", "method");
      Option<ThresholdStatistic> thresholdStat = new("--threshold-stat") { DefaultValueFactory = (_) => ThresholdStatistic.Minimum, Description = "Coverage statistic used to enforce the threshold value.", Arity = ArgumentArity.ZeroOrOne };
      Option<string[]> excludeFilters = new("--exclude") { Description = "Filter expressions to exclude specific modules and types.", Arity = ArgumentArity.ZeroOrMore, AllowMultipleArgumentsPerToken = true };
      Option<string[]> includeFilters = new("--include") { Description = "Filter expressions to include only specific modules and types.", Arity = ArgumentArity.ZeroOrMore, AllowMultipleArgumentsPerToken = true };
      Option<string[]> excludedSourceFiles = new("--exclude-by-file") { Description = "Glob patterns specifying source files to exclude.", Arity = ArgumentArity.ZeroOrMore, AllowMultipleArgumentsPerToken = true };
      Option<string[]> includeDirectories = new("--include-directory") { Description = "Include directories containing additional assemblies to be instrumented.", Arity = ArgumentArity.ZeroOrMore, AllowMultipleArgumentsPerToken = true };
      Option<string[]> excludeAttributes = new("--exclude-by-attribute") { Description = "Attributes to exclude from code coverage.", Arity = ArgumentArity.ZeroOrMore, AllowMultipleArgumentsPerToken = true };
      Option<bool> includeTestAssembly = new("--include-test-assembly") { Description = "Specifies whether to report code coverage of the test assembly.", Arity = ArgumentArity.Zero };
      Option<bool> singleHit = new("--single-hit") { Description = "Specifies whether to limit code coverage hit reporting to a single hit for each location", Arity = ArgumentArity.Zero };
      Option<bool> skipAutoProp = new("--skipautoprops") { Description = "Neither track nor record auto-implemented properties.", Arity = ArgumentArity.Zero };
      Option<string> mergeWith = new("--merge-with") { Description = "Path to existing coverage result to merge.", Arity = ArgumentArity.ZeroOrOne };
      Option<bool> useSourceLink = new("--use-source-link") { Description = "Specifies whether to use SourceLink URIs in place of file system paths.", Arity = ArgumentArity.Zero };
      Option<string[]> doesNotReturnAttributes = new("--does-not-return-attribute") { Description = "Attributes that mark methods that do not return", Arity = ArgumentArity.ZeroOrMore, AllowMultipleArgumentsPerToken = true };
      Option<string> excludeAssembliesWithoutSources = new("--exclude-assemblies-without-sources") { DefaultValueFactory = (_) => "MissingAll", Description = "Specifies behavior of heuristic to ignore assemblies with missing source documents." };
      excludeAssembliesWithoutSources.AcceptOnlyFromAmong("MissingAll", "MissingAny", "None");
      Option<string> sourceMappingFile = new("--source-mapping-file") { Description = "Specifies the path to a SourceRootsMappings file.", Arity = ArgumentArity.ZeroOrOne };

      RootCommand rootCommand = new("Cross platform .NET Core code coverage tool");
      rootCommand.Arguments.Add(moduleOrAppDirectory);
      rootCommand.Options.Add(target);
      rootCommand.Options.Add(targs);
      rootCommand.Options.Add(output);
      rootCommand.Options.Add(verbosity);
      rootCommand.Options.Add(formats);
      rootCommand.Options.Add(threshold);
      rootCommand.Options.Add(thresholdTypes);
      rootCommand.Options.Add(thresholdStat);
      rootCommand.Options.Add(excludeFilters);
      rootCommand.Options.Add(includeFilters);
      rootCommand.Options.Add(excludedSourceFiles);
      rootCommand.Options.Add(includeDirectories);
      rootCommand.Options.Add(excludeAttributes);
      rootCommand.Options.Add(includeTestAssembly);
      rootCommand.Options.Add(singleHit);
      rootCommand.Options.Add(skipAutoProp);
      rootCommand.Options.Add(mergeWith);
      rootCommand.Options.Add(useSourceLink);
      rootCommand.Options.Add(doesNotReturnAttributes);
      rootCommand.Options.Add(excludeAssembliesWithoutSources);
      rootCommand.Options.Add(sourceMappingFile);

      rootCommand.SetAction(async (parseResult) =>
      {
        string moduleOrAppDirectoryValue = parseResult.GetValue(moduleOrAppDirectory);
        string targetValue = parseResult.GetValue(target);
        string targsValue = parseResult.GetValue(targs);
        string outputValue = parseResult.GetValue(output);
        LogLevel verbosityValue = parseResult.GetValue(verbosity);
        string[] formatsValue = parseResult.GetValue(formats);
        string thresholdValue = parseResult.GetValue(threshold);
        List<string> thresholdTypesValue = parseResult.GetValue(thresholdTypes);
        ThresholdStatistic thresholdStatValue = parseResult.GetValue(thresholdStat);
        string[] excludeFiltersValue = parseResult.GetValue(excludeFilters);
        string[] includeFiltersValue = parseResult.GetValue(includeFilters);
        string[] excludedSourceFilesValue = parseResult.GetValue(excludedSourceFiles);
        string[] includeDirectoriesValue = parseResult.GetValue(includeDirectories);
        string[] excludeAttributesValue = parseResult.GetValue(excludeAttributes);
        bool includeTestAssemblyValue = parseResult.GetValue(includeTestAssembly);
        bool singleHitValue = parseResult.GetValue(singleHit);
        bool skipAutoPropValue = parseResult.GetValue(skipAutoProp);
        string mergeWithValue = parseResult.GetValue(mergeWith);
        bool useSourceLinkValue = parseResult.GetValue(useSourceLink);
        string[] doesNotReturnAttributesValue = parseResult.GetValue(doesNotReturnAttributes);
        string excludeAssembliesWithoutSourcesValue = parseResult.GetValue(excludeAssembliesWithoutSources);
        string sourceMappingFileValue = parseResult.GetValue(sourceMappingFile);

        if (string.IsNullOrEmpty(moduleOrAppDirectoryValue) || string.IsNullOrWhiteSpace(moduleOrAppDirectoryValue))
          throw new ArgumentException("No test assembly or application directory specified.");

        int taskStatus = await HandleCommand(moduleOrAppDirectoryValue,
                      targetValue,
                      targsValue,
                      outputValue,
                      verbosityValue,
                      formatsValue,
                      thresholdValue,
                      thresholdTypesValue,
                      thresholdStatValue,
                      excludeFiltersValue,
                      includeFiltersValue,
                      excludedSourceFilesValue,
                      includeDirectoriesValue,
                      excludeAttributesValue,
                      includeTestAssemblyValue,
                      singleHitValue,
                      skipAutoPropValue,
                      mergeWithValue,
                      useSourceLinkValue,
                      doesNotReturnAttributesValue,
                      excludeAssembliesWithoutSourcesValue,
                      sourceMappingFileValue);

        s_exitCode = taskStatus;
        return taskStatus;
      });

      ParseResult parseResult = rootCommand.Parse(args);
      await parseResult.InvokeAsync();
      return s_exitCode;
    }
    private static Task<int> HandleCommand(string moduleOrAppDirectory,
                                                           string target,
                                                           string targs,
                                                           string output,
                                                           LogLevel verbosity,
                                                           string[] formats,
                                                           string threshold,
                                                           List<string> thresholdTypes,
                                                           ThresholdStatistic thresholdStat,
                                                           string[] excludeFilters,
                                                           string[] includeFilters,
                                                           string[] excludedSourceFiles,
                                                           string[] includeDirectories,
                                                           string[] excludeAttributes,
                                                           bool includeTestAssembly,
                                                           bool singleHit,
                                                           bool skipAutoProp,
                                                           string mergeWith,
                                                           bool useSourceLink,
                                                           string[] doesNotReturnAttributes,
                                                           string excludeAssembliesWithoutSources,
                                                           string sourceMappingFile
             )
    {

      IServiceCollection serviceCollection = new ServiceCollection();
      serviceCollection.AddTransient<IRetryHelper, RetryHelper>();
      serviceCollection.AddTransient<IProcessExitHandler, ProcessExitHandler>();
      serviceCollection.AddTransient<IFileSystem, FileSystem>();
      serviceCollection.AddTransient<ILogger, ConsoleLogger>();
      // We need to keep singleton/static semantics
      serviceCollection.AddSingleton<IInstrumentationHelper, InstrumentationHelper>();
      serviceCollection.AddSingleton<ISourceRootTranslator, SourceRootTranslator>(provider => new SourceRootTranslator(sourceMappingFile, provider.GetRequiredService<ILogger>(), provider.GetRequiredService<IFileSystem>()));
      serviceCollection.AddSingleton<ICecilSymbolHelper, CecilSymbolHelper>();

      ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();

      var logger = (ConsoleLogger)serviceProvider.GetService<ILogger>();
      IFileSystem fileSystem = serviceProvider.GetService<IFileSystem>();

      // Adjust log level based on user input.
      logger.Level = verbosity;
      s_exitCode = (int)CommandExitCodes.Success;

      try
      {
        CoverageParameters parameters = new()
        {
          IncludeFilters = includeFilters,
          IncludeDirectories = includeDirectories,
          ExcludeFilters = excludeFilters,
          ExcludedSourceFiles = excludedSourceFiles,
          ExcludeAttributes = excludeAttributes,
          IncludeTestAssembly = includeTestAssembly,
          SingleHit = singleHit,
          MergeWith = mergeWith,
          UseSourceLink = useSourceLink,
          SkipAutoProps = skipAutoProp,
          DoesNotReturnAttributes = doesNotReturnAttributes,
          ExcludeAssembliesWithoutSources = excludeAssembliesWithoutSources
        };
        ISourceRootTranslator sourceRootTranslator = serviceProvider.GetRequiredService<ISourceRootTranslator>();

        Coverage coverage = new(moduleOrAppDirectory,
                                         parameters,
                                         logger,
                                         serviceProvider.GetRequiredService<IInstrumentationHelper>(),
                                         fileSystem,
                                         sourceRootTranslator,
                                         serviceProvider.GetRequiredService<ICecilSymbolHelper>());
        coverage.PrepareModules();

        Process process = new();
        process.StartInfo.FileName = target;
        process.StartInfo.Arguments = targs ?? string.Empty;
        process.StartInfo.CreateNoWindow = true;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.OutputDataReceived += (sender, eventArgs) =>
        {
          if (!string.IsNullOrEmpty(eventArgs.Data))
            logger.LogInformation(eventArgs.Data, important: true);
        };

        process.ErrorDataReceived += (sender, eventArgs) =>
        {
          if (!string.IsNullOrEmpty(eventArgs.Data))
            logger.LogError(eventArgs.Data);
        };

        process.Start();

        process.BeginErrorReadLine();
        process.BeginOutputReadLine();

        process.WaitForExit();

        string dOutput = output != null ? output : Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar.ToString();

        logger.LogInformation("\nCalculating coverage result...");

        CoverageResult result = coverage.GetCoverageResult();

        string directory = Path.GetDirectoryName(dOutput);
        if (directory == string.Empty)
        {
          directory = Directory.GetCurrentDirectory();
        }
        else if (!Directory.Exists(directory))
        {
          Directory.CreateDirectory(directory);
        }

        foreach (string format in formats)
        {
          IReporter reporter = new ReporterFactory(format).CreateReporter();
          if (reporter == null)
          {
            throw new InvalidOperationException($"Specified output format '{format}' is not supported");
          }

          if (reporter.OutputType == ReporterOutputType.Console)
          {
            // Output to console
            logger.LogInformation("  Outputting results to console", important: true);
            logger.LogInformation(reporter.Report(result, sourceRootTranslator), important: true);
          }
          else
          {
            // Output to file
            string filename = Path.GetFileName(dOutput);
            filename = (filename == string.Empty) ? $"coverage.{reporter.Extension}" : filename;
            filename = Path.HasExtension(filename) ? filename : $"{filename}.{reporter.Extension}";

            string report = Path.Combine(directory, filename);
            logger.LogInformation($"  Generating report '{report}'", important: true);
            fileSystem.WriteAllText(report, reporter.Report(result, sourceRootTranslator));
          }
        }

        var thresholdTypeFlagQueue = new Queue<ThresholdTypeFlags>();

        foreach (string thresholdType in thresholdTypes)
        {
          if (thresholdType.Equals("line", StringComparison.OrdinalIgnoreCase))
          {
            thresholdTypeFlagQueue.Enqueue(ThresholdTypeFlags.Line);
          }
          else if (thresholdType.Equals("branch", StringComparison.OrdinalIgnoreCase))
          {
            thresholdTypeFlagQueue.Enqueue(ThresholdTypeFlags.Branch);
          }
          else if (thresholdType.Equals("method", StringComparison.OrdinalIgnoreCase))
          {
            thresholdTypeFlagQueue.Enqueue(ThresholdTypeFlags.Method);
          }
        }

        var thresholdTypeFlagValues = new Dictionary<ThresholdTypeFlags, double>();
        if (!string.IsNullOrEmpty(threshold) && threshold.Contains(','))
        {
          IEnumerable<string> thresholdValues = threshold.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim());
          if (thresholdValues.Count() != thresholdTypeFlagQueue.Count)
          {
            throw new InvalidOperationException($"Threshold type flag count ({thresholdTypeFlagQueue.Count}) and values count ({thresholdValues.Count()}) doesn't match");
          }

          foreach (string thresholdValue in thresholdValues)
          {
            if (double.TryParse(thresholdValue, out double value))
            {
              thresholdTypeFlagValues[thresholdTypeFlagQueue.Dequeue()] = value;
            }
            else
            {
              throw new InvalidOperationException($"Invalid threshold value must be numeric");
            }
          }
        }
        else
        {
          double thresholdValue = threshold != null ? double.Parse(threshold) : 0;

          while (thresholdTypeFlagQueue.Any())
          {
            thresholdTypeFlagValues[thresholdTypeFlagQueue.Dequeue()] = thresholdValue;
          }
        }

        var coverageTable = new ConsoleTable("Module", "Line", "Branch", "Method");

        CoverageDetails linePercentCalculation = CoverageSummary.CalculateLineCoverage(result.Modules);
        CoverageDetails branchPercentCalculation = CoverageSummary.CalculateBranchCoverage(result.Modules);
        CoverageDetails methodPercentCalculation = CoverageSummary.CalculateMethodCoverage(result.Modules);

        double totalLinePercent = linePercentCalculation.Percent;
        double totalBranchPercent = branchPercentCalculation.Percent;
        double totalMethodPercent = methodPercentCalculation.Percent;

        double averageLinePercent = linePercentCalculation.AverageModulePercent;
        double averageBranchPercent = branchPercentCalculation.AverageModulePercent;
        double averageMethodPercent = methodPercentCalculation.AverageModulePercent;

        foreach (KeyValuePair<string, Documents> _module in result.Modules)
        {
          double linePercent = CoverageSummary.CalculateLineCoverage(_module.Value).Percent;
          double branchPercent = CoverageSummary.CalculateBranchCoverage(_module.Value).Percent;
          double methodPercent = CoverageSummary.CalculateMethodCoverage(_module.Value).Percent;

          coverageTable.AddRow(Path.GetFileNameWithoutExtension(_module.Key), $"{InvariantFormat(linePercent)}%", $"{InvariantFormat(branchPercent)}%", $"{InvariantFormat(methodPercent)}%");
        }

        logger.LogInformation(coverageTable.ToStringAlternative());

        coverageTable.Columns.Clear();
        coverageTable.Rows.Clear();

        coverageTable.AddColumn(new[] { "", "Line", "Branch", "Method" });
        coverageTable.AddRow("Total", $"{InvariantFormat(totalLinePercent)}%", $"{InvariantFormat(totalBranchPercent)}%", $"{InvariantFormat(totalMethodPercent)}%");
        coverageTable.AddRow("Average", $"{InvariantFormat(averageLinePercent)}%", $"{InvariantFormat(averageBranchPercent)}%", $"{InvariantFormat(averageMethodPercent)}%");

        logger.LogInformation(coverageTable.ToStringAlternative());
        if (process.ExitCode > 0)
        {
          s_exitCode = (int)CommandExitCodes.TestFailed;
        }

        ThresholdTypeFlags thresholdTypeFlags = result.GetThresholdTypesBelowThreshold(thresholdTypeFlagValues, thresholdStat);
        if (thresholdTypeFlags != ThresholdTypeFlags.None)
        {
          s_exitCode = (int)CommandExitCodes.CoverageBelowThreshold;
          var errorMessageBuilder = new StringBuilder();
          if ((thresholdTypeFlags & ThresholdTypeFlags.Line) != ThresholdTypeFlags.None)
          {
            errorMessageBuilder.AppendLine($"The {thresholdStat.ToString().ToLower()} line coverage is below the specified {thresholdTypeFlagValues[ThresholdTypeFlags.Line]}");
          }

          if ((thresholdTypeFlags & ThresholdTypeFlags.Branch) != ThresholdTypeFlags.None)
          {
            errorMessageBuilder.AppendLine($"The {thresholdStat.ToString().ToLower()} branch coverage is below the specified {thresholdTypeFlagValues[ThresholdTypeFlags.Branch]}");
          }

          if ((thresholdTypeFlags & ThresholdTypeFlags.Method) != ThresholdTypeFlags.None)
          {
            errorMessageBuilder.AppendLine($"The {thresholdStat.ToString().ToLower()} method coverage is below the specified {thresholdTypeFlagValues[ThresholdTypeFlags.Method]}");
          }
          logger.LogError(errorMessageBuilder.ToString());
        }

        return Task.FromResult(s_exitCode);

      }

      catch (Win32Exception we) when (we.Source == "System.Diagnostics.Process")
      {
        logger.LogError($"Start process '{target}' failed with '{we.Message}'");
        return Task.FromResult(s_exitCode > 0 ? s_exitCode : (int)CommandExitCodes.Exception);
      }
      catch (Exception ex)
      {
        logger.LogError(ex.Message);
        return Task.FromResult(s_exitCode > 0 ? s_exitCode : (int)CommandExitCodes.Exception);
      }

    }

    static string InvariantFormat(double value) => value.ToString(CultureInfo.InvariantCulture);
  }
}
