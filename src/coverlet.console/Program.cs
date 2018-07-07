using System;
using System.Diagnostics;
using System.IO;

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
            CommandOption format = app.Option("-f|--format", "Format of the generated coverage report.", CommandOptionType.MultipleValue);
            CommandOption threshold = app.Option("--threshold", "Exits with error if the coverage % is below value.", CommandOptionType.SingleValue);
            CommandOption thresholdType = app.Option("--threshold-type", "Coverage type to apply the threshold to.", CommandOptionType.MultipleValue);
            CommandOption filters = app.Option("--exclude", "Filter expressions to exclude specific modules and types.", CommandOptionType.MultipleValue);
            CommandOption excludes = app.Option("--exclude-by-file", "Glob patterns specifying source files to exclude.", CommandOptionType.MultipleValue);

            app.OnExecute(() =>
            {
                if (string.IsNullOrEmpty(module.Value) || string.IsNullOrWhiteSpace(module.Value))
                    throw new CommandParsingException(app, "No test assembly specified.");

                if (!target.HasValue())
                    throw new CommandParsingException(app, "Target must be specified.");

                Coverage coverage = new Coverage(module.Value, filters.Values.ToArray(), excludes.Values.ToArray());
                coverage.PrepareModules();

                Process process = new Process();
                process.StartInfo.FileName = target.Value();
                process.StartInfo.Arguments = targs.HasValue() ? targs.Value() : string.Empty;
                process.StartInfo.CreateNoWindow = true;
                process.Start();
                process.WaitForExit();

                return 0;
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
        }

        static string GetAssemblyVersion() => typeof(Program).Assembly.GetName().Version.ToString();
    }
}
