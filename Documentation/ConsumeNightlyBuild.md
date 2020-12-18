# Consume nightly build

To consume nightly builds, create a `NuGet.Config` in your root solution directory and add the following content:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <!-- Coverlet nightly build feed -->
    <add key="coverletNightly" value="https://pkgs.dev.azure.com/tonerdo/coverlet/_packaging/coverlet-nightly/nuget/v3/index.json" /> 
    <!-- Default nuget feed -->
    <add key="nuget" value="https://api.nuget.org/v3/index.json" /> 
    <!-- Add all other needed feed -->
  </packageSources>
</configuration>
```

### Install packages

Visual Studio:

![File](images/nightly.PNG)

NuGet (Package Manager console):

```powershell
PM> Install-Package coverlet.msbuild -Version 3.0.0-preview.18.g183cbed8a6 -Source https://pkgs.dev.azure.com/tonerdo/coverlet/_packaging/coverlet-nightly/nuget/v3/index.json
```

.NET CLI:

```bash
 dotnet add package coverlet.msbuild --version 3.0.0-preview.18.g183cbed8a6 --source https://pkgs.dev.azure.com/tonerdo/coverlet/_packaging/coverlet-nightly/nuget/v3/index.json
```

MSBuild project file:

```xml
<PackageReference Include="coverlet.msbuild" Version="3.0.0-preview.18.g183cbed8a6" />
```

_Note: The version provided here is just an example, you should use the latest when possible_
