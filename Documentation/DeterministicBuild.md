# Deterministic build

Deterministic build **is supported only** by `msbuild`(`/p:CollectCoverage=true`) and `collectors`(`--collect:"XPlat Code Coverage"`) drivers.

Deterministic builds are important as they enable verification that the resulting binary was built from the specified source and provides traceability.  
For more information on how to enable deterministic build I recommend you to take a look at [@clairernovotny](https://github.com/clairernovotny)'s guide https://github.com/clairernovotny/DeterministicBuilds

From coverage perspective deterministic build put some challenge because usually coverage tools need access to source file metadata (ie. local path) during instrumentation and report generation.  
These files are reported inside `pdb` files, where are stored debugging information needed by debuggers and also by tools that needs to work with "build" metadata information, for example generated `dll` modules and `source files` used.  
In local (non-CI) builds metadata emitted to pdbs are not "deterministic", that means that for instance source files path are reported with full path.  
If for instance we build same project on different machine we'll have different paths emitted inside pdbs, so builds are "non deterministic" because same project build won't generates same artifacts.  

As explained above, to improve the level of security of generated artifacts (suppose for instance DLLs inside the nuget package), we need to apply some signature (signing with certificate) and validate before usage to avoid possible security issues like tampering.  
Finally thanks to deterministic CI builds (with the `ContinuousIntegrationBuild` property set to `true`) plus signature we can validate artifacts and be sure that binary was build from a specific sources (because there is no hard-coded variables metadata like paths from different build machines).

At the moment deterministic build works thanks to Roslyn compiler that emits deterministic metadata if `DeterministicSourcePaths` is enabled. Take a look here for more information https://github.com/dotnet/sourcelink/tree/master/docs#deterministicsourcepaths.  
To allow coverlet to correctly do his work we need to provide information to translate deterministic path to real local path for every project referenced by tests project.  
The current workaround is to add on top of your repo a `Directory.Build.targets` with inside a simple snippet with custom `target` that supports coverlet resolution algorithm.
```xml
<!-- This target must be imported into Directory.Build.targets -->
<!-- Workaround. Remove once we're on 3.1.300+
https://github.com/dotnet/sourcelink/issues/572 -->
<Project>
  <PropertyGroup>
    <TargetFrameworkMonikerAssemblyAttributesPath>$([System.IO.Path]::Combine('$(IntermediateOutputPath)','$(TargetFrameworkMoniker).AssemblyAttributes$(DefaultLanguageSourceExtension)'))</TargetFrameworkMonikerAssemblyAttributesPath>
  </PropertyGroup>
  <ItemGroup>
    <EmbeddedFiles Include="$(GeneratedAssemblyInfoFile)"/>
  </ItemGroup>
  <ItemGroup>
    <SourceRoot Include="$(NuGetPackageRoot)" />
  </ItemGroup>

  <Target Name="CoverletGetPathMap"
          DependsOnTargets="InitializeSourceRootMappedPaths"
          Returns="@(_LocalTopLevelSourceRoot)"
          Condition="'$(DeterministicSourcePaths)' == 'true'">
    <ItemGroup>
      <_LocalTopLevelSourceRoot Include="@(SourceRoot)" Condition="'%(SourceRoot.NestedRoot)' == ''"/>
    </ItemGroup>
  </Target>
</Project>

```
If you already have a `Directory.Build.targets` file on your repo root you can simply copy `DeterministicBuild.targets` you can find on coverlet repo root next to yours and import it in your targets file.  
This target will be used by coverlet to generate on build phase a file on output folder with mapping translation informations, the file is named `CoverletSourceRootsMapping`.

You can follow our [step-by-step sample](Examples.md)

Feel free to file an issue in case of troubles!
