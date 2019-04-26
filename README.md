# coverlet [![Build status](https://ci.appveyor.com/api/projects/status/6rdf00wufospr4r8/branch/master?svg=true)](https://ci.appveyor.com/project/tonerdo/coverlet) [![codecov](https://codecov.io/gh/tonerdo/coverlet/branch/master/graph/badge.svg)](https://codecov.io/gh/tonerdo/coverlet) [![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE) [![NuGet](https://img.shields.io/nuget/v/coverlet.msbuild.svg)](https://www.nuget.org/packages/coverlet.msbuild)

Coverlet is a cross platform code coverage library for .NET Core, with support for line, branch and method coverage.

## Installation

**Global Tool**:

```bash
dotnet tool install --global coverlet.console
```

**Package Reference**:

```bash
dotnet add package coverlet.msbuild
```

## How It Works

Coverlet generates code coverage information by going through the following process:

### Before Tests Run

* Locates the unit test assembly and selects all the referenced assemblies that have PDBs.
* Instruments the selected assemblies by inserting code to record sequence point hits to a temporary file.

### After Tests Run

* Restore the original non-instrumented assembly files.
* Read the recorded hits information from the temporary file.
* Generate the coverage result from the hits information and write it to a file.

_Note: The assembly you'd like to get coverage for must be different from the assembly that contains the tests_

## Usage

Coverlet can be used either as a .NET Core global tool that can be invoked from a terminal or as a NuGet package that integrates with the MSBuild system of your test project.

### Global Tool

To see a list of options, run:

```bash
coverlet --help
```

The current options are (output of `coverlet --help`):

```bash
Cross platform .NET Core code coverage tool 1.0.0.0

Usage: coverlet [arguments] [options]

Arguments:
  <ASSEMBLY>  Path to the test assembly.

Options:
  -h|--help               Show help information
  -v|--version            Show version information
  -t|--target             Path to the test runner application.
  -a|--targetargs         Arguments to be passed to the test runner.
  -o|--output             Output of the generated coverage report
  -v|--verbosity          Sets the verbosity level of the command. Allowed values are quiet, minimal, normal, detailed.
  -f|--format             Format of the generated coverage report.
  --threshold             Exits with error if the coverage % is below value.
  --threshold-type        Coverage type to apply the threshold to.
  --threshold-stat        Coverage statistic used to enforce the threshold value.
  --exclude               Filter expressions to exclude specific modules and types.
  --include               Filter expressions to include specific modules and types.
  --include-directory     Include directories containing additional assemblies to be instrumented.
  --exclude-by-file       Glob patterns specifying source files to exclude.
  --exclude-by-attribute  Attributes to exclude from code coverage.
  --merge-with            Path to existing coverage result to merge.
  --use-source-link       Specifies whether to use SourceLink URIs in place of file system paths.
  --single-hit            Specifies whether to limit code coverage hit reporting to a single hit for each location.
```

#### Code Coverage

The `coverlet` tool is invoked by specifying the path to the assembly that contains the unit tests. You also need to specify the test runner and the arguments to pass to the test runner using the `--target` and `--targetargs` options respectively. The invocation of the test runner with the supplied arguments **must not** involve a recompilation of the unit test assembly or no coverage data will be generated.

The following example shows how to use the familiar `dotnet test` toolchain:

```bash
coverlet /path/to/test-assembly.dll --target "dotnet" --targetargs "test /path/to/test-project --no-build"
```

After the above command is run, a `coverage.json` file containing the results will be generated in the directory the `coverlet` command was run. A summary of the results will also be displayed in the terminal.

_Note: The `--no-build` flag is specified so that the `/path/to/test-assembly.dll` isn't rebuilt_

#### Coverage Output

Coverlet can generate coverage results in multiple formats, which is specified using the `--format` or `-f` options. For example, the following command emits coverage results in the `opencover` format instead of `json`:

```bash
coverlet <ASSEMBLY> --target <TARGET> --targetargs <TARGETARGS> --format opencover
```

Supported Formats:

* json (default)
* lcov
* opencover
* cobertura
* teamcity

The `--format` option can be specified multiple times to output multiple formats in a single run:

```bash
coverlet <ASSEMBLY> --target <TARGET> --targetargs <TARGETARGS> --format opencover --format lcov
```

By default, Coverlet will output the coverage results file(s) in the current working directory. The `--output` or `-o` options can be used to override this behaviour.

```bash
coverlet <ASSEMBLY> --target <TARGET> --targetargs <TARGETARGS> --output "/custom/path/result.json"
```

The above command will write the results to the supplied path, if no file extension is specified it'll use the standard extension of the selected output format. To specify a directory instead, simply append a `/` to the end of the value.

```bash
coverlet <ASSEMBLY> --target <TARGET> --targetargs <TARGETARGS> --output "/custom/directory/" -f json -f lcov
```

#### TeamCity Output

Coverlet can output basic code coverage statistics using [TeamCity service messages](https://confluence.jetbrains.com/display/TCD18/Build+Script+Interaction+with+TeamCity#BuildScriptInteractionwithTeamCity-ServiceMessages).

```bash
coverlet <ASSEMBLY> --target <TARGET> --targetargs <TARGETARGS> --output teamcity
```

The currently supported [TeamCity statistics](https://confluence.jetbrains.com/display/TCD18/Build+Script+Interaction+with+TeamCity#BuildScriptInteractionwithTeamCity-ServiceMessages) are:

| TeamCity Statistic Key  | Description                    |
| :---                    | :---                           |
| CodeCoverageL           | Line-level code coverage       |
| CodeCoverageR           | Branch-level code coverage     |
| CodeCoverageM           | Method-level code coverage     |
| CodeCoverageAbsLTotal   | The total number of lines      |
| CodeCoverageAbsLCovered | The number of covered lines    |
| CodeCoverageAbsRTotal   | The total number of branches   |
| CodeCoverageAbsRCovered | The number of covered branches |
| CodeCoverageAbsMTotal   | The total number of methods    |
| CodeCoverageAbsMCovered | The number of covered methods  |

#### Merging Results

With Coverlet you can combine the output of multiple coverage runs into a single result.

```bash
coverlet <ASSEMBLY> --target <TARGET> --targetargs <TARGETARGS> --merge-with "/path/to/result.json" --format opencover
```

The value given to `--merge-with` **must** be a path to Coverlet's own json result format.

#### Threshold

Coverlet allows you to specify a coverage threshold below which it returns a non-zero exit code. This allows you to enforce a minimum coverage percent on all changes to your project.

```bash
coverlet <ASSEMBLY> --target <TARGET> --targetargs <TARGETARGS> --threshold 80
```

The above command will automatically fail the build if the line, branch or method coverage of _any_ of the instrumented modules falls below 80%. You can specify what type of coverage to apply the threshold value to using the `--threshold-type` option. For example to apply the threshold check to only **line** coverage:

```bash
coverlet <ASSEMBLY> --target <TARGET> --targetargs <TARGETARGS> --threshold 80 --threshold-type line
```

You can specify the `--threshold-type` option multiple times. Valid values include `line`, `branch` and `method`.

```bash
coverlet <ASSEMBLY> --target <TARGET> --targetargs <TARGETARGS> --threshold 80 --threshold-type line --threshold-type method
```

By default, Coverlet will validate the threshold value against the coverage result of each module. The `--threshold-stat` option allows you to change this behaviour and can have any of the following values:

* Minimum (Default): Ensures the coverage result of each module isn't less than the threshold
* Total: Ensures the total combined coverage result of all modules isn't less than the threshold
* Average: Ensures the average coverage result of all modules isn't less than the threshold

The following command will compare the threshold value with the overall total coverage of all modules:

```bash
coverlet <ASSEMBLY> --target <TARGET> --targetargs <TARGETARGS> --threshold 80 --threshold-type line --threshold-stat total
```

#### Excluding From Coverage

##### Attributes

You can ignore a method or an entire class from code coverage by creating and applying the `ExcludeFromCodeCoverage` attribute present in the `System.Diagnostics.CodeAnalysis` namespace.

You can also ignore additional attributes by using the `ExcludeByAttribute` property (short name or full name supported):

```bash
coverlet <ASSEMBLY> --target <TARGET> --targetargs <TARGETARGS> --exclude-by-attribute "Obsolete,GeneratedCodeAttribute,CompilerGeneratedAttribute"
```

##### Source Files

You can also ignore specific source files from code coverage using the `--exclude-by-file` option
 - Can be specified multiple times
 - Use absolute or relative paths (relative to the project directory)
 - Use file path or directory path with globbing (e.g `dir1/*.cs`)

```bash
coverlet <ASSEMBLY> --target <TARGET> --targetargs <TARGETARGS> --exclude-by-file "../dir1/class1.cs"
```

##### Filters

Coverlet gives the ability to have fine grained control over what gets excluded using "filter expressions".

Syntax: `--exclude '[Assembly-Filter]Type-Filter'`

Wildcards
- `*` => matches zero or more characters
- `?` => the prefixed character is optional

Examples
 - `--exclude "[*]*"` => Excludes all types in all assemblies (nothing is instrumented)
 - `--exclude "[coverlet.*]Coverlet.Core.Coverage"` => Excludes the Coverage class in the `Coverlet.Core` namespace belonging to any assembly that matches `coverlet.*` (e.g `coverlet.core`)
 - `--exclude "[*]Coverlet.Core.Instrumentation.*"` => Excludes all types belonging to `Coverlet.Core.Instrumentation` namespace in any assembly
 - `--exclude "[coverlet.*.tests?]*"` => Excludes all types in any assembly starting with `coverlet.` and ending with `.test` or `.tests` (the `?` makes the `s`  optional)
 - `--exclude "[coverlet.*]*" --exclude "[*]Coverlet.Core*"` => Excludes assemblies matching `coverlet.*` and excludes all types belonging to the `Coverlet.Core` namespace in any assembly

```bash
coverlet <ASSEMBLY> --target <TARGET> --targetargs <TARGETARGS> --exclude "[coverlet.*]Coverlet.Core.Coverage"
```

Coverlet goes a step in the other direction by also letting you explicitly set what can be included using the `--include` option.

Examples
 - `--include "[*]*"` => Includes all types in all assemblies (everything is instrumented)
 - `--include "[coverlet.*]Coverlet.Core.Coverage"` => Includes the Coverage class in the `Coverlet.Core` namespace belonging to any assembly that matches `coverlet.*` (e.g `coverlet.core`)
  - `--include "[coverlet.*.tests?]*"` => Includes all types in any assembly starting with `coverlet.` and ending with `.test` or `.tests` (the `?` makes the `s`  optional)

Both `--exclude` and `--include` options can be used together but `--exclude` takes precedence. You can specify the `--exclude` and `--include` options multiple times to allow for multiple filter expressions.

### MSBuild

In this mode, Coverlet doesn't require any additional setup other than including the NuGet package in the unit test project. It integrates with the `dotnet test` infrastructure built into the .NET Core CLI and when enabled, will automatically generate coverage results after tests are run.

If a property takes multiple comma-separated values please note that [you will have to add escaped quotes around the string](https://github.com/Microsoft/msbuild/issues/2999#issuecomment-366078677) like this: `/p:Exclude=\"[coverlet.*]*,[*]Coverlet.Core*\"`, `/p:Include=\"[coverlet.*]*,[*]Coverlet.Core*\"`, or `/p:CoverletOutputFormat=\"json,opencover\"`.

##### Note for Powershell / VSTS users
To exclude or include multiple assemblies when using Powershell scripts or creating a .yaml file for a VSTS build ```%2c``` should be used as a separator. Msbuild will translate this symbol to ```,```.

```/p:Exclude="[*]*Examples?%2c[*]*Startup"```

VSTS builds do not require double quotes to be unescaped:
```
dotnet test --configuration $(buildConfiguration) --no-build /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura /p:CoverletOutput=$(Build.SourcesDirectory)/TestResults/Coverage/ /p:Exclude="[MyAppName.DebugHost]*%2c[MyAppNamet.WebHost]*%2c[MyAppName.App]*"
```

#### Code Coverage

Enabling code coverage is as simple as setting the `CollectCoverage` property to `true`

```bash
dotnet test /p:CollectCoverage=true
```

After the above command is run, a `coverage.json` file containing the results will be generated in the root directory of the test project. A summary of the results will also be displayed in the terminal.

#### Coverage Output

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

#### Merging Results

With Coverlet you can combine the output of multiple coverage runs into a single result.

```bash
dotnet test /p:CollectCoverage=true /p:MergeWith='/path/to/result.json'
```

The value given to `/p:MergeWith` **must** be a path to Coverlet's own json result format. The results in `result.json` will be read, and added to the new results written to by Coverlet.

#### Threshold

Coverlet allows you to specify a coverage threshold below which it fails the build. This allows you to enforce a minimum coverage percent on all changes to your project.

```bash
dotnet test /p:CollectCoverage=true /p:Threshold=80
```

The above command will automatically fail the build if the line, branch or method coverage of _any_ of the instrumented modules falls below 80%. You can specify what type of coverage to apply the threshold value to using the `ThresholdType` property. For example to apply the threshold check to only **line** coverage:

```bash
dotnet test /p:CollectCoverage=true /p:Threshold=80 /p:ThresholdType=line
```

You can specify multiple values for `ThresholdType` by separating them with commas. Valid values include `line`, `branch` and `method`.

By default, Coverlet will validate the threshold value against the coverage result of each module. The `/p:ThresholdStat` option allows you to change this behaviour and can have any of the following values:

* Minimum (Default): Ensures the coverage result of each module isn't less than the threshold
* Total: Ensures the total combined coverage result of all modules isn't less than the threshold
* Average: Ensures the average coverage result of all modules isn't less than the threshold

The following command will compare the threshold value with the overall total coverage of all modules:

```bash
dotnet test /p:CollectCoverage=true /p:Threshold=80 /p:ThresholdType=line /p:ThresholdStat=total
```

#### Excluding From Coverage

##### Attributes

You can ignore a method or an entire class from code coverage by creating and applying the `ExcludeFromCodeCoverage` attribute present in the `System.Diagnostics.CodeAnalysis` namespace.

You can also ignore additional attributes by using the `ExcludeByAttribute` property (short name or full name supported):

```bash
dotnet test /p:CollectCoverage=true /p:ExcludeByAttribute="Obsolete,GeneratedCodeAttribute,CompilerGeneratedAttribute"
```

#### Source Files
You can also ignore specific source files from code coverage using the `ExcludeByFile` property
 - Use single or multiple paths (separate by comma)
 - Use absolute or relative paths (relative to the project directory)
 - Use file path or directory path with globbing (e.g `dir1/*.cs`)

```bash
dotnet test /p:CollectCoverage=true /p:ExcludeByFile=\"../dir1/class1.cs,../dir2/*.cs,../dir3/**/*.cs\"
```

##### Filters
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

Both `Exclude` and `Include` properties can be used together but `Exclude` takes precedence.

You can specify multiple filter expressions by separting them with a comma (`,`).

### SourceLink

Coverlet supports [SourceLink](https://github.com/dotnet/sourcelink) custom debug information contained in PDBs. When you specify the `--use-source-link` flag in the global tool or `/p:UseSourceLink=true` property in the MSBuild command, Coverlet will generate results that contain the URL to the source files in your source control instead of absolute file paths.

### Cake Addin
If you're using [Cake Build](https://cakebuild.net) for your build script you can use the [Cake.Coverlet](https://github.com/Romanx/Cake.Coverlet) addin to provide you extensions to dotnet test for passing coverlet arguments in a strongly typed manner.

## Issues & Contributions

If you find a bug or have a feature request, please report them at this repository's issues section. See the [CONTRIBUTING GUIDE](CONTRIBUTING.md) for details on building and contributing to this project.

## Code of Conduct

This project enforces a code of conduct in line with the contributor covenant. See [CODE OF CONDUCT](CODE_OF_CONDUCT.md) for details.

## License

This project is licensed under the MIT license. See the [LICENSE](LICENSE) file for more info.
