# coverlet [![Build status](https://ci.appveyor.com/api/projects/status/6rdf00wufospr4r8/branch/master?svg=true)](https://ci.appveyor.com/project/tonerdo/coverlet) [![codecov](https://codecov.io/gh/tonerdo/coverlet/branch/master/graph/badge.svg)](https://codecov.io/gh/tonerdo/coverlet) [![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

Coverlet is a cross platform code coverage library for .NET Core, with support for line, branch and method coverage.

## Installation

Available on [NuGet](https://www.nuget.org/packages/coverlet.msbuild/)

Visual Studio:

```powershell
PM> Install-Package coverlet.msbuild
```

.NET Core CLI:

```bash
dotnet add package coverlet.msbuild
```

## How It Works

Coverlet integrates with the MSBuild system and that allows it to go through the following process:

### Before Tests Run

* Locates the unit test assembly and selects all the referenced assemblies that have PDBs.
* Instruments the selected assemblies by inserting code to record sequence point hits to a temporary file.

### After Tests Run

* Restore the original non-instrumented assembly files.
* Read the recorded hits information from the temporary file.
* Generate the coverage result from the hits information and write it to a file.

_Note: The assembly you'd like to get coverage for must be different from the assembly that contains the tests_

## Usage

Coverlet doesn't require any additional setup other than including the NuGet package in the unit test project. It integrates with the `dotnet test` infrastructure built into the .NET Core CLI and when enabled, will automatically generate coverage results after tests are run.

### Code Coverage

Enabling code coverage is as simple as setting the `CollectCoverage` property to `true`

```bash
dotnet test /p:CollectCoverage=true
```

After the above command is run, a `coverage.json` file containing the results will be generated in the root directory of the test project. A summary of the results will also be displayed in the terminal.

### Coverage Output

Coverlet can generate coverage results in multiple formats, which is specified using the `CoverletOutputFormat` property. For example, the following command emits coverage results in the `opencover` format:

```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

Supported Formats:

* json (default)
* lcov
* opencover
* cobertura

The output of the coverage result can also be specified using the `CoverletOutput` property.

### Threshold

Coverlet allows you to specify a coverage threshold below which it fails the build. This allows you to enforce a minimum coverage percent on all changes to your project.

```bash
dotnet test /p:CollectCoverage=true /p:Threshold=80
```

The above command will automatically fail the build if the line, branch or method coverage of _any_ of the instrumented modules falls below 80%. You can specify what type of coverage to apply the threshold value to using the `ThresholdType` property. For example to apply the threshold check to only **line** coverage:

```bash
dotnet test /p:CollectCoverage=true /p:Threshold=80 /p:ThresholdType=line
```

You can specify multiple values for `ThresholdType` by separating them with commas. Valid values include `line`, `branch` and `method`.

### Excluding From Coverage

#### Attributes

You can ignore a method or an entire class from code coverage by creating and applying the `ExcludeFromCodeCoverage` attribute present in the `System.Diagnostics.CodeAnalysis` namespace.

#### Source Files
You can also ignore specific source files from code coverage using the `ExcludeByFile` property
 - Use single or multiple paths (separate by comma)
 - Use absolute or relative paths (relative to the project directory)
 - Use file path or directory path with globbing (e.g `dir1/*.cs`)

```bash
dotnet test /p:CollectCoverage=true /p:ExcludeByFile=\"../dir1/class1.cs,../dir2/*.cs,../dir3/**/*.cs,\"
```

#### Filters 
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


You can specify multiple filter expressions by separting them with a comma (`,`). If you specify multiple filters, then [you'll have to escape the surrounding quotes](https://github.com/Microsoft/msbuild/issues/2999#issuecomment-366078677) like this:
`/p:Exclude=\"[coverlet.*]*,[*]Coverlet.Core*\"`.

### Cake Addin
If you're using [Cake Build](https://cakebuild.net) for your build script you can use the [Cake.Coverlet](https://github.com/Romanx/Cake.Coverlet) addin to provide you extensions to dotnet test for passing coverlet arguments in a strongly typed manner.

## Roadmap

* Merging outputs (multiple test projects, one coverage result)
* Support for more output formats (e.g. JaCoCo)
* Console runner (removes the need for requiring a NuGet package)

## Issues & Contributions

If you find a bug or have a feature request, please report them at this repository's issues section. Contributions are highly welcome, however, except for very small changes, kindly file an issue and let's have a discussion before you open a pull request.

### Building The Project

Clone this repo:

```bash
git clone https://github.com/tonerdo/coverlet
```

Change directory to repo root:

```bash
cd coverlet
```

Execute build script:

```bash
dotnet msbuild build.proj
```

This will result in the following:

* Restore all NuGet packages required for building
* Build and publish all projects. Final binaries are placed into `<repo_root>\build\<Configuration>`
* Build and run tests

These steps must be followed before you attempt to open the solution in an IDE (e.g. Visual Studio, Rider) for all projects to be loaded successfully.

## Code of Conduct

This project enforces a code of conduct in line with the contributor covenant. See [CODE OF CONDUCT](CODE_OF_CONDUCT.md) for details.

## License

This project is licensed under the MIT license. See the [LICENSE](LICENSE) file for more info.
