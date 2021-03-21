# Coverlet integration with MSBuild

In this mode, Coverlet doesn't require any additional setup other than including the NuGet package in the unit test project. It integrates with the `dotnet test` infrastructure built into the .NET Core CLI and when enabled, will automatically generate coverage results after tests are run.

If a property takes multiple comma-separated values please note that [you will have to add escaped quotes around the string](https://github.com/Microsoft/msbuild/issues/2999#issuecomment-366078677) like this: `/p:Exclude=\"[coverlet.*]*,[*]Coverlet.Core*\"`, `/p:Include=\"[coverlet.*]*,[*]Coverlet.Core*\"`, or `/p:CoverletOutputFormat=\"json,opencover\"`.

## Code Coverage

Enabling code coverage is as simple as setting the `CollectCoverage` property to `true`

```bash
dotnet test /p:CollectCoverage=true
```

After the above command is run, a `coverage.json` file containing the results will be generated in the root directory of the test project. A summary of the results will also be displayed in the terminal.

## Coverage Output

Coverlet can generate coverage results in multiple formats, which is specified using the `CoverletOutputFormat` property. For example, the following command emits coverage results in the `opencover` format:

```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

Supported Formats:

* json (default)
* lcov
* opencover
* cobertura
* teamcity

You can specify multiple output formats by separating them with a comma (`,`).

The output of the coverage result can be specified using the `CoverletOutput` property.

```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutput='./result.json'
```

To specify a directory where all results will be written to (especially if using multiple formats), end the value with a `/`.

```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutput='./results/'
```

The Coverlet MSBuild task sets the `CoverletReport` MSBuild item so that you can easily use the produced coverage reports. For example, using [ReportGenerator](https://github.com/danielpalme/ReportGenerator#usage--command-line-parameters) to generate an html coverage report.

```xml
<Target Name="GenerateHtmlCoverageReport" AfterTargets="GenerateCoverageResultAfterTest">
  <ReportGenerator ReportFiles="@(CoverletReport)" TargetDirectory="../html-coverage-report" />
</Target>
```

### TeamCity Output

Coverlet can output basic code coverage statistics using [TeamCity service messages](https://confluence.jetbrains.com/display/TCD18/Build+Script+Interaction+with+TeamCity#BuildScriptInteractionwithTeamCity-ServiceMessages).

```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=teamcity
```

The currently supported [TeamCity statistics](https://confluence.jetbrains.com/display/TCD18/Build+Script+Interaction+with+TeamCity#BuildScriptInteractionwithTeamCity-ServiceMessages) are:

| TeamCity Statistic Key  | Description                    |
|:------------------------|:-------------------------------|
| CodeCoverageL           | Line-level code coverage       |
| CodeCoverageB           | Branch-level code coverage     |
| CodeCoverageM           | Method-level code coverage     |
| CodeCoverageAbsLTotal   | The total number of lines      |
| CodeCoverageAbsLCovered | The number of covered lines    |
| CodeCoverageAbsBTotal   | The total number of branches   |
| CodeCoverageAbsBCovered | The number of covered branches |
| CodeCoverageAbsMTotal   | The total number of methods    |
| CodeCoverageAbsMCovered | The number of covered methods  |

## Merging Results

With Coverlet, you can combine the output of multiple coverage runs into a single result.

```bash
dotnet test /p:CollectCoverage=true /p:MergeWith='/path/to/result.json'
```

The value given to `/p:MergeWith` **must** be a path to Coverlet's own json result format. The results in `result.json` will be read, and added to the new results written to by Coverlet. [Check the sample](Examples.md).

## Threshold

Coverlet allows you to specify a coverage threshold below which it fails the build. This allows you to enforce a minimum coverage percent on all changes to your project.

```bash
dotnet test /p:CollectCoverage=true /p:Threshold=80
```

The above command will automatically fail the build if the line, branch or method coverage of _any_ of the instrumented modules falls below 80%. You can specify what type of coverage to apply the threshold value to using the `ThresholdType` property. For example to apply the threshold check to only **line** coverage:

```bash
dotnet test /p:CollectCoverage=true /p:Threshold=80 /p:ThresholdType=line
```

You can specify multiple values for `ThresholdType` by separating them with commas. Valid values include `line`, `branch` and `method`. 
You can do the same for `Threshold` as well.

```bash
dotnet test /p:CollectCoverage=true /p:Threshold="80,100,70", /p:ThresholdType="line,branch,method"
```

By default, Coverlet will validate the threshold value against the coverage result of each module. The `/p:ThresholdStat` option allows you to change this behaviour and can have any of the following values:

* Minimum (Default): Ensures the coverage result of each module isn't less than the threshold
* Total: Ensures the total combined coverage result of all modules isn't less than the threshold
* Average: Ensures the average coverage result of all modules isn't less than the threshold

The following command will compare the threshold value with the overall total coverage of all modules:

```bash
dotnet test /p:CollectCoverage=true /p:Threshold=80 /p:ThresholdType=line /p:ThresholdStat=total
```

## Excluding From Coverage

### Attributes

You can ignore a method, an entire class or assembly from code coverage by creating and applying the `ExcludeFromCodeCoverage` attribute present in the `System.Diagnostics.CodeAnalysis` namespace.

You can also ignore additional attributes by using the `ExcludeByAttribute` property (short name, i.e. the type name without the namespace, supported only):

```bash
dotnet test /p:CollectCoverage=true /p:ExcludeByAttribute="Obsolete,GeneratedCodeAttribute,CompilerGeneratedAttribute"
```

### Source Files

You can also ignore specific source files from code coverage using the `ExcludeByFile` property
 - Use single or multiple paths (separate by comma)
 - Use file path or directory path with globbing (e.g `dir1/*.cs`)

```bash
dotnet test /p:CollectCoverage=true /p:ExcludeByFile=\"**/dir1/class1.cs,**/dir2/*.cs,**/dir3/**/*.cs\"
```

### Filters

Coverlet gives the ability to have fine grained control over what gets excluded using "filter expressions".

Syntax: `/p:Exclude=[Assembly-Filter]Type-Filter`

Wildcards
- `*` => matches zero or more characters
- `?` => the prefixed character is optional

Examples
 - `/p:Exclude="[*]*"` => Excludes all types in all assemblies (nothing is instrumented)
 - `/p:Exclude="[coverlet.*]Coverlet.Core.Coverage"` => Excludes the Coverage class in the `Coverlet.Core` namespace belonging to any assembly that matches `coverlet.*` (e.g `coverlet.core`)
 - `/p:Exclude="[*]Coverlet.Core.Instrumentation.*"` => Excludes all types belonging to `Coverlet.Core.Instrumentation` namespace in any assembly
 - `/p:Exclude="[coverlet.*.tests?]*"` => Excludes all types in any assembly starting with `coverlet.` and ending with `.test` or `.tests` (the `?` makes the `s`  optional)
 - `/p:Exclude=\"[coverlet.*]*,[*]Coverlet.Core*\"` => Excludes assemblies matching `coverlet.*` and excludes all types belonging to the `Coverlet.Core` namespace in any assembly

```bash
dotnet test /p:CollectCoverage=true /p:Exclude="[coverlet.*]Coverlet.Core.Coverage"
```

Coverlet goes a step in the other direction by also letting you explicitly set what can be included using the `Include` property.

Examples
 - `/p:Include="[*]*"` => Includes all types in all assemblies (everything is instrumented)
 - `/p:Include="[coverlet.*]Coverlet.Core.Coverage"` => Includes the Coverage class in the `Coverlet.Core` namespace belonging to any assembly that matches `coverlet.*` (e.g `coverlet.core`)
  - `/p:Include="[coverlet.*.tests?]*"` => Includes all types in any assembly starting with `coverlet.` and ending with `.test` or `.tests` (the `?` makes the `s`  optional)

Both `Exclude` and `Include` properties can be used together but `Exclude` takes precedence. You can specify multiple filter expressions by separting them with a comma (`,`).

You can also include coverage of the test assembly itself by setting `/p:IncludeTestAssembly` to `true`.

### Skip auto-implemented properties  

Neither track nor record auto-implemented properties.  
Syntax:  `/p:SkipAutoProps=true`

### Methods that do not return

Methods that do not return can be marked with attributes to cause statements after them to be excluded from coverage.

Attributes can be specified with the following syntax.
Syntax:  `/p:DoesNotReturnAttribute="DoesNotReturnAttribute,OtherAttribute"`

### Note for Powershell / Azure DevOps users

To exclude or include multiple assemblies when using Powershell scripts or creating a .yaml file for an Azure DevOps build ```%2c``` should be used as a separator. Msbuild will translate this symbol to ```,```.

```/p:Exclude="[*]*Examples?%2c[*]*Startup"```

Azure DevOps builds do not require double quotes to be unescaped:

```
dotnet test --configuration $(buildConfiguration) --no-build /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura /p:CoverletOutput=$(Build.SourcesDirectory)/TestResults/Coverage/ /p:Exclude="[MyAppName.DebugHost]*%2c[MyAppNamet.WebHost]*%2c[MyAppName.App]*"
```

### Note for Linux users

[There is an issue with MSBuild on Linux](https://github.com/microsoft/msbuild/issues/3468) that affects the ability to escape quotes while specifying multiple comma-separated values. Linux MSBuild automatically translates `\` to `/` in properties, tasks, etc. before using them, which means if you specified `/p:CoverletOutputFormat=\"json,opencover\"` in an MSBuild script, it will be converted to `/p:CoverletOutputFormat=/"json,opencover/"` before execution. This yields an error similar to the following:

```text
MSBUILD : error MSB1006: Property is not valid. [/home/vsts/work/1/s/default.proj]
  Switch: opencover/
```

You'll see this if directly consuming Linux MSBuild or if using the Azure DevOps `MSBuild` task on a Linux agent.

The workaround is to use the .NET Core `dotnet msbuild` command instead of using MSBuild directly. The issue is not present in `dotnet msbuild` and the script will run with correctly escaped quotes.

## SourceLink

Coverlet supports [SourceLink](https://github.com/dotnet/sourcelink) custom debug information contained in PDBs. When you specify the `/p:UseSourceLink=true` property, Coverlet will generate results that contain the URL to the source files in your source control instead of local file paths.
