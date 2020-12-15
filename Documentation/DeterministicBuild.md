# Deterministic build

Support for deterministic builds is available **only** in the `msbuild` (`/p:CollectCoverage=true`) and `collectors` (`--collect:"XPlat Code Coverage"`) drivers. Deterministic builds are important because they enable verification that the resulting binary was built from the specified sources. For more information on how to enable deterministic builds, I recommend you to take a look at [Claire Novotny](https://github.com/clairernovotny)'s [guide](https://github.com/clairernovotny/DeterministicBuilds).

From a coverage perspective, deterministic builds create some challenges because coverage tools usually need access to complete source file metadata (ie. local path) during instrumentation and report generation. These files are reported inside the `.pdb` files, where debugging information is stored.

In local (non-CI) builds, metadata emitted to pdbs are not "deterministic", which means that source files are reported with their full paths. For example, when we build the same project on different machines we'll have different paths emitted inside pdbs, hence, builds are "non deterministic".  

As explained above, to improve the level of security of generated artifacts (for instance, DLLs inside the NuGet package), we need to apply some signature (signing with certificate) and validate before usage to avoid possible security issues like tampering.

Finally, thanks to deterministic CI builds (with the `ContinuousIntegrationBuild` property set to `true`) plus signature we can validate artifacts and be sure that the binary was built from specific sources (because there is no hard-coded variable metadata, like paths from different build machines).

**Deterministic build is supported without any workaround since version 3.1.100 of .NET Core SDK**

## Workaround only for .NET Core SDK < 3.1.100

At the moment, deterministic build works thanks to the Roslyn compiler emitting deterministic metadata if `DeterministicSourcePaths` is enabled. Take a look [here](https://github.com/dotnet/sourcelink/tree/master/docs#deterministicsourcepaths) for more information.

To allow Coverlet to correctly do its work, we need to provide information to translate deterministic paths to real local paths for every project referenced by the test project. The current workaround is to add at the root of your repo a `Directory.Build.targets` with a custom `target` that supports Coverlet resolution algorithm.

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

If you already have a `Directory.Build.targets` file on your repo root you can simply copy `DeterministicBuild.targets` (which can be found at the root of this repo) next to yours and import it in your targets file. This target will be used by Coverlet to generate, at build time, a file that contains mapping translation information, the file is named `CoverletSourceRootsMapping` and will be in the output folder of your project.

You can follow our [step-by-step sample](Examples.md)

Feel free to file an issue in case you run into any trouble!
