# Using Deterministic Builds with Coverlet and VSTest

## Prerequisites

Before running tests with deterministic builds, you need to generate the required NuGet packages. Run the following command from the repository root:

```powershell
dotnet pack
```

## Project Setup

Update your test project file `XUnitTestProject1.csproj` with the following configuration:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFrameworks>net10.0</TargetFrameworks>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
    <TestingPlatformDotnetTestSupport>false</TestingPlatformDotnetTestSupport>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.4.0" />
    <PackageReference Include="xunit.v3" Version="3.2.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.5">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
    </PackageReference>
    <!-- Update the version to match your locally built package -->
    <PackageReference Include="10.0.1-preview-0001-ga126b0a194" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\ClassLibrary1\ClassLibrary1.csproj" />
  </ItemGroup>
</Project>
```

## Running Tests

Navigate to your test project directory and execute:

```powershell
dotnet test --collect:"XPlat Code Coverage" /p:DeterministicSourcePaths=true -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.DeterministicReport=true
```

## Verification

After running the tests, verify that the deterministic build is working by checking for the source root mapping file:

```text
<ProjectRoot>\bin\Debug\net8.0\CoverletSourceRootsMapping_XUnitTestProject1
```

The presence of this file confirms that your coverage report was generated with deterministic build settings.
