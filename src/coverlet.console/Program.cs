// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using ConsoleTables;
using Coverlet.Console.Logging;
using Coverlet.Core;
using Coverlet.Core.Abstractions;
using Coverlet.Core.Enums;
using Coverlet.Core.Helpers;
using Coverlet.Core.Reporters;
using Coverlet.Core.Symbols;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyInjection;

namespace Coverlet.Console
{
    class Program
    {
        static int Main(string[] args)
        {
            IServiceCollection serviceCollection = new ServiceCollection();
            serviceCollection.AddTransient<IRetryHelper, RetryHelper>();
            serviceCollection.AddTransient<IProcessExitHandler, ProcessExitHandler>();
            serviceCollection.AddTransient<IFileSystem, FileSystem>();
            serviceCollection.AddTransient<ILogger, ConsoleLogger>();
            // We need to keep singleton/static semantics
            serviceCollection.AddSingleton<IInstrumentationHelper, InstrumentationHelper>();
            serviceCollection.AddSingleton<ISourceRootTranslator, SourceRootTranslator>(provider => new SourceRootTranslator(provider.GetRequiredService<ILogger>(), provider.GetRequiredService<IFileSystem>()));
            serviceCollection.AddSingleton<ICecilSymbolHelper, CecilSymbolHelper>();

            ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();

            var logger = (ConsoleLogger)serviceProvider.GetService<ILogger>();
            IFileSystem fileSystem = serviceProvider.GetService<IFileSystem>();

            var app = new CommandLineApplication
            {
                Name = "coverlet",
                FullName = "Cross platform .NET Core code coverage tool"
            };
            app.HelpOption("-h|--help");
            app.VersionOption("-v|--version", GetAssemblyVersion());
            int exitCode = (int)CommandExitCodes.Success;

            CommandArgument moduleOrAppDirectory = app.Argument("<ASSEMBLY|DIRECTORY>", "Path to the test assembly or application directory.");
            CommandOption target = app.Option("-t|--target", "Path to the test runner application.", CommandOptionType.SingleValue);
            CommandOption targs = app.Option("-a|--targetargs", "Arguments to be passed to the test runner.", CommandOptionType.SingleValue);
            CommandOption output = app.Option("-o|--output", "Output of the generated coverage report", CommandOptionType.SingleValue);
            CommandOption<LogLevel> verbosity = app.Option<LogLevel>("-v|--verbosity", "Sets the verbosity level of the command. Allowed values are quiet, minimal, normal, detailed.", CommandOptionType.SingleValue);
            CommandOption formats = app.Option("-f|--format", "Format of the generated coverage report.", CommandOptionType.MultipleValue);
            CommandOption threshold = app.Option("--threshold", "Exits with error if the coverage % is below value.", CommandOptionType.SingleValue);
            CommandOption thresholdTypes = app.Option("--threshold-type", "Coverage type to apply the threshold to.", CommandOptionType.MultipleValue);
            CommandOption thresholdStat = app.Option("--threshold-stat", "Coverage statistic used to enforce the threshold value.", CommandOptionType.SingleValue);
            CommandOption excludeFilters = app.Option("--exclude", "Filter expressions to exclude specific modules and types.", CommandOptionType.MultipleValue);
            CommandOption includeFilters = app.Option("--include", "Filter expressions to include only specific modules and types.", CommandOptionType.MultipleValue);
            CommandOption excludedSourceFiles = app.Option("--exclude-by-file", "Glob patterns specifying source files to exclude.", CommandOptionType.MultipleValue);
            CommandOption includeDirectories = app.Option("--include-directory", "Include directories containing additional assemblies to be instrumented.", CommandOptionType.MultipleValue);
            CommandOption excludeAttributes = app.Option("--exclude-by-attribute", "Attributes to exclude from code coverage.", CommandOptionType.MultipleValue);
            CommandOption includeTestAssembly = app.Option("--include-test-assembly", "Specifies whether to report code coverage of the test assembly.", CommandOptionType.NoValue);
            CommandOption singleHit = app.Option("--single-hit", "Specifies whether to limit code coverage hit reporting to a single hit for each location", CommandOptionType.NoValue);
            CommandOption skipAutoProp = app.Option("--skipautoprops", "Neither track nor record auto-implemented properties.", CommandOptionType.NoValue);
            CommandOption mergeWith = app.Option("--merge-with", "Path to existing coverage result to merge.", CommandOptionType.SingleValue);
            CommandOption useSourceLink = app.Option("--use-source-link", "Specifies whether to use SourceLink URIs in place of file system paths.", CommandOptionType.NoValue);
            CommandOption doesNotReturnAttributes = app.Option("--does-not-return-attribute", "Attributes that mark methods that do not return.", CommandOptionType.MultipleValue);
            CommandOption instrumentModulesWithoutLocalSources = app.Option("--instrument-modules-without-local-sources", "Specifies whether modules should be instrumented even if the sources from the PDBs can't be found locally.", CommandOptionType.MultipleValue);

            app.OnExecute(() =>
            {
                if (string.IsNullOrEmpty(moduleOrAppDirectory.Value) || string.IsNullOrWhiteSpace(moduleOrAppDirectory.Value))
                    throw new CommandParsingException(app, "No test assembly or application directory specified.");

                if (!target.HasValue())
                    throw new CommandParsingException(app, "Target must be specified.");

                if (verbosity.HasValue())
                {
                    // Adjust log level based on user input.
                    logger.Level = verbosity.ParsedValue;
                }

                CoverageParameters parameters = new()
                {
                    IncludeFilters = includeFilters.Values.ToArray(),
                    IncludeDirectories = includeDirectories.Values.ToArray(),
                    ExcludeFilters = excludeFilters.Values.ToArray(),
                    ExcludedSourceFiles = excludedSourceFiles.Values.ToArray(),
                    ExcludeAttributes = excludeAttributes.Values.ToArray(),
                    IncludeTestAssembly = includeTestAssembly.HasValue(),
                    SingleHit = singleHit.HasValue(),
                    MergeWith = mergeWith.Value(),
                    UseSourceLink = useSourceLink.HasValue(),
                    SkipAutoProps = skipAutoProp.HasValue(),
                    DoesNotReturnAttributes = doesNotReturnAttributes.Values.ToArray(),
                    InstrumentModulesWithoutLocalSources = instrumentModulesWithoutLocalSources.HasValue(),
                };

                ISourceRootTranslator sourceRootTranslator = serviceProvider.GetRequiredService<ISourceRootTranslator>();

                Coverage coverage = new(moduleOrAppDirectory.Value,
                                                 parameters,
                                                 logger,
                                                 serviceProvider.GetRequiredService<IInstrumentationHelper>(),
                                                 fileSystem,
                                                 sourceRootTranslator,
                                                 serviceProvider.GetRequiredService<ICecilSymbolHelper>());
                coverage.PrepareModules();

                Process process = new();
                process.StartInfo.FileName = target.Value();
                process.StartInfo.Arguments = targs.HasValue() ? targs.Value() : string.Empty;
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

                string dOutput = output.HasValue() ? output.Value() : Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar.ToString();
                List<string> dThresholdTypes = thresholdTypes.HasValue() ? thresholdTypes.Values : new List<string>(new string[] { "line", "branch", "method" });
                ThresholdStatistic dThresholdStat = thresholdStat.HasValue() ? Enum.Parse<ThresholdStatistic>(thresholdStat.Value(), true) : Enum.Parse<ThresholdStatistic>("minimum", true);

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

                foreach (string format in formats.HasValue() ? formats.Values : new List<string>(new string[] { "json" }))
                {
                    Core.Abstractions.IReporter reporter = new ReporterFactory(format).CreateReporter();
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

                foreach (string thresholdType in dThresholdTypes)
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
                if (threshold.HasValue() && threshold.Value().Contains(','))
                {
                    IEnumerable<string> thresholdValues = threshold.Value().Split(',', StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim());
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
                    double thresholdValue = threshold.HasValue() ? double.Parse(threshold.Value()) : 0;

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

                ThresholdTypeFlags thresholdTypeFlags = result.GetThresholdTypesBelowThreshold(summary, thresholdTypeFlagValues, dThresholdStat);
                if (thresholdTypeFlags != ThresholdTypeFlags.None)
                {
                    exitCode += (int)CommandExitCodes.CoverageBelowThreshold;
                    var exceptionMessageBuilder = new StringBuilder();
                    if ((thresholdTypeFlags & ThresholdTypeFlags.Line) != ThresholdTypeFlags.None)
                    {
                        exceptionMessageBuilder.AppendLine($"The {dThresholdStat.ToString().ToLower()} line coverage is below the specified {thresholdTypeFlagValues[ThresholdTypeFlags.Line]}");
                    }

                    if ((thresholdTypeFlags & ThresholdTypeFlags.Branch) != ThresholdTypeFlags.None)
                    {
                        exceptionMessageBuilder.AppendLine($"The {dThresholdStat.ToString().ToLower()} branch coverage is below the specified {thresholdTypeFlagValues[ThresholdTypeFlags.Branch]}");
                    }

                    if ((thresholdTypeFlags & ThresholdTypeFlags.Method) != ThresholdTypeFlags.None)
                    {
                        exceptionMessageBuilder.AppendLine($"The {dThresholdStat.ToString().ToLower()} method coverage is below the specified {thresholdTypeFlagValues[ThresholdTypeFlags.Method]}");
                    }
                    throw new Exception(exceptionMessageBuilder.ToString());
                }

                return exitCode;
            });

            try
            {
                return app.Execute(args);
            }
            catch (CommandParsingException ex)
            {
                logger.LogError(ex.Message);
                app.ShowHelp();
                return (int)CommandExitCodes.CommandParsingException;
            }
            catch (Win32Exception we) when (we.Source == "System.Diagnostics.Process")
            {
                logger.LogError($"Start process '{target.Value()}' failed with '{we.Message}'");
                return exitCode > 0 ? exitCode : (int)CommandExitCodes.Exception;
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                return exitCode > 0 ? exitCode : (int)CommandExitCodes.Exception;
            }
        }

        static string GetAssemblyVersion() => typeof(Program).Assembly.GetName().Version.ToString();

        static string InvariantFormat(double value) => value.ToString(CultureInfo.InvariantCulture);
    }
}
