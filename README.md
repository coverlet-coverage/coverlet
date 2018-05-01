# coverlet [![Build Status](https://www.travis-ci.org/tonerdo/coverlet.svg?branch=master)](https://www.travis-ci.org/tonerdo/coverlet) [![Build status](https://ci.appveyor.com/api/projects/status/6rdf00wufospr4r8/branch/master?svg=true)](https://ci.appveyor.com/project/tonerdo/coverlet) [![Coverage Status](https://coveralls.io/repos/github/tonerdo/coverlet/badge.svg?branch=master)](https://coveralls.io/github/tonerdo/coverlet?branch=master) [![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

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

Coverlet doesn't require any additional setup other than including the NuGet package to the unit test project. It integrates with the `dotnet test` infrastructure built into the .NET Core CLI and when enabled, will automatically generate coverage results after tests are run.

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

The output folder of the coverage result file can also be specified using the `CoverletOutputDirectory` property.

### Threshold

Coverlet allows you to specify a coverage threshold below which it fails the build. This allows you to enforce a minimum coverage percent on all changes to your project.

```bash
dotnet test /p:CollectCoverage=true /p:Threshold=80
```

The above command will automatically fail the build if the average code coverage of all instrumented modules falls below 80%.

### Excluding From Coverage

#### Attributes  
You can ignore a method or an entire class from code coverage by creating and applying any of the following attributes:

* ExcludeFromCoverage
* ExcludeFromCoverageAttribute

Coverlet just uses the type name, so the attributes can be created under any namespace of your choosing.

#### File Path  
You can also ignore specific source files from code coverage using the `Exclude` property
 - Use single or multiple paths (separate by comma)
 - Use absolute or relative paths (relative to the project directory)
 - Use file path or directory path with globbing (e.g `dir1/*.cs`)

```bash
dotnet test /p:CollectCoverage=true /p:Exclude=\"../dir1/class1.cs,../dir2/*.cs,../dir3/**/*.cs,\"
```

## Roadmap

* Filter modules to be instrumented
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
