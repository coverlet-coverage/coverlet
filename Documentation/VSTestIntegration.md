# Coverlet integration with VSTest (a.k.a. Visual Studio Test Platform)

As explained in quick start section to use collectors you need to run *SDK v2.2.401* or newer and your project file must reference `coverlet.collector.dll` and a minimum version of `Microsoft.NET.Test.Sdk`.  
A sample project file looks like:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFrameworks>netcoreapp3.0;netcoreapp2.1;net46</TargetFrameworks>
  </PropertyGroup>
  <ItemGroup>
    <!-- Temporary preview reference with essential vstest bug fix -->
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="16.5.0-preview-20200116-01" />
    <!-- Update this reference when new version is released -->
    <PackageReference Include="coverlet.collector" Version="1.2.0">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
  ...
  </ItemGroup>
...
</Project>
```
As you can see in sample above we're referencing a preview version of `Microsoft.NET.Test.Sdk`, this is temporary needed because there is a bug inside vstest platform during collectors loading([details](https://github.com/microsoft/vstest/issues/2205)). **At the moment there isn't a stable package released with fix so we need to use a preview**.

The reference to `coverlet.collector` package is included by default with xunit template test (`dotnet new xunit`), you only need to update the package for new versions like any other package reference.

With correct reference in place you can run coverage through default dotnet test CLI verbs:

```
dotnet test --collect:"XPlat Code Coverage"
```
or
```
dotnet publish
...
  ... -> C:\project\bin\Debug\netcoreapp3.0\testdll.dll
  ... -> C:\project\bin\Debug\netcoreapp3.0\publish\
...
dotnet vstest C:\project\bin\Debug\netcoreapp3.0\publish\testdll.dll --collect:"XPlat Code Coverage"
```
As you can see in case of `vstest` verb you **must** publish project before.

## Coverlet options supported by VSTest integration

At the moment VSTest integration doesn't support all features of msbuild and .NET tool, for instance show result on console, report merging and threshold validation.
We're working to fill the gaps.


#### Default
| Option | Summary |
|-------------|------------------------------------|
|Format              | Results format in which coverage output is generated. Default format is cobertura.| 

#### Advanced Options (Supported via runsettings)
These are a list of options that are supported by coverlet. These can be specified as datacollector configurations in the runsettings.

| Option         | Summary                                                                                  |
|-------------   |------------------------------------------------------------------------------------------|
|Format          | Coverage output format. These are either cobertura, json, lcov, opencover or teamcity as well as combinations of these formats.   | 
|Exclude         | Exclude from code coverage analysing using filter expressions.                           | 
|ExcludeByFile   | Ignore specific source files from code coverage.                                         | 
|Include         | Explicitly set what to include in code coverage analysis using filter expressions.       | 
|IncludeDirectory| Explicitly set which directories to include in code coverage analysis.                   |
|SingleHit       | Specifies whether to limit code coverage hit reporting to a single hit for each location.| 
|UseSourceLink   | Specifies whether to use SourceLink URIs in place of file system paths.                  |
|IncludeTestAssembly    | Include coverage of the test assembly.                  |

How to specify these options via runsettings?
```
<?xml version="1.0" encoding="utf-8" ?>
<RunSettings>
  <DataCollectionRunSettings>
    <DataCollectors>
      <DataCollector friendlyName="XPlat code coverage">
        <Configuration>
          <Format>json,cobertura</Format>          
          <Exclude>[coverlet.*.tests?]*,[*]Coverlet.Core*</Exclude> <!-- [Assembly-Filter]Type-Filter -->
          <Include>[coverlet.*]*,[*]Coverlet.Core*</Include> <!-- [Assembly-Filter]Type-Filter -->
          <ExcludeByAttribute>Obsolete,GeneratedCodeAttribute,CompilerGeneratedAttribute</ExcludeByAttribute>
          <ExcludeByFile>../dir1/class1.cs,../dir2/*.cs,../dir3/**/*.cs,</ExcludeByFile> <!-- Absolute or relative file paths -->
          <IncludeDirectory>../dir1/,../dir2/,</IncludeDirectory>
          <SingleHit>false</SingleHit>
          <UseSourceLink>true</UseSourceLink>
          <IncludeTestAssembly>true</IncludeTestAssembly>
        </Configuration>
      </DataCollector>
    </DataCollectors>
  </DataCollectionRunSettings>
</RunSettings>
```
This runsettings file can easily be provided using command line option as given :

1. `dotnet test --collect:"XPlat Code Coverage" --settings coverlet.runsettings`

2. `dotnet vstest C:\project\bin\Debug\netcoreapp3.0\publish\testdll.dll --collect:"XPlat Code Coverage" --settings coverlet.runsettings`

Take a look at our [`HelloWorld`](Examples/VSTest/HelloWorld/HowTo.md) sample.

## How it works

Coverlet integration is implemented with the help of [datacollectors](https://github.com/Microsoft/vstest-docs/blob/master/docs/extensions/datacollector.md).  
When we specify `--collect:"XPlat Code Coverage"` vstest platform tries to load coverlet collectors present inside `coverlet.collector.dll`

1. Outproc Datacollector : The outproc collector run in a separate process(datacollector.exe/datacollector.dll) than the process in which tests are being executed(testhost*.exe/testhost.dll). This datacollector is responsible for calling into coverlet APIs for instrumenting dlls, collecting coverage results and sending the coverage output file back to test platform.

2. Inproc Datacollector : The inproc collector is loaded in the testhost process executing the tests. This collector will be needed to remove the dependency on the process exit handler to flush the hit files and avoid to hit this [serious know issue](https://github.com/tonerdo/coverlet/blob/master/Documentation/KnowIssues.md#1-vstest-stops-process-execution-earlydotnet-test)

## Known Issues

For a comprehensive list of Known issues check the detailed documentation https://github.com/tonerdo/coverlet/blob/master/Documentation/KnowIssues.md
