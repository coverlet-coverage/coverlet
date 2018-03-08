using System;
using Microsoft.Extensions.CommandLineUtils;

namespace coverlet.console
{
    class Program
    {
        static void Main(string[] args)
        {
            var app = new CommandLineApplication();
            app.Name = "coverlet";
            app.FullName = "Cross platform .NET Core code coverage tool";
            app.HelpOption("-h|--help");
            app.VersionOption("-v|--version", GetAssemblyVersion());

            app.OnExecute(() =>
            {
                app.ShowHelp();
                return 0;
            });
        }

        static string GetAssemblyVersion() => typeof(Program).Assembly.GetName().Version.ToString();
    }
}
