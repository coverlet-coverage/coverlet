# Coverlet as a Global Tool

To see a list of options, run:

```bash
coverlet --help
```

The current options are (output of `coverlet --help`):

```bash
Cross platform .NET Core code coverage tool 3.0.0.0

Usage: coverlet [arguments] [options]

Arguments:
  <ASSEMBLY|DIRECTORY>         Path to the test assembly or application directory.

Options:
  -h|--help                    Show help information
  -v|--version                 Show version information
  -t|--target                  Path to the test runner application.
  -a|--targetargs              Arguments to be passed to the test runner.
  -o|--output                  Output of the generated coverage report
  -v|--verbosity               Sets the verbosity level of the command. Allowed values are quiet, minimal, normal, detailed.
  -f|--format                  Format of the generated coverage report.
  --threshold                  Exits with error if the coverage % is below value.
  --threshold-type             Coverage type to apply the threshold to.
  --threshold-stat             Coverage statistic used to enforce the threshold value.
  --exclude                    Filter expressions to exclude specific modules and types.
  --include                    Filter expressions to include only specific modules and types.
  --exclude-by-file            Glob patterns specifying source files to exclude.
  --include-directory          Include directories containing additional assemblies to be instrumented.
  --exclude-by-attribute       Attributes to exclude from code coverage.
  --include-test-assembly      Specifies whether to report code coverage of the test assembly.
  --single-hit                 Specifies whether to limit code coverage hit reporting to a single hit for each location
  --skipautoprops              Neither track nor record auto-implemented properties.
  --merge-with                 Path to existing coverage result to merge.
  --use-source-link            Specifies whether to use SourceLink URIs in place of file system paths.
  --does-not-return-attribute  Attributes that mark methods that do not return.
```

NB. For a [multiple value] options you have to specify values multiple times i.e.
```
--exclude-by-attribute 'Obsolete' --exclude-by-attribute'GeneratedCode' --exclude-by-attribute 'CompilerGenerated'
```
For `--merge-with` [check the sample](Examples.md).

## Code Coverage

The `coverlet` tool is invoked by specifying the path to the assembly that contains the unit tests. You also need to specify the test runner and the arguments to pass to the test runner using the `--target` and `--targetargs` options respectively. The invocation of the test runner with the supplied arguments **must not** involve a recompilation of the unit test assembly or no coverage data will be generated.

The following example shows how to use the familiar `dotnet test` toolchain:

```bash
coverlet /path/to/test-assembly.dll --target "dotnet" --targetargs "test /path/to/test-project --no-build"
```

After the above command is run, a `coverage.json` file containing the results will be generated in the directory the `coverlet` command was run. A summary of the results will also be displayed in the terminal.

_Note: The `--no-build` flag is specified so that the `/path/to/test-assembly.dll` isn't rebuilt_

## Code Coverage for integration tests and end-to-end tests.

Sometimes, there are tests that doesn't use regular unit test frameworks like xunit. You may find yourself in a situation where your tests are driven by a custom executable/script, which when run, could do anything from making API calls to driving Selenium.

As an example, suppose you have a folder `/integrationtest` which contains said executable (lets call it `runner.exe`) and everything it needs to successfully execute. You can use our tool to startup the executable and gather live coverage:

```bash
coverlet "/integrationtest" --target "/application/runner.exe"
```

Coverlet will first instrument all .NET assemblies within the `integrationtests` folder, after which it will execute `runner.exe`. Finally, at shutdown of your `runner.exe`, it will generate the coverage report. You can use all parameters available to customize the report generation. Coverage results will be generated once `runner.exe` exits. You can use all parameters available to customize the report generation.

_Note: Today, Coverlet relies on `AppDomain.CurrentDomain.ProcessExit` and `AppDomain.CurrentDomain.DomainUnload` to record hits to the filesystem, as a result, you need to ensure a graceful process shutdown. Forcefully, killing the process will result in an incomplete coverage report._

## Coverage Output

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

### TeamCity Output

Coverlet can output basic code coverage statistics using [TeamCity service messages](https://confluence.jetbrains.com/display/TCD18/Build+Script+Interaction+with+TeamCity#BuildScriptInteractionwithTeamCity-ServiceMessages).

```bash
coverlet <ASSEMBLY> --target <TARGET> --targetargs <TARGETARGS> --output teamcity
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

With Coverlet you can combine the output of multiple coverage runs into a single result.

```bash
coverlet <ASSEMBLY> --target <TARGET> --targetargs <TARGETARGS> --merge-with "/path/to/result.json" --format opencover
```

The value given to `--merge-with` **must** be a path to Coverlet's own json result format.

## Threshold

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

## Excluding From Coverage

### Attributes

You can ignore a method or an entire class from code coverage by creating and applying the `ExcludeFromCodeCoverage` attribute present in the `System.Diagnostics.CodeAnalysis` namespace.

You can also ignore additional attributes by using the `ExcludeByAttribute` property (short name or full name supported):

```bash
coverlet <ASSEMBLY> --target <TARGET> --targetargs <TARGETARGS> --exclude-by-attribute 'Obsolete' --exclude-by-attribute'GeneratedCode' --exclude-by-attribute 'CompilerGenerated'
```

### Source Files

You can also ignore specific source files from code coverage using the `--exclude-by-file` option
 - Can be specified multiple times
 - Use file path or directory path with globbing (e.g `dir1/*.cs`)

```bash
coverlet <ASSEMBLY> --target <TARGET> --targetargs <TARGETARGS> --exclude-by-file "**/dir1/class1.cs"
```

### Filters

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

You can also include coverage of the test assembly itself by specifying the `--include-test-assembly` flag.

## SourceLink

Coverlet supports [SourceLink](https://github.com/dotnet/sourcelink) custom debug information contained in PDBs. When you specify the `--use-source-link` flag, Coverlet will generate results that contain the URL to the source files in your source control instead of local file paths.

## Exit Codes

Coverlet outputs specific exit codes to better support build automation systems for determining the kind of failure so the appropriate action can be taken.

```bash
0 - Success.
1 - If any test fails.
2 - Coverage percentage is below threshold.
3 - Test fails and also coverage percentage is below threshold.
101 - General exception occurred during coverlet process.
102 - Missing options or invalid arguments for coverlet process.
```
