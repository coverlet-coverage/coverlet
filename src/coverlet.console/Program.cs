using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using ConsoleTables;
using Coverlet.Console.Logging;
using Coverlet.Core;
using Coverlet.Core.Enums;
using Coverlet.Core.Reporters;
using McMaster.Extensions.CommandLineUtils;

namespace Coverlet.Console
{
    class Program
    {
        static int Main(string[] args)
        {
            var logger = new ConsoleLogger();
            var app = new CommandLineApplication();
            app.Name = "coverlet";
            app.FullName = "Cross platform .NET Core code coverage tool";
            app.HelpOption("-h|--help");
            app.VersionOption("-v|--version", GetAssemblyVersion());

            CommandArgument module = app.Argument("<ASSEMBLY>", "Path to the test assembly.");
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
            CommandOption includeTestAssembly = app.Option("--include-test-assembly", "Specifies whether to report code coverage of the test assembly", CommandOptionType.NoValue);
            CommandOption singleHit = app.Option("--single-hit", "Specifies whether to limit code coverage hit reporting to a single hit for each location", CommandOptionType.NoValue);
            CommandOption mergeWith = app.Option("--merge-with", "Path to existing coverage result to merge.", CommandOptionType.SingleValue);
            CommandOption useSourceLink = app.Option("--use-source-link", "Specifies whether to use SourceLink URIs in place of file system paths.", CommandOptionType.NoValue);

            app.OnExecute(() =>
            {
                if (string.IsNullOrEmpty(module.Value) || string.IsNullOrWhiteSpace(module.Value))
                    throw new CommandParsingException(app, "No test assembly specified.");

                if (!target.HasValue())
                    throw new CommandParsingException(app, "Target must be specified.");

                if (verbosity.HasValue())
                {
                    // Adjust log level based on user input.
                    logger.Level = verbosity.ParsedValue;
                }

                Coverage coverage = new Coverage(module.Value,
                    includeFilters.Values.ToArray(),
                    includeDirectories.Values.ToArray(),
                    excludeFilters.Values.ToArray(),
                    excludedSourceFiles.Values.ToArray(),
                    excludeAttributes.Values.ToArray(),
                    includeTestAssembly.HasValue(),
                    singleHit.HasValue(),
                    mergeWith.Value(),
                    useSourceLink.HasValue(),
                    logger);
                coverage.PrepareModules();

                Process process = new Process();
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

                var dOutput = output.HasValue() ? output.Value() : Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar.ToString();
                var dThreshold = threshold.HasValue() ? double.Parse(threshold.Value()) : 0;
                var dThresholdTypes = thresholdTypes.HasValue() ? thresholdTypes.Values : new List<string>(new string[] { "line", "branch", "method" });
                var dThresholdStat = thresholdStat.HasValue() ? Enum.Parse<ThresholdStatistic>(thresholdStat.Value(), true) : Enum.Parse<ThresholdStatistic>("minimum", true);

                logger.LogInformation("\nCalculating coverage result...");

                var result = coverage.GetCoverageResult();
                var directory = Path.GetDirectoryName(dOutput);
                if (directory == string.Empty)
                {
                    directory = Directory.GetCurrentDirectory();
                }
                else if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                foreach (var format in (formats.HasValue() ? formats.Values : new List<string>(new string[] { "json" })))
                {
                    var reporter = new ReporterFactory(format).CreateReporter();
                    if (reporter == null)
                    {
                        throw new Exception($"Specified output format '{format}' is not supported");
                    }

                    if (reporter.OutputType == ReporterOutputType.Console)
                    {
                        // Output to console
                        logger.LogInformation("  Outputting results to console", important: true);
                        logger.LogInformation(reporter.Report(result), important: true);
                    }
                    else
                    {
                        // Output to file
                        var filename = Path.GetFileName(dOutput);
                        filename = (filename == string.Empty) ? $"coverage.{reporter.Extension}" : filename;
                        filename = Path.HasExtension(filename) ? filename : $"{filename}.{reporter.Extension}";

                        var report = Path.Combine(directory, filename);
                        logger.LogInformation($"  Generating report '{report}'", important: true);
                        File.WriteAllText(report, reporter.Report(result));
                    }
                }

                var thresholdTypeFlags = ThresholdTypeFlags.None;

                foreach (var thresholdType in dThresholdTypes)
                {
                    if (thresholdType.Equals("line", StringComparison.OrdinalIgnoreCase))
                    {
                        thresholdTypeFlags |= ThresholdTypeFlags.Line;
                    }
                    else if (thresholdType.Equals("branch", StringComparison.OrdinalIgnoreCase))
                    {
                        thresholdTypeFlags |= ThresholdTypeFlags.Branch;
                    }
                    else if (thresholdType.Equals("method", StringComparison.OrdinalIgnoreCase))
                    {
                        thresholdTypeFlags |= ThresholdTypeFlags.Method;
                    }
                }

                var coverageTable = new ConsoleTable("Module", "Line", "Branch", "Method");
                var summary = new CoverageSummary();
                int numModules = result.Modules.Count;

                var totalLinePercent = summary.CalculateLineCoverage(result.Modules).Percent * 100;
                var totalBranchPercent = summary.CalculateBranchCoverage(result.Modules).Percent * 100;
                var totalMethodPercent = summary.CalculateMethodCoverage(result.Modules).Percent * 100;

                foreach (var _module in result.Modules)
                {
                    var linePercent = summary.CalculateLineCoverage(_module.Value).Percent * 100;
                    var branchPercent = summary.CalculateBranchCoverage(_module.Value).Percent * 100;
                    var methodPercent = summary.CalculateMethodCoverage(_module.Value).Percent * 100;

                    coverageTable.AddRow(Path.GetFileNameWithoutExtension(_module.Key), $"{linePercent}%", $"{branchPercent}%", $"{methodPercent}%");
                }

                logger.LogInformation(coverageTable.ToStringAlternative());

                coverageTable.Columns.Clear();
                coverageTable.Rows.Clear();

                coverageTable.AddColumn(new[] { "", "Line", "Branch", "Method" });
                coverageTable.AddRow("Total", $"{totalLinePercent}%", $"{totalBranchPercent}%", $"{totalMethodPercent}%");
                coverageTable.AddRow("Average", $"{totalLinePercent / numModules}%", $"{totalBranchPercent / numModules}%", $"{totalMethodPercent / numModules}%");

                logger.LogInformation(coverageTable.ToStringAlternative());

                thresholdTypeFlags = result.GetThresholdTypesBelowThreshold(summary, dThreshold, thresholdTypeFlags, dThresholdStat);
                if (thresholdTypeFlags != ThresholdTypeFlags.None)
                {
                    var exceptionMessageBuilder = new StringBuilder();
                    if ((thresholdTypeFlags & ThresholdTypeFlags.Line) != ThresholdTypeFlags.None)
                    {
                        exceptionMessageBuilder.AppendLine($"The {dThresholdStat.ToString().ToLower()} line coverage is below the specified {dThreshold}");
                    }

                    if ((thresholdTypeFlags & ThresholdTypeFlags.Branch) != ThresholdTypeFlags.None)
                    {
                        exceptionMessageBuilder.AppendLine($"The {dThresholdStat.ToString().ToLower()} branch coverage is below the specified {dThreshold}");
                    }

                    if ((thresholdTypeFlags & ThresholdTypeFlags.Method) != ThresholdTypeFlags.None)
                    {
                        exceptionMessageBuilder.AppendLine($"The {dThresholdStat.ToString().ToLower()} method coverage is below the specified {dThreshold}");
                    }

                    throw new Exception(exceptionMessageBuilder.ToString());
                }

                return process.ExitCode == 0 ? 0 : process.ExitCode;
            });

            try
            {
                return app.Execute(args);
            }
            catch (CommandParsingException ex)
            {
                logger.LogError(ex.Message);
                app.ShowHelp();
                return 1;
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                return 1;
            }
        }

        static string GetAssemblyVersion() => typeof(Program).Assembly.GetName().Version.ToString();
    }
}
