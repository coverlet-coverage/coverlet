using System;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.Extensions.CommandLineUtils;

namespace coverlet.console
{
    class Program
    {
        static int Main(string[] args)
        {
            var app = new CommandLineApplication();
            app.Name = "coverlet";
            app.FullName = "Cross platform .NET Core code coverage tool";
            app.HelpOption("-h|--help");
            app.VersionOption("-v|--version", GetAssemblyVersion());

            CommandArgument project = app.Argument("<PROJECT>", "The project to test. Defaults to the current directory.");
            CommandOption config = app.Option("-c|--configuration", "Configuration to use for building the project.", CommandOptionType.SingleValue);
            CommandOption intermediateResult = app.Option("-i|--coverage-intermediate-result", "The output path of intermediate result (for merging multiple runs).", CommandOptionType.SingleValue);
            CommandOption output = app.Option("-o|--coverage-output", "The output path of the generated coverage report", CommandOptionType.SingleValue);
            CommandOption format = app.Option("-f|--coverage-format", "The format of the coverage report", CommandOptionType.SingleValue);

            app.OnExecute(() =>
            {
                var dotnetTestArgs = new List<string>();

                dotnetTestArgs.Add("test");

                if (project.Value != string.Empty)
                    dotnetTestArgs.Add(project.Value);

                if (config.HasValue())
                    dotnetTestArgs.Add($"-c {config.Value()}");

                dotnetTestArgs.Add("/p:CollectCoverage=true");

                if (intermediateResult.HasValue())
                    dotnetTestArgs.Add($"/p:CoverletIntermediateResult={intermediateResult.Value()}");

                if (output.HasValue())
                    dotnetTestArgs.Add($"/p:CoverletOutput={output.Value()}");

                if (format.HasValue())
                    dotnetTestArgs.Add($"/p:CoverletOutputFormat={format.Value()}");

                Process process = new Process();
                process.StartInfo.FileName = "dotnet";
                process.StartInfo.Arguments = string.Join(" ", dotnetTestArgs);
                process.StartInfo.CreateNoWindow = true;

                process.Start();
                process.WaitForExit();

                return process.ExitCode;
            });

            try
            {
                return app.Execute(args);
            }
            catch (CommandParsingException ex)
            {
                Console.WriteLine(ex.Message);
                app.ShowHelp();
                return 1;
            }
        }

        static string GetAssemblyVersion() => typeof(Program).Assembly.GetName().Version.ToString();
    }
}
