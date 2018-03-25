[![Build Status](https://www.travis-ci.org/tonerdo/coverlet.svg?branch=master)](https://www.travis-ci.org/tonerdo/coverlet)
[![Build status](https://ci.appveyor.com/api/projects/status/6rdf00wufospr4r8?svg=true)](https://ci.appveyor.com/project/tonerdo/coverlet)
[![Coverage Status](https://img.shields.io/coveralls/github/tonerdo/coverlet.svg)](https://coveralls.io/github/tonerdo/coverlet?branch=master)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
# coverlet

Coverlet is a cross platform code coverage library for .NET Core, with support for line and method coverage.

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

* Locate the unit test assembly and selects all the referenced assemblies that have PDBs.
* Instruments the selected assemblies by inserting code to record sequence point hits to a temporary file.

### After Tests Run

* Restore the original non-instrumented assembly files.
* Read the recorded hits information from the temporary file.
* Generate the coverage result from the hits information and write it to a file.

_Note: The assembly you'd like to get coverage for must be different from the assembly that contains the tests_

## Usage

Coverlet doesn't require any additional setup other than including the NuGet package. It integrates with the `dotnet test` infrastructure built into the .NET Core CLI and when enabled will automatically generate coverage results after tests are run.

### Code Coverage

Enabling code coverage is as simple as setting the `CollectCoverage` property to `true`

```bash
dotnet test /p:CollectCoverage=true
```

After the above command is run, a `coverage.json` file containing the results will be generated in the root directory of the test project. A summary of the results will also be displayed in the terminal.

### Coverage Output

Coverlet can generate coverage results in multiple formats, which is specified using the `CoverletOutputFormat` property. Possible values include `json` (default), `lcov` and `opencover`. For example, the following command emits coverage results in the opencover format:

```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

The output folder of the coverage result file can also be specified using the `CoverletOutputDirectory` property.

### Excluding Classes/Methods From Coverage

By default, Coverlet instruments every method of every class but sometimes for any number of reasons you might want it to ignore a specific method or class altogether. This is easily achieved by creating and applying the `ExcludeFromCoverage` attribute to the method or the class. Coverlet uses just the type name so the `ExcludeFromCoverage` class can be created under any namespace you wish. Also, in line with standard C# convention, either `ExcludeFromCoverage` or `ExcludeFromCoverageAttribute` will work.

## Roadmap

* Branch coverage
* Filter modules to be instrumented
* Support for more output formats (e.g. JaCoCo, Cobertura)
* Console runner (removes the need for requiring a NuGet package)

## Issues & Contributions

If you find a bug or have a feature request, please report them at this repository's issues section. Contributions are highly welcome, however, except for very small changes, kindly file an issue and let's have a discussion before you open a pull request.

## License

This project is licensed under the MIT license. See the [LICENSE](LICENSE) file for more info.
