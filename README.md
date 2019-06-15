# Coverlet

[![Build Status](https://dev.azure.com/tonerdo/coverlet/_apis/build/status/tonerdo.coverlet?branchName=master)](https://dev.azure.com/tonerdo/coverlet/_build/latest?definitionId=3&branchName=master)

Coverlet is a cross platform code coverage framework for .NET, with support for line, branch and method coverage. It works with .NET Framework on Windows and .NET Core on all supported platforms.

## Installation

**VSTest Integration**:

```bash
dotnet add package coverlet.collector
```

**MSBuild Integration**:

```bash
dotnet add package coverlet.msbuild
```

**Global Tool**:

```bash
dotnet tool install --global coverlet.console
```

## Quick Start

### VSTest Integration

Coverlet is integrated into the Visual Studio Test Platform as a [data collector](https://github.com/Microsoft/vstest-docs/blob/master/docs/extensions/datacollector.md). To get coverage simply run the following command:

```bash
dotnet test --collect:"XPlat Code Coverage"
```

After the above command is run, a `coverage.cobertura.json` file containing the results will be published to the `TestResults` directory as an attachment. A summary of the results will also be displayed in the terminal.

See [documentation](Documentation/VSTestIntegration.md) for advanced usage.

#### Requirements
* _You need to be running .NET Core SDK v2.2.300 and above_
* _You need to reference version 16.1.0 and above of Microsoft.NET.Test.Sdk_
```
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="16.1.0" />
```

### MSBuild Integration

Coverlet also integrates with the build system to run code coverage after tests. Enabling code coverage is as simple as setting the `CollectCoverage` property to `true`

```bash
dotnet test /p:CollectCoverage=true
```

After the above command is run, a `coverage.json` file containing the results will be generated in the root directory of the test project. A summary of the results will also be displayed in the terminal.

See [documentation](Documentation/MSBuildIntegration.md) for advanced usage.

#### Requirements
Requires a runtime that support _.NET Standard 2.0 and above_

### Global Tool

The `coverlet` tool is invoked by specifying the path to the assembly that contains the unit tests. You also need to specify the test runner and the arguments to pass to the test runner using the `--target` and `--targetargs` options respectively. The invocation of the test runner with the supplied arguments **must not** involve a recompilation of the unit test assembly or no coverage result will be generated.

The following example shows how to use the familiar `dotnet test` toolchain:

```bash
coverlet /path/to/test-assembly.dll --target "dotnet" --targetargs "test /path/to/test-project --no-build"
```

_Note: The `--no-build` flag is specified so that the `/path/to/test-assembly.dll` assembly isn't rebuilt_

See [documentation](Documentation/GlobalTool.md) for advanced usage.

#### Requirements
.NET global tools rely on a .NET Core runtime installed on your machine https://docs.microsoft.com/en-us/dotnet/core/tools/global-tools#what-could-go-wrong

.NET Coverlet global tool requires _.NET Core 2.2 and above_ 


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

## Cake Add-In

If you're using [Cake Build](https://cakebuild.net) for your build script you can use the [Cake.Coverlet](https://github.com/Romanx/Cake.Coverlet) add-in to provide you extensions to dotnet test for passing Coverlet arguments in a strongly typed manner.

## Issues & Contributions

If you find a bug or have a feature request, please report them at this repository's issues section. See the [CONTRIBUTING GUIDE](CONTRIBUTING.md) for details on building and contributing to this project.

## Code of Conduct

This project enforces a code of conduct in line with the contributor covenant. See [CODE OF CONDUCT](CODE_OF_CONDUCT.md) for details.

## License

This project is licensed under the MIT license. See the [LICENSE](LICENSE) file for more info.
