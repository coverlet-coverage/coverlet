# Coverlet Integration with VSTest

## Motivation

The cross platform solution for Code Coverage generation for .NET Core projects (in a consistent manner).
The asks for Code Coverage support for .NET Core on Linux is the most commented issue on vstest repo:
https://github.com/Microsoft/vstest/issues/981#issuecomment-320311552

## Summary

What would integrating Coverlet with Microsoft Test Platform mean:

1. Coverlet based coverage solution is available as a data collector that does the instrumentation of the necessary modules before test execution and appropriate restore after.
2. Authoring - When new test projects are created (dotnet mstest), references to the data collector package are added by default. This reduces adoption friction for customers.
3. Test execution - Today Coverlet is invoked as a msbuild target and as such only works with dotnet test (and requires a csproj). With the data collector becoming available, coverage can also be collected when tests are run on built binaries (dotnet vstest)

## Proposed Solution

### Scenarios to support

The following table summarizes the support that needs to be added for a seamless code coverage collection for .NET Core on both Linux and Windows platforms:

| Entry point | How will code coverage be enabled? | Syntax                                                               |
|-------------|------------------------------------|----------------------------------------------------------------------|
|dotnet test CLI              | Through a switch to condition data collection | `dotnet test --collect:"XPlat Code Coverage"`   |
|dotnet vstest CLI            | Through a switch to condition data collection | `dotnet vstest --collect:"XPlat Code Coverage"` |

### Coverlet Options Supported with VSTest

#### Default
| Option | Summary |
|-------------|------------------------------------|
|Format              | Results format in which coverage output is generated. Default format is cobertura.| 

#### Advanced Options (Supported via runsettings)
These are a list of options that are supported by coverlet. These can be specified as datacollector configurations in the runsettings.

| Option         | Summary                                                                                  |
|-------------   |------------------------------------------------------------------------------------------|
|Format          | Coverage output format. These are either cobertura, json, lcov, opencover or teamcity.   | 
|MergeWith       | Combine the output of multiple coverage runs into a single result.                       | 
|Exclude         | Exclude from code coverage analysing using filter expressions.                           | 
|ExcludeByFile   | Ignore specific source files from code coverage.                                         | 
|Include         | Explicitly set what to include in code coverage analysis using filter expressions.       | 
|IncludeDirectory| Explicitly set which directories to include in code coverage analysis.                   |
|SingleHit       | Specifies whether to limit code coverage hit reporting to a single hit for each location.| 
|UseSourceLink   | Specifies whether to use SourceLink URIs in place of file system paths.                  |

How to specify these options via runsettings?
```
<?xml version="1.0" encoding="utf-8" ?>
<RunSettings>
  <DataCollectionRunSettings>
    <DataCollectors>
      <DataCollector friendlyName="XPlat code coverage">
        <Configuration>
          <Format>json</Format>
          <MergeWith>/custom/path/result.json</MergeWith>
          <Exclude>[coverlet.*.tests?]*,[*]Coverlet.Core*</Exclude> <!-- [Assembly-Filter]Type-Filter -->
          <Include>[coverlet.*]*,[*]Coverlet.Core*</Include> <!-- [Assembly-Filter]Type-Filter -->
          <ExcludeByAttribute>Obsolete,GeneratedCodeAttribute,CompilerGeneratedAttribute</ExcludeByAttribute>
          <ExcludeByFile>../dir1/class1.cs,../dir2/*.cs,../dir3/**/*.cs,</ExcludeByFile> <!-- Absolute or relative file paths -->
          <IncludeDirectory>../dir1/,../dir2/,</IncludeDirectory>
          <SingleHit>false</SingleHit>
          <UseSourceLink>true</UseSourceLink>
        </Configuration>
      </DataCollector>
    </DataCollectors>
  </DataCollectionRunSettings>
</RunSettings>
```
This runsettings file can easily be provided using command line option as given :

1. `dotnet test --settings coverletArgs.runsettings`

2. `dotnet vstest --settings coverletArgs.runsettings`


#### Scope of Enhancement

Currently, advanced options are supported via runsettings. Providing support through additional command line arguments in vstest can be taken up separately.

## Implementation Details

The proposed solution is implemented with the help of [datacollectors](https://github.com/Microsoft/vstest-docs/blob/master/docs/extensions/datacollector.md).

1. Outproc Datacollector : The outproc collector would always run in a separate process(datacollector.exe/datacollector.dll) than the process in which tests are being executed(testhost*.exe/testhost.dll). This datacollector would be responsible for calling into coverlet APIs for instrumenting dlls, collecting coverage results and sending the coverage output file back to test platform.

2. Inproc Datacollector : The inproc collector in the testhost process executing the tests. This collector will be needed to remove the dependency on the exit handler to flush the hit files.

The datacollectors will be bundled as a separate NuGet package, the reference to which will be added by default in the .NET Core test templates, thus making it the default solution for collecting code coverage for .NET core projects.
```
<ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="x.x.x" />
    <PackageReference Include="MSTest.TestAdapter" Version="x.x.x" />
    <PackageReference Include="MSTest.TestFramework" Version="x.x.x" />
    <PackageReference Include="coverlet.collector" Version="1.0.0" />
</ItemGroup>
```
