[![Build Status](https://www.travis-ci.org/tonerdo/coverlet.svg?branch=master)](https://www.travis-ci.org/tonerdo/coverlet)
[![Build status](https://ci.appveyor.com/api/projects/status/6rdf00wufospr4r8?svg=true)](https://ci.appveyor.com/project/tonerdo/coverlet)
[![Coverage Status](https://coveralls.io/repos/github/tonerdo/coverlet/badge.svg?branch=master)](https://coveralls.io/github/tonerdo/coverlet?branch=master)
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

## Roadmap

* Branch coverage
* Console runner (removes the need for requiring a NuGet package)

## Issues & Contributions

If you find a bug or have a feature request, please report them at this repository's issues section. Contributions are highly welcome, however, except for very small changes, kindly file an issue and let's have a discussion before you open a pull request.

## License

This project is licensed under the MIT license. See the [LICENSE](LICENSE) file for more info.
