# Using Deterministic Builds with Coverlet MSBuild Integration

## Prerequisites

Before running tests with deterministic builds, you need to generate the local NuGet packages:

```shell
# Run from repository root
dotnet pack
```

## Project Setup

Update your test project file `XUnitTestProject1.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.13.0" />
    <PackageReference Include="xunit.v3" Version="2.0.0" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.0">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
    </PackageReference>
    <!-- Use the version from your locally built package -->
    <PackageReference Include="coverlet.msbuild" Version="8.0.1-preview.8.gcb9b802a5f">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\ClassLibrary1\ClassLibrary1.csproj" />
  </ItemGroup>
</Project>
```

## Running Tests

Navigate to your test project directory and run:

```shell
dotnet test /p:CollectCoverage=true /p:DeterministicSourcePaths=true
```

> **Important**: Do not use the `--no-build` option as it will prevent the generation of deterministic build artifacts.

## Verification

After running the tests, verify the deterministic build by checking for the source root mapping file:

```text
bin\Debug\net8.0\CoverletSourceRootsMapping_XUnitTestProject1
```

The presence of this file confirms that your coverage report was generated with deterministic build settings.