﻿// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using Coverlet.Core;
using Newtonsoft.Json;
using NuGet.Packaging;
using Xunit;
using Xunit.Sdk;

namespace Coverlet.Integration.Tests
{
    [Flags]
    public enum BuildConfiguration
    {
        Debug = 1,
        Release = 2
    }

    public abstract class BaseTest
    {
        private static int s_folderSuffix;

        protected BuildConfiguration GetAssemblyBuildConfiguration()
        {
#if DEBUG
            return BuildConfiguration.Debug;
#endif
#if RELEASE
            return BuildConfiguration.Release;
#endif
            throw new NotSupportedException($"Build configuration not supported");
        }

        private protected string GetPackageVersion(string filter)
        {
            string packagesPath = $"../../../../../bin/{GetAssemblyBuildConfiguration()}/Packages";

            if (!Directory.Exists(packagesPath))
            {
                throw new DirectoryNotFoundException("Package directory not found, run 'dotnet pack' on repository root");
            }

            var files = Directory.GetFiles(packagesPath, filter).ToList();
            if (files.Count == 0)
            {
                throw new InvalidOperationException($"Could not find any package using filter '{filter}' in folder '{Path.GetFullPath(packagesPath)}'. Make sure 'dotnet pack' was called.");
            }
            else if (files.Count > 1)
            {
                throw new InvalidOperationException($"Found more than one package using filter '{filter}' in folder '{Path.GetFullPath(packagesPath)}'. Make sure 'dotnet pack' was only called once.");
            }
            else
            {
                using Stream pkg = File.OpenRead(files[0]);
                using var reader = new PackageArchiveReader(pkg);
                using Stream nuspecStream = reader.GetNuspec();
                var manifest = Manifest.ReadFrom(nuspecStream, false);
                return manifest.Metadata.Version.OriginalVersion;
            }
        }

        private protected ClonedTemplateProject CloneTemplateProject(bool cleanupOnDispose = true, string testSDKVersion = "17.5.0")
        {
            DirectoryInfo finalRoot = Directory.CreateDirectory($"{Guid.NewGuid().ToString("N")[..6]}{Interlocked.Increment(ref s_folderSuffix)}");
            foreach (string file in (Directory.GetFiles($"../../../../coverlet.integration.template", "*.cs")
                    .Union(Directory.GetFiles($"../../../../coverlet.integration.template", "*.csproj")
                    .Union(Directory.GetFiles($"../../../../coverlet.integration.template", "nuget.config")))))
            {
                File.Copy(file, Path.Combine(finalRoot.FullName, Path.GetFileName(file)));
            }

            // We need to prevent the inheritance of global props/targets for template project
            File.WriteAllText(Path.Combine(finalRoot.FullName, "Directory.Build.props"),
@"<?xml version=""1.0"" encoding=""utf-8""?>
<Project>
</Project>");

            File.WriteAllText(Path.Combine(finalRoot.FullName, "Directory.Build.targets"),
@"<?xml version=""1.0"" encoding=""utf-8""?>
<Project>
</Project>");

            AddMicrosoftNETTestSdkRef(finalRoot.FullName, testSDKVersion);

            SetIsTestProjectTrue(finalRoot.FullName);

            return new ClonedTemplateProject(finalRoot.FullName, cleanupOnDispose);
        }

        private protected bool RunCommand(string command, string arguments, out string standardOutput, out string standardError, string workingDirectory = "")
        {
            Debug.WriteLine($"BaseTest.RunCommand: {command} {arguments}\nWorkingDirectory: {workingDirectory}");
            // https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.process.standardoutput?view=net-7.0&redirectedfrom=MSDN#System_Diagnostics_Process_StandardOutput
            var commandProcess = new Process();
            commandProcess.StartInfo.FileName = command;
            commandProcess.StartInfo.Arguments = arguments;
            commandProcess.StartInfo.WorkingDirectory = workingDirectory;
            commandProcess.StartInfo.RedirectStandardError = true;
            commandProcess.StartInfo.RedirectStandardOutput = true;
            commandProcess.StartInfo.UseShellExecute = false;
            string eOut = "";
            commandProcess.ErrorDataReceived += new DataReceivedEventHandler((sender, e) => { eOut += e.Data; });
            commandProcess.Start();
            // To avoid deadlocks, use an asynchronous read operation on at least one of the streams.
            commandProcess.BeginErrorReadLine();
            standardOutput = commandProcess.StandardOutput.ReadToEnd();
            if (!commandProcess.WaitForExit((int)TimeSpan.FromMinutes(5).TotalMilliseconds))
            {
              throw new XunitException($"Command 'dotnet {arguments}' didn't end after 5 minute");
            }
            standardError = eOut;
            return commandProcess.ExitCode == 0;
          }

        private protected bool DotnetCli(string arguments, out string standardOutput, out string standardError, string workingDirectory = "")
        {
            return RunCommand("dotnet", arguments, out standardOutput, out standardError, workingDirectory);
        }

        private protected void UpdateNugeConfigtWithLocalPackageFolder(string projectPath)
        {
            string nugetFile = Path.Combine(projectPath, "nuget.config");
            if (!File.Exists(nugetFile))
            {
                throw new FileNotFoundException("Nuget.config not found", "nuget.config");
            }
            XDocument xml;
            using (FileStream? nugetFileStream = File.OpenRead(nugetFile))
            {
                xml = XDocument.Load(nugetFileStream);
            }

            string localPackageFolder = Path.GetFullPath($"../../../../../bin/{GetAssemblyBuildConfiguration()}/Packages");
            xml.Element("configuration")!
               .Element("packageSources")!
               .Elements()
               .ElementAt(0)
               .AddAfterSelf(new XElement("add", new XAttribute("key", "localCoverletPackages"), new XAttribute("value", localPackageFolder)));
            xml.Save(nugetFile);
        }

        private void SetIsTestProjectTrue(string projectPath)
        {
            string csproj = Path.Combine(projectPath, "coverlet.integration.template.csproj");
            if (!File.Exists(csproj))
            {
                throw new FileNotFoundException("coverlet.integration.template.csproj not found", "coverlet.integration.template.csproj");
            }
            XDocument xml;
            using (FileStream? csprojStream = File.OpenRead(csproj))
            {
                xml = XDocument.Load(csprojStream);
            }

            xml.Element("Project")!
               .Element("PropertyGroup")!
               .Element("IsTestProject")!.Value = "true";

            xml.Save(csproj);
        }

        private protected void AddMicrosoftNETTestSdkRef(string projectPath, string version)
        {
            string csproj = Path.Combine(projectPath, "coverlet.integration.template.csproj");
            if (!File.Exists(csproj))
            {
                throw new FileNotFoundException("coverlet.integration.template.csproj not found", "coverlet.integration.template.csproj");
            }
            XDocument xml;
            using (FileStream? csprojStream = File.OpenRead(csproj))
            {
                xml = XDocument.Load(csprojStream);
            }

            xml.Element("Project")!
               .Element("ItemGroup")!
               .Add(new XElement("PackageReference", new XAttribute("Include", "Microsoft.NET.Test.Sdk"),
               new XAttribute("Version", version)));
            xml.Save(csproj);
        }

        private protected void AddCoverletMsbuildRef(string projectPath)
        {
            string csproj = Path.Combine(projectPath, "coverlet.integration.template.csproj");
            if (!File.Exists(csproj))
            {
                throw new FileNotFoundException("coverlet.integration.template.csproj not found", "coverlet.integration.template.csproj");
            }
            XDocument xml;
            using (FileStream? csprojStream = File.OpenRead(csproj))
            {
                xml = XDocument.Load(csprojStream);
            }
            string msbuildPkgVersion = GetPackageVersion("*msbuild*.nupkg");
            xml.Element("Project")!
               .Element("ItemGroup")!
               .Add(new XElement("PackageReference", new XAttribute("Include", "coverlet.msbuild"), new XAttribute("Version", msbuildPkgVersion),
                    new XElement("PrivateAssets", "all"),
                    new XElement("IncludeAssets", "runtime; build; native; contentfiles; analyzers")));
            xml.Save(csproj);
        }

        private protected void AddCoverletCollectosRef(string projectPath)
        {
            string csproj = Path.Combine(projectPath, "coverlet.integration.template.csproj");
            if (!File.Exists(csproj))
            {
                throw new FileNotFoundException("coverlet.integration.template.csproj not found", "coverlet.integration.template.csproj");
            }
            XDocument xml;
            using (FileStream? csprojStream = File.OpenRead(csproj))
            {
                xml = XDocument.Load(csprojStream);
            }
            string msbuildPkgVersion = GetPackageVersion("*collector*.nupkg");
            xml.Element("Project")!
               .Element("ItemGroup")!
               .Add(new XElement("PackageReference", new XAttribute("Include", "coverlet.collector"), new XAttribute("Version", msbuildPkgVersion),
                    new XElement("PrivateAssets", "all"),
                    new XElement("IncludeAssets", "runtime; build; native; contentfiles; analyzers")));
            xml.Save(csproj);
        }

        private protected string AddCollectorRunsettingsFile(string projectPath, string includeFilter = "[coverletsamplelib.integration.template]*DeepThought", bool sourceLink = false, bool deterministicReport = false)
        {
            string runSettings =
$@"<?xml version=""1.0"" encoding=""utf-8"" ?>
  <RunSettings>
    <DataCollectionRunSettings>  
      <DataCollectors>  
        <DataCollector friendlyName=""XPlat code coverage"" >
           <Configuration>
            <Format>json,cobertura</Format>
            <DeterministicReport>{deterministicReport}</DeterministicReport>
            <Include>{includeFilter}</Include>
            <UseSourceLink>{(sourceLink ? "true" : "false")}</UseSourceLink>
            <!-- We need to include test assembly because test and code to cover are in same template project -->
            <IncludeTestAssembly>true</IncludeTestAssembly>
        </Configuration>
      </DataCollector>
    </DataCollectors>
  </DataCollectionRunSettings>
</RunSettings>";

            string runsettingsPath = Path.Combine(projectPath, "runSettings");
            File.WriteAllText(runsettingsPath, runSettings);
            return runsettingsPath;
        }

        private protected void AssertCoverage(ClonedTemplateProject clonedTemplateProject, string filter = "coverage.json", string standardOutput = "")
        {
            if (GetAssemblyBuildConfiguration() == BuildConfiguration.Debug)
            {
                bool coverageChecked = false;
                foreach (string coverageFile in clonedTemplateProject.GetFiles(filter))
                {
                    Classes? document = JsonConvert.DeserializeObject<Modules>(File.ReadAllText(coverageFile))?.Document("DeepThought.cs");
                    if (document != null)
                    {
                        document.Class("Coverlet.Integration.Template.DeepThought")
                            .Method("System.Int32 Coverlet.Integration.Template.DeepThought::AnswerToTheUltimateQuestionOfLifeTheUniverseAndEverything()")
                            .AssertLinesCovered((6, 1), (7, 1), (8, 1));
                        coverageChecked = true;
                    }
                }

                Assert.True(coverageChecked, $"Coverage check fail\n{standardOutput}");
            }
        }

        private protected void UpdateProjectTargetFramework(ClonedTemplateProject project, params string[] targetFrameworks)
        {
            if (targetFrameworks is null || targetFrameworks.Length == 0)
            {
                throw new ArgumentException("Invalid targetFrameworks", nameof(targetFrameworks));
            }

            if (!File.Exists(project.ProjectFileNamePath))
            {
                throw new FileNotFoundException("coverlet.integration.template.csproj not found", "coverlet.integration.template.csproj");
            }
            XDocument xml;
            using (FileStream? csprojStream = File.OpenRead(project.ProjectFileNamePath))
            {
                xml = XDocument.Load(csprojStream);
            }

            xml.Element("Project")!
              .Element("PropertyGroup")!
              .Element("TargetFramework")!
              .Remove();

            XElement targetFrameworkElement;

            if (targetFrameworks.Length == 1)
            {
                targetFrameworkElement = new XElement("TargetFramework", targetFrameworks[0]);
            }
            else
            {
                targetFrameworkElement = new XElement("TargetFrameworks", string.Join(';', targetFrameworks));
            }

            xml.Element("Project")!.Element("PropertyGroup")!.Add(targetFrameworkElement);
            xml.Save(project.ProjectFileNamePath);
        }

        private protected void PinSDK(ClonedTemplateProject project, string sdkVersion)
        {
            if (project is null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            if (string.IsNullOrEmpty(sdkVersion))
            {
                throw new ArgumentException("Invalid sdkVersion", nameof(sdkVersion));
            }

            if (!File.Exists(project.ProjectFileNamePath))
            {
                throw new FileNotFoundException("coverlet.integration.template.csproj not found", "coverlet.integration.template.csproj");
            }

            if (project.ProjectRootPath is null || !Directory.Exists(project.ProjectRootPath))
            {
                throw new ArgumentException("Invalid ProjectRootPath");
            }

            File.WriteAllText(Path.Combine(project.ProjectRootPath, "global.json"), $"{{ \"sdk\": {{ \"version\": \"{sdkVersion}\" }} }}");
        }
    }

    class ClonedTemplateProject : IDisposable
    {
        public string ProjectRootPath { get; private set; }
        public bool CleanupOnDispose { get; private set; }

        // We need to have a different asm name to avoid issue with collectors, we filter [coverlet.*]* by default
        // https://github.com/tonerdo/coverlet/pull/410#discussion_r284526728
        public static string AssemblyName { get; } = "coverletsamplelib.integration.template";
        public static string ProjectFileName { get; } = "coverlet.integration.template.csproj";
        public string ProjectFileNamePath => Path.Combine(ProjectRootPath, "coverlet.integration.template.csproj");

        public ClonedTemplateProject(string? projectRootPath, bool cleanupOnDispose)
        {
            ProjectRootPath = (projectRootPath ?? throw new ArgumentNullException(nameof(projectRootPath)));
            CleanupOnDispose = cleanupOnDispose;
        }

        public bool IsMultipleTargetFramework()
        {
            using FileStream? csprojStream = File.OpenRead(ProjectFileNamePath);
            var xml = XDocument.Load(csprojStream);
            return xml.Element("Project")!.Element("PropertyGroup")!.Element("TargetFramework") == null;
        }

        public string[] GetTargetFrameworks()
        {
            using FileStream? csprojStream = File.OpenRead(ProjectFileNamePath);
            var xml = XDocument.Load(csprojStream);
            XElement element = xml.Element("Project")!.Element("PropertyGroup")!.Element("TargetFramework") ?? xml.Element("Project")!.Element("PropertyGroup")!.Element("TargetFrameworks")!;
            if (element is null)
            {
                throw new ArgumentNullException("No 'TargetFramework' neither 'TargetFrameworks' found in csproj file");
            }
            return element.Value.Split(";");
        }

        public string[] GetFiles(string filter)
        {
            return Directory.GetFiles(ProjectRootPath, filter, SearchOption.AllDirectories);
        }

        public void Dispose()
        {
            if (CleanupOnDispose)
            {
                try
                {
                    // Directory.Delete(ProjectRootPath, true);
                }
                catch (UnauthorizedAccessException)
                {
                    // Sometimes on CI AzDo we get Access Denied on delete
                    // swallowed exception to not waste time
                }
            }
        }
    }
}
