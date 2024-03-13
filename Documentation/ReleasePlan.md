# Release Plan

## Versioning strategy

Coverlet is versioned with Semantic Versioning [2.0.0](https://semver.org/#semantic-versioning-200) that states:

```text
Given a version number MAJOR.MINOR.PATCH, increment the:

MAJOR version when you make incompatible API changes,
MINOR version when you add functionality in a backwards-compatible manner, and
PATCH version when you make backwards-compatible bug fixes.
Additional labels for pre-release and build metadata are available as extensions to the MAJOR.MINOR.PATCH format.
```

We release 3 components as NuGet packages:

**coverlet.msbuild.nupkg**
**coverlet.console.nupkg**
**coverlet.collector.nupkg**

## How to manually compare latest release with nightly build

Before creating a new release it makes sense to test the new release against a benchmark repository. This can help to determine bugs that haven't been found
by the unit/integration tests. Therefore, coverage of the latest release is compared with our nightly build.

In the following example the benchmark repository refit (<https://github.com/reactiveui/refit>) is used which already uses coverlet for coverage.

1. Clone the benchmark repository (<https://github.com/reactiveui/refit>)
2. Check if latest coverlet version is used by the project, otherwise add coverlet to the project (<https://github.com/coverlet-coverage/coverlet#installation>).
3. Create coverage report for latest coverlet version:

    ```shell
    dotnet test --collect:"XPlat Code Coverage"
    ```

4. Update the test projects with the latest nightly build version of coverlet
(<https://github.com/coverlet-coverage/coverlet/blob/master/Documentation/ConsumeNightlyBuild.md>).

5. Create coverage report for nightly build version by rerunning the tests:

    ```shell
    dotnet test --collect:"XPlat Code Coverage"
    ```

6. Check for differences in the coverage reports.

## How to manually release packages to nuget.org

This is the steps to release new packages to nuget.org

1. Update projects version in file `version.json` in root of repo (remove `-preview.{height}` and adjust version)

    Do a PR and merge to master.

1. Clone repo, **remember to build packages from master and not from your fork or metadata links will point to your forked repo.** . Run `git log -5` from repo root to verify last commit.

1. From new cloned, aligned and versions updated repo root run build command

  ```shell
  dotnet pack -c release /p:TF_BUILD=true /p:PublicRelease=true
  ...
  coverlet.core -> C:\GitHub\coverlet\artifacts\bin\coverlet.core\release_netstandard2.0\coverlet.core.dll
  coverlet.core -> C:\GitHub\coverlet\artifacts\bin\coverlet.core\release_net6.0\coverlet.core.dll
  coverlet.collector -> C:\GitHub\coverlet\artifacts\bin\coverlet.collector\release_netstandard2.0\coverlet.collector.dll
  coverlet.collector -> C:\GitHub\coverlet\artifacts\bin\coverlet.collector\release_net6.0\coverlet.collector.dll
  coverlet.msbuild.tasks -> C:\GitHub\coverlet\artifacts\bin\coverlet.msbuild.tasks\release_netstandard2.0\coverlet.msbuild.tasks.dll
  coverlet.msbuild.tasks -> C:\GitHub\coverlet\artifacts\bin\coverlet.msbuild.tasks\release_net6.0\coverlet.msbuild.tasks.dll
  coverlet.console -> C:\GitHub\coverlet\artifacts\bin\coverlet.console\release\coverlet.console.dll
  coverlet.console -> C:\GitHub\coverlet\artifacts\bin\coverlet.console\release\coverlet.console.exe
  ...
  Successfully created package 'C:\GitHub\coverlet\artifacts\package\release\coverlet.msbuild.6.0.1.nupkg'.
  Successfully created package 'C:\GitHub\coverlet\artifacts\package\release\coverlet.msbuild.6.0.1.snupkg'.
  Successfully created package 'C:\GitHub\coverlet\artifacts\package\release\coverlet.collector.6.0.1.nupkg'.
  Successfully created package 'C:\GitHub\coverlet\artifacts\package\release\coverlet.collector.6.0.1.snupkg'.
  Successfully created package 'C:\GitHub\coverlet\artifacts\package\release\coverlet.console.6.0.1.nupkg'.
  Successfully created package 'C:\GitHub\coverlet\artifacts\package\release\coverlet.console.6.0.1.snupkg'.
  ...
  ```

1. Sign nuget packages using sign <https://www.nuget.org/packages/sign>

```powershell
sign code azure-key-vault **/*.nupkg --base-directory [ROOT-DIRECTORY]\artifacts\package\release\ --file-digest sha256 --description Coverlet --description-url https://github.com/coverlet-coverage/coverlet `
 --azure-key-vault-url [KEYVAULT-URL] `
 --azure-key-vault-client-id [CLIENT-ID] `
 --azure-key-vault-tenant-id [TENANT-ID] `
 --azure-key-vault-client-secret [KEYVAULT-SECRET] `
 --azure-key-vault-certificate [CERT-FRIENDLY-NAME]
```

1. Upload *.nupkg files to Nuget.org site. **Check all metadata(url links, deterministic build etc...) before "Submit"**

1. **On your fork**:
    * Align to master
    * Bump version by one (fix part) and re-add `-preview.{height}`
    * Create release on repo <https://github.com/coverlet-coverage/coverlet/releases>
    * Update the [Release Plan](https://github.com/coverlet-coverage/coverlet/blob/master/Documentation/ReleasePlan.md)(this document) and [ChangeLog](https://github.com/coverlet-coverage/coverlet/blob/master/Documentation/Changelog.md)
    * Do PR and merge
