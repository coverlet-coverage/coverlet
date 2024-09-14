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
    static int Main(string[] args)
    {
      var moduleOrAppDirectory = new Argument<string>("path", "Path to the test assembly or application directory.");
      var target = new Option<string>(new[] { "--target", "-t" }, "Path to the test runner application.") { Arity = ArgumentArity.ZeroOrOne, IsRequired = true };
      var targs = new Option<string>(new[] { "--targetargs", "-a" }, "Arguments to be passed to the test runner.") { Arity = ArgumentArity.ZeroOrOne };
      var output = new Option<string>(new[] { "--output", "-o" }, "Output of the generated coverage report") { Arity = ArgumentArity.ZeroOrOne };
      var verbosity = new Option<LogLevel>(new[] { "--verbosity", "-v" }, () => LogLevel.Normal, "Sets the verbosity level of the command. Allowed values are quiet, minimal, normal, detailed.") { Arity = ArgumentArity.ZeroOrOne };
      var formats = new Option<string[]>(new[] { "--format", "-f" }, () => new[] { "json" }, "Format of the generated coverage report.") { Arity = ArgumentArity.ZeroOrMore, AllowMultipleArgumentsPerToken = true };
      var threshold = new Option<string>("--threshold", "Exits with error if the coverage % is below value.") { Arity = ArgumentArity.ZeroOrOne };
      var thresholdTypes = new Option<List<string>>("--threshold-type", () => new List<string>(new string[] { "line", "branch", "method" }), "Coverage type to apply the threshold to.").FromAmong("line", "branch", "method");
      var thresholdStat = new Option<ThresholdStatistic>("--threshold-stat", () => ThresholdStatistic.Minimum, "Coverage statistic used to enforce the threshold value.") { Arity = ArgumentArity.ZeroOrOne };
      var excludeFilters = new Option<string[]>("--exclude", "Filter expressions to exclude specific modules and types.") { Arity = ArgumentArity.ZeroOrMore, AllowMultipleArgumentsPerToken = true };
      var includeFilters = new Option<string[]>("--include", "Filter expressions to include only specific modules and types.") { Arity = ArgumentArity.ZeroOrMore, AllowMultipleArgumentsPerToken = true };
      var excludedSourceFiles = new Option<string[]>("--exclude-by-file", "Glob patterns specifying source files to exclude.") { Arity = ArgumentArity.ZeroOrMore, AllowMultipleArgumentsPerToken = true };
      var includeDirectories = new Option<string[]>("--include-directory", "Include directories containing additional assemblies to be instrumented.") { Arity = ArgumentArity.ZeroOrMore, AllowMultipleArgumentsPerToken = true };
      var excludeAttributes = new Option<string[]>("--exclude-by-attribute", "Attributes to exclude from code coverage.") { Arity = ArgumentArity.ZeroOrMore, AllowMultipleArgumentsPerToken = true };
      var includeTestAssembly = new Option<bool>("--include-test-assembly", "Specifies whether to report code coverage of the test assembly.") { Arity = ArgumentArity.Zero };
      var singleHit = new Option<bool>("--single-hit", "Specifies whether to limit code coverage hit reporting to a single hit for each location") { Arity = ArgumentArity.Zero };
      var skipAutoProp = new Option<bool>("--skipautoprops", "Neither track nor record auto-implemented properties.") { Arity = ArgumentArity.Zero };
      var mergeWith = new Option<string>("--merge-with", "Path to existing coverage result to merge.") { Arity = ArgumentArity.ZeroOrOne };
      var useSourceLink = new Option<bool>("--use-source-link", "Specifies whether to use SourceLink URIs in place of file system paths.") { Arity = ArgumentArity.Zero };
      var doesNotReturnAttributes = new Option<string[]>("--does-not-return-attribute", "Attributes that mark methods that do not return") { Arity = ArgumentArity.ZeroOrMore, AllowMultipleArgumentsPerToken = true };
      var excludeAssembliesWithoutSources = new Option<string>("--exclude-assemblies-without-sources", "Specifies behaviour of heuristic to ignore assemblies with missing source documents.") { Arity = ArgumentArity.ZeroOrOne };
      var sourceMappingFile = new Option<string>("--source-mapping-file", "Specifies the path to a SourceRootsMappings file.") { Arity = ArgumentArity.ZeroOrOne };
      var unloadCoverletFromModulesOnly = new Option<bool>("---only-unload-modules", "Specifies Whether or not coverlet will only unload after unit tests are finished and skip coverage calculation"){ Arity = ArgumentArity.ZeroOrOne };

      RootCommand rootCommand = new()
      {
        moduleOrAppDirectory,
        target,
        targs,
        output,
        verbosity,
        formats,
        threshold,
        thresholdTypes,
        thresholdStat,
        excludeFilters,
        includeFilters,
        excludedSourceFiles,
        includeDirectories,
        excludeAttributes,
        includeTestAssembly,
        singleHit,
        skipAutoProp,
        mergeWith,
        useSourceLink,
        doesNotReturnAttributes,
        excludeAssembliesWithoutSources,
        sourceMappingFile,
        unloadCoverletFromModulesOnly
      };

      rootCommand.Description = "Cross platform .NET Core code coverage tool";

      rootCommand.SetHandler(async (context) =>
      {
        string moduleOrAppDirectoryValue = context.ParseResult.GetValueForArgument(moduleOrAppDirectory);
        string targetValue = context.ParseResult.GetValueForOption(target);
        string targsValue = context.ParseResult.GetValueForOption(targs);
        string outputValue = context.ParseResult.GetValueForOption(output);
        LogLevel verbosityValue = context.ParseResult.GetValueForOption(verbosity);
        string[] formatsValue = context.ParseResult.GetValueForOption(formats);
        string thresholdValue = context.ParseResult.GetValueForOption(threshold);
        List<string> thresholdTypesValue = context.ParseResult.GetValueForOption(thresholdTypes);
        ThresholdStatistic thresholdStatValue = context.ParseResult.GetValueForOption(thresholdStat);
        string[] excludeFiltersValue = context.ParseResult.GetValueForOption(excludeFilters);
        string[] includeFiltersValue = context.ParseResult.GetValueForOption(includeFilters);
        string[] excludedSourceFilesValue = context.ParseResult.GetValueForOption(excludedSourceFiles);
        string[] includeDirectoriesValue = context.ParseResult.GetValueForOption(includeDirectories);
        string[] excludeAttributesValue = context.ParseResult.GetValueForOption(excludeAttributes);
        bool includeTestAssemblyValue = context.ParseResult.GetValueForOption(includeTestAssembly);
        bool singleHitValue = context.ParseResult.GetValueForOption(singleHit);
        bool skipAutoPropValue = context.ParseResult.GetValueForOption(skipAutoProp);
        string mergeWithValue = context.ParseResult.GetValueForOption(mergeWith);
        bool useSourceLinkValue = context.ParseResult.GetValueForOption(useSourceLink);
        string[] doesNotReturnAttributesValue = context.ParseResult.GetValueForOption(doesNotReturnAttributes);
        string excludeAssembliesWithoutSourcesValue = context.ParseResult.GetValueForOption(excludeAssembliesWithoutSources);
        string sourceMappingFileValue = context.ParseResult.GetValueForOption(sourceMappingFile);
        bool unloadCoverletFromModulesOnlyBool = context.ParseResult.GetValueForOption(unloadCoverletFromModulesOnly);

        if (string.IsNullOrEmpty(moduleOrAppDirectoryValue) || string.IsNullOrWhiteSpace(moduleOrAppDirectoryValue))
          throw new ArgumentException("No test assembly or application directory specified.");

        var taskStatus = await HandleCommand(moduleOrAppDirectoryValue,
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
                      sourceMappingFileValue,
                      unloadCoverletFromModulesOnlyBool);
        context.ExitCode = taskStatus;

      });
      return rootCommand.Invoke(args);
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
                                                           string sourceMappingFile,
                                                           bool unloadCoverletFromModulesOnly
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
      int exitCode = (int)CommandExitCodes.Success;

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

        if (unloadCoverletFromModulesOnly)
        {
          int unloadModuleExitCode = coverage.UnloadModules();
          return Task.FromResult(unloadModuleExitCode);
        }

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
            throw new Exception($"Specified output format '{format}' is not supported");
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
            throw new Exception($"Threshold type flag count ({thresholdTypeFlagQueue.Count}) and values count ({thresholdValues.Count()}) doesn't match");
          }

          foreach (string thresholdValue in thresholdValues)
          {
            if (double.TryParse(thresholdValue, out double value))
            {
              thresholdTypeFlagValues[thresholdTypeFlagQueue.Dequeue()] = value;
            }
            else
            {
              throw new Exception($"Invalid threshold value must be numeric");
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
        var summary = new CoverageSummary();

        CoverageDetails linePercentCalculation = summary.CalculateLineCoverage(result.Modules);
        CoverageDetails branchPercentCalculation = summary.CalculateBranchCoverage(result.Modules);
        CoverageDetails methodPercentCalculation = summary.CalculateMethodCoverage(result.Modules);

        double totalLinePercent = linePercentCalculation.Percent;
        double totalBranchPercent = branchPercentCalculation.Percent;
        double totalMethodPercent = methodPercentCalculation.Percent;

        double averageLinePercent = linePercentCalculation.AverageModulePercent;
        double averageBranchPercent = branchPercentCalculation.AverageModulePercent;
        double averageMethodPercent = methodPercentCalculation.AverageModulePercent;

        foreach (KeyValuePair<string, Documents> _module in result.Modules)
        {
          double linePercent = summary.CalculateLineCoverage(_module.Value).Percent;
          double branchPercent = summary.CalculateBranchCoverage(_module.Value).Percent;
          double methodPercent = summary.CalculateMethodCoverage(_module.Value).Percent;

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
          exitCode += (int)CommandExitCodes.TestFailed;
        }

        ThresholdTypeFlags thresholdTypeFlags = result.GetThresholdTypesBelowThreshold(summary, thresholdTypeFlagValues, thresholdStat);
        if (thresholdTypeFlags != ThresholdTypeFlags.None)
        {
          exitCode += (int)CommandExitCodes.CoverageBelowThreshold;
          var exceptionMessageBuilder = new StringBuilder();
          if ((thresholdTypeFlags & ThresholdTypeFlags.Line) != ThresholdTypeFlags.None)
          {
            exceptionMessageBuilder.AppendLine($"The {thresholdStat.ToString().ToLower()} line coverage is below the specified {thresholdTypeFlagValues[ThresholdTypeFlags.Line]}");
          }

          if ((thresholdTypeFlags & ThresholdTypeFlags.Branch) != ThresholdTypeFlags.None)
          {
            exceptionMessageBuilder.AppendLine($"The {thresholdStat.ToString().ToLower()} branch coverage is below the specified {thresholdTypeFlagValues[ThresholdTypeFlags.Branch]}");
          }

          if ((thresholdTypeFlags & ThresholdTypeFlags.Method) != ThresholdTypeFlags.None)
          {
            exceptionMessageBuilder.AppendLine($"The {thresholdStat.ToString().ToLower()} method coverage is below the specified {thresholdTypeFlagValues[ThresholdTypeFlags.Method]}");
          }
          throw new Exception(exceptionMessageBuilder.ToString());
        }

        return Task.FromResult(exitCode);

      }

      catch (Win32Exception we) when (we.Source == "System.Diagnostics.Process")
      {
        logger.LogError($"Start process '{target}' failed with '{we.Message}'");
        return Task.FromResult(exitCode > 0 ? exitCode : (int)CommandExitCodes.Exception);
      }
      catch (Exception ex)
      {
        logger.LogError(ex.Message);
        return Task.FromResult(exitCode > 0 ? exitCode : (int)CommandExitCodes.Exception);
      }

    }

    static string InvariantFormat(double value) => value.ToString(CultureInfo.InvariantCulture);
  }
}
