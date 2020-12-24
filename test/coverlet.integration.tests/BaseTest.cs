using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
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
        private static int _folderSuffix = 0;

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
            if (!Directory.Exists($"../../../../../bin/{GetAssemblyBuildConfiguration()}/Packages"))
            {
                throw new DirectoryNotFoundException("Package directory not found, run 'dotnet pack' on repository root");
            }

            using Stream pkg = File.OpenRead(Directory.GetFiles($"../../../../../bin/{GetAssemblyBuildConfiguration()}/Packages", filter).Single());
            using var reader = new PackageArchiveReader(pkg);
            using Stream nuspecStream = reader.GetNuspec();
            Manifest manifest = Manifest.ReadFrom(nuspecStream, false);
            return manifest.Metadata.Version.OriginalVersion;
        }

        private protected ClonedTemplateProject CloneTemplateProject(bool cleanupOnDispose = true, string testSDKVersion = "16.5.0")
        {
            DirectoryInfo finalRoot = Directory.CreateDirectory($"{Guid.NewGuid().ToString("N").Substring(0, 6)}{Interlocked.Increment(ref _folderSuffix)}");
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
            Debug.WriteLine($"BaseTest.RunCommand: {command} {arguments}");
            ProcessStartInfo psi = new ProcessStartInfo(command, arguments);
            psi.WorkingDirectory = workingDirectory;
            psi.RedirectStandardError = true;
            psi.RedirectStandardOutput = true;
            Process commandProcess = Process.Start(psi)!;
            if (!commandProcess.WaitForExit((int)TimeSpan.FromMinutes(5).TotalMilliseconds))
            {
                throw new XunitException($"Command 'dotnet {arguments}' didn't end after 5 minute");
            }
            standardOutput = commandProcess.StandardOutput.ReadToEnd();
            standardError = commandProcess.StandardError.ReadToEnd();
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
            using (var nugetFileStream = File.OpenRead(nugetFile))
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
            using (var csprojStream = File.OpenRead(csproj))
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
            using (var csprojStream = File.OpenRead(csproj))
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
            using (var csprojStream = File.OpenRead(csproj))
            {
                xml = XDocument.Load(csprojStream);
            }
            string msbuildPkgVersion = GetPackageVersion("*msbuild*.nupkg");
            xml.Element("Project")!
               .Element("ItemGroup")!
               .Add(new XElement("PackageReference", new XAttribute("Include", "coverlet.msbuild"), new XAttribute("Version", msbuildPkgVersion)));
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
            using (var csprojStream = File.OpenRead(csproj))
            {
                xml = XDocument.Load(csprojStream);
            }
            string msbuildPkgVersion = GetPackageVersion("*collector*.nupkg");
            xml.Element("Project")!
               .Element("ItemGroup")!
               .Add(new XElement("PackageReference", new XAttribute("Include", "coverlet.collector"), new XAttribute("Version", msbuildPkgVersion)));
            xml.Save(csproj);
        }

        private protected string AddCollectorRunsettingsFile(string projectPath, string includeFilter = "[coverletsamplelib.integration.template]*DeepThought", bool sourceLink = false)
        {
            string runSettings =
$@"<?xml version=""1.0"" encoding=""utf-8"" ?>
  <RunSettings>
    <DataCollectionRunSettings>  
      <DataCollectors>  
        <DataCollector friendlyName=""XPlat code coverage"" >
           <Configuration>
            <Format>json,cobertura</Format>
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
                    JsonConvert.DeserializeObject<Modules>(File.ReadAllText(coverageFile))
                    .Document("DeepThought.cs")
                    .Class("Coverlet.Integration.Template.DeepThought")
                    .Method("System.Int32 Coverlet.Integration.Template.DeepThought::AnswerToTheUltimateQuestionOfLifeTheUniverseAndEverything()")
                    .AssertLinesCovered((6, 1), (7, 1), (8, 1));
                    coverageChecked = true;
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
            using (var csprojStream = File.OpenRead(project.ProjectFileNamePath))
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
            using var csprojStream = File.OpenRead(ProjectFileNamePath);
            XDocument xml = XDocument.Load(csprojStream);
            return xml.Element("Project")!.Element("PropertyGroup")!.Element("TargetFramework") == null;
        }

        public string[] GetTargetFrameworks()
        {
            using var csprojStream = File.OpenRead(ProjectFileNamePath);
            XDocument xml = XDocument.Load(csprojStream);
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
