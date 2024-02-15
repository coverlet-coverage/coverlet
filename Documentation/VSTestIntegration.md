# Coverlet integration with VSTest (a.k.a. Visual Studio Test Platform)

**Supported runtime versions**:

Since version `6.0.0`

* .NET Core >= 6.0
* .NET Framework >= 4.6.2

As explained in quick start section, to use collectors you need to run *SDK v6.0.100* (LTS) or newer and your project file must reference `coverlet.collector` and a minimum version of `Microsoft.NET.Test.Sdk`.

A sample project file looks like:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFrameworks>net6.0;net48</TargetFrameworks>
  </PropertyGroup>
  <ItemGroup>
    <!-- Minimum version 17.7.0 -->
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
    <!-- Update this reference when new version is released -->
    <PackageReference Include="coverlet.collector" Version="6.0.0">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
  ...
  </ItemGroup>
...
</Project>
```

The reference to `coverlet.collector` package is included by default with xunit template test (`dotnet new xunit`), you only need to update the package for new versions like any other package reference.

With correct reference in place you can run coverage through default dotnet test CLI verbs:

```shell
dotnet test --collect:"XPlat Code Coverage"
```

or

```text
dotnet publish
...
  ... -> C:\project\bin\Debug\netcoreapp3.0\testdll.dll
  ... -> C:\project\bin\Debug\netcoreapp3.0\publish\
...
dotnet vstest C:\project\bin\Debug\netcoreapp3.0\publish\testdll.dll --collect:"XPlat Code Coverage"
```

As you can see in case of `vstest` verb you **must** publish project before.

At the end of tests you'll find the coverage file data under default VSTest platform directory `TestResults`

```text
Attachments:
  C:\git\coverlet\Documentation\Examples\VSTest\HelloWorld\XUnitTestProject1\TestResults\bc5e983b-d7a8-4f17-8c0a-8a8831a4a891\coverage.cobertura.xml
Test Run Successful.
Total tests: 1
     Passed: 1
 Total time: 2,5451 Seconds
```

You can change the output directory using the standard `dotnet test` switch `--results-directory`

>*NB: By design VSTest platform will create your file under a random named folder(guid string) so if you need stable path to load file to some gui report system(i.e. coveralls, codecov, reportgenerator etc..) that doesn't support glob patterns or hierarchical  search, you'll need to manually move resulting file to a predictable folder*

## Coverlet options supported by VSTest integration

:warning:At the moment VSTest integration **doesn't support all features** of msbuild and .NET tool, for instance show result on console, report merging and threshold validation.
We're working to fill the gaps.
>*PS: if you don't have any other way to merge reports(for instance your report generator doesn't support multi coverage file) you can for the moment exploit a trick reported by one of our contributor Daniel Paz(@p4p3) <https://github.com/tonerdo/coverlet/pull/225#issuecomment-573896446>*

### Default option (if you don't specify a runsettings file)

Without specifying a runsettings file and calling coverlet by just the name of the collector, the result of the generated coverage output is by default in cobertura format.

```shell
dotnet test --collect:"XPlat Code Coverage"
```

The output format of the coverage report can also be changed without a runsettings file by specifying it in a parameter. The supported formats are lcov, opencover, cobertura, teamcity, json (default coverlet proprietary format).

```shell
dotnet test --collect:"XPlat Code Coverage;Format=json"
```

It is even possible to specify the coverage output in multiple formats.

```shell
dotnet test --collect:"XPlat Code Coverage;Format=json,lcov,cobertura"
```

### Advanced Options (Supported via runsettings)

These are a list of options that are supported by coverlet. These can be specified as datacollector configurations in the runsettings.

| Option                   | Summary                                                                                                                                                         |
|:-------------------------|:----------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Format                   | Coverage output format. These are either cobertura, json, lcov, opencover or teamcity as well as combinations of these formats.                                 |
| Exclude                  | Exclude from code coverage analysing using filter expressions.                                                                                                  |
| ExcludeByAttribute       | Exclude a method, an entire class or assembly from code coverage decorated by an attribute.    |
| ExcludeByFile            | Ignore specific source files from code coverage.                                                                                                                |
| Include                  | Explicitly set what to include in code coverage analysis using filter expressions.                                                                              |
| IncludeDirectory         | Explicitly set which directories to include in code coverage analysis.                                                                                          |
| SingleHit                | Specifies whether to limit code coverage hit reporting to a single hit for each location.                                                                       |
| UseSourceLink            | Specifies whether to use SourceLink URIs in place of file system paths.                                                                                         |
| IncludeTestAssembly      | Include coverage of the test assembly.                                                                                                                          |
| SkipAutoProps            | Neither track nor record auto-implemented properties.                                                                                                           |
| DoesNotReturnAttribute   | Methods marked with these attributes are known not to return, statements following them will be excluded from coverage                                          |
| DeterministicReport      | Generates deterministic report in context of deterministic build. Take a look at [documentation](DeterministicBuild.md) for further informations.
| ExcludeAssembliesWithoutSources |  Specifies whether to exclude assemblies without source. Options are either MissingAll, MissingAny or None. Default is MissingAll.|

How to specify these options via runsettings?

```xml
<?xml version="1.0" encoding="utf-8" ?>
<RunSettings>
  <DataCollectionRunSettings>
    <DataCollectors>
      <DataCollector friendlyName="XPlat code coverage">
        <Configuration>
          <Format>json,cobertura,lcov,teamcity,opencover</Format>
          <Exclude>[coverlet.*.tests?]*,[*]Coverlet.Core*</Exclude> <!-- [Assembly-Filter]Type-Filter -->
          <Include>[coverlet.*]*,[*]Coverlet.Core*</Include> <!-- [Assembly-Filter]Type-Filter -->
          <ExcludeByAttribute>Obsolete,GeneratedCodeAttribute,CompilerGeneratedAttribute</ExcludeByAttribute>
          <ExcludeByFile>**/dir1/class1.cs,**/dir2/*.cs,**/dir3/**/*.cs,</ExcludeByFile> <!-- Globbing filter -->
          <IncludeDirectory>../dir1/,../dir2/,</IncludeDirectory>
          <SingleHit>false</SingleHit>
          <UseSourceLink>true</UseSourceLink>
          <IncludeTestAssembly>true</IncludeTestAssembly>
          <SkipAutoProps>true</SkipAutoProps>
          <DeterministicReport>false</DeterministicReport>
          <ExcludeAssembliesWithoutSources>MissingAll,MissingAny,None</ExcludeAssembliesWithoutSources>
        </Configuration>
      </DataCollector>
    </DataCollectors>
  </DataCollectionRunSettings>
</RunSettings>
```

Filtering details are present on [msbuild guide](MSBuildIntegration.md#excluding-from-coverage).

This runsettings file can easily be provided using command line option as given :

* `dotnet test --collect:"XPlat Code Coverage" --settings coverlet.runsettings`
* `dotnet vstest C:\project\bin\Debug\netcoreapp3.0\publish\testdll.dll --collect:"XPlat Code Coverage" --settings coverlet.runsettings`

Take a look at our [`HelloWorld`](Examples/VSTest/HelloWorld/HowTo.md) sample.

### Passing runsettings arguments through commandline

You can avoid passing a `runsettings` file to `dotnet test` driver by using the xml flat syntax in the command line.

For instance if you want to set the `Format` element as a runsettings option you can use this syntax:

```shell
dotnet test --collect:"XPlat Code Coverage" -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=json,cobertura,lcov,teamcity,opencover
```

Take a look here for further information:<https://github.com/microsoft/vstest/blob/main/docs/RunSettingsArguments.md>

## How it works

Coverlet integration is implemented with the help of [datacollectors](https://github.com/microsoft/vstest/blob/main/docs/extensions/datacollector.md).
When we specify `--collect:"XPlat Code Coverage"` VSTest platform tries to load coverlet collectors inside `coverlet.collector.dll`

1. Out-of-proc Datacollector: The outproc collector run in a separate process(datacollector.exe/datacollector.dll) than the process in which tests are being executed(testhost*.exe/testhost.dll). This datacollector is responsible for calling into Coverlet APIs for instrumenting dlls, collecting coverage results and sending the coverage output file back to test platform.

1. In-proc Datacollector: The in-proc collector is loaded in the testhost process executing the tests. This collector will be needed to remove the dependency on the process exit handler to flush the hit files and avoid to hit this [serious known issue](KnownIssues.md#1-vstest-stops-process-execution-earlydotnet-test)

## Known Issues

For a comprehensive list of known issues check the detailed documentation [KnownIssues.md](KnownIssues.md)
