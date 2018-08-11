using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

using ConsoleTables;
using Coverlet.Console.Logging;
using Coverlet.Core;
using Coverlet.Core.Reporters;

using Microsoft.Extensions.CommandLineUtils;

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
            CommandOption formats = app.Option("-f|--format", "Format of the generated coverage report.", CommandOptionType.MultipleValue);
            CommandOption threshold = app.Option("--threshold", "Exits with error if the coverage % is below value.", CommandOptionType.SingleValue);
            CommandOption thresholdTypes = app.Option("--threshold-type", "Coverage type to apply the threshold to.", CommandOptionType.MultipleValue);
            CommandOption excludeFilters = app.Option("--exclude", "Filter expressions to exclude specific modules and types.", CommandOptionType.MultipleValue);
            CommandOption includeFilters = app.Option("--include", "Filter expressions to include only specific modules and types.", CommandOptionType.MultipleValue);
            CommandOption excludedSourceFiles = app.Option("--exclude-by-file", "Glob patterns specifying source files to exclude.", CommandOptionType.MultipleValue);

            app.OnExecute(() =>
            {
                if (string.IsNullOrEmpty(module.Value) || string.IsNullOrWhiteSpace(module.Value))
                    throw new CommandParsingException(app, "No test assembly specified.");

                if (!target.HasValue())
                    throw new CommandParsingException(app, "Target must be specified.");

                Coverage coverage = new Coverage(module.Value, excludeFilters.Values.ToArray(), includeFilters.Values.ToArray(), excludedSourceFiles.Values.ToArray());
                coverage.PrepareModules();

                Process process = new Process();
                process.StartInfo.FileName = target.Value();
                process.StartInfo.Arguments = targs.HasValue() ? targs.Value() : string.Empty;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.Start();
                logger.LogInformation(process.StandardOutput.ReadToEnd());
                process.WaitForExit();

                var dOutput = output.HasValue() ? output.Value() : Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar.ToString();
                var dThreshold = threshold.HasValue() ? int.Parse(threshold.Value()) : 0;
                var dThresholdTypes = thresholdTypes.HasValue() ? thresholdTypes.Values : new List<string>(new string[] { "line", "branch", "method" });

                logger.LogInformation("\nCalculating coverage result...");

                var result = coverage.GetCoverageResult();
                var directory = Path.GetDirectoryName(dOutput);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                foreach (var format in (formats.HasValue() ? formats.Values : new List<string>(new string[] { "json" })))
                {
                    var reporter = new ReporterFactory(format).CreateReporter();
                    if (reporter == null)
                        throw new Exception($"Specified output format '{format}' is not supported");

                    var filename = Path.GetFileName(dOutput);
                    filename = (filename == string.Empty) ? $"coverage.{reporter.Extension}" : filename;
                    filename = Path.HasExtension(filename) ? filename : $"{filename}.{reporter.Extension}";

                    var report = Path.Combine(directory, filename);
                    logger.LogInformation($"  Generating report '{report}'");
                    using (var streamWriter = File.CreateText(report))
                        reporter.Report(result, streamWriter);
                }

                var summary = new CoverageSummary();
                var exceptionBuilder = new StringBuilder();
                var coverageTable = new ConsoleTable("Module", "Line", "Branch", "Method");
                var thresholdFailed = false;

                foreach (var _module in result.Modules)
                {
                    var linePercent = summary.CalculateLineCoverage(_module.Value).Percent * 100;
                    var branchPercent = summary.CalculateBranchCoverage(_module.Value).Percent * 100;
                    var methodPercent = summary.CalculateMethodCoverage(_module.Value).Percent * 100;

                    coverageTable.AddRow(Path.GetFileNameWithoutExtension(_module.Key), $"{linePercent}%", $"{branchPercent}%", $"{methodPercent}%");

                    if (dThreshold > 0)
                    {
                        if (linePercent < dThreshold && dThresholdTypes.Contains("line"))
                        {
                            exceptionBuilder.AppendLine($"'{Path.GetFileNameWithoutExtension(_module.Key)}' has a line coverage '{linePercent}%' below specified threshold '{dThreshold}%'");
                            thresholdFailed = true;
                        }

                        if (branchPercent < dThreshold && dThresholdTypes.Contains("branch"))
                        {
                            exceptionBuilder.AppendLine($"'{Path.GetFileNameWithoutExtension(_module.Key)}' has a branch coverage '{branchPercent}%' below specified threshold '{dThreshold}%'");
                            thresholdFailed = true;
                        }

                        if (methodPercent < dThreshold && dThresholdTypes.Contains("method"))
                        {
                            exceptionBuilder.AppendLine($"'{Path.GetFileNameWithoutExtension(_module.Key)}' has a method coverage '{methodPercent}%' below specified threshold '{dThreshold}%'");
                            thresholdFailed = true;
                        }
                    }
                }

                logger.LogInformation(string.Empty);
                logger.LogInformation(coverageTable.ToStringAlternative());

                if (thresholdFailed)
                    throw new Exception(exceptionBuilder.ToString().TrimEnd(Environment.NewLine.ToCharArray()));

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
