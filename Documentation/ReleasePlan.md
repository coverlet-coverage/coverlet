# Release Plan

## Versioning strategy

Coverlet is versioned with Semantic Versioning [2.0.0](https://semver.org/#semantic-versioning-200) that states:

```
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

### Current versions

| Package               | Version |
|:----------------------|:--------|
|**coverlet.msbuild**   | 3.0.3   |  
|**coverlet.console**   | 3.0.3   |
|**coverlet.collector** | 3.0.3   |  


| Release Date      | coverlet.msbuild | coverlet.console  | coverlet.collector| commit hash                              | notes                          |
| :-----------------|:-----------------|:------------------|:------------------|:-----------------------------------------|:-------------------------------|
| 21 February 2021  | 3.0.3            | 3.0.3             |   3.0.3           | adfabfd58de0aabe263e7d2080324e0b8541071e | Fix regressions                | 
| 24 January 2021   | 3.0.2            | 3.0.2             |   3.0.2           | ed918515492193fd154b60270d440c40fa30fee9 | Fix regressions                | 
| 16 January 2021   | 3.0.1            | 3.0.1             |   3.0.1           | 1b45fd89245369ae94407e7a77bdfee112042486 | Fix severe coverage regression | 
| 09 January 2021   | 3.0.0            | 3.0.0             |   3.0.0           | 1e77f9d2183a320e8991bfc296460e793301931f | Align versions numbers         | 
| 30 May 2020       | 2.9.0            | 1.7.2             |   1.3.0           | 83a38d45b3f9c231d705bfed849efbf41b3aaa86 | deterministic build support    |
| 04 April 2020     | 2.8.1            | 1.7.1             |   1.2.1           | 3f81828821d07d756e02a4105b2533cedf0b543c |                                |
| 03 January 2019   | 2.8.0            | 1.7.0             |   1.2.0           | 72a688f1c47fa92059540d5fbb1c4b0b4bf0dc8c |                                |
| 23 September 2019 | 2.7.0            | 1.6.0             |   1.1.0           | 4ca01eb239038808739699470a61fad675af6c79 |                                |
| 01 July 2019      | 2.6.3            | 1.5.3             |   1.0.1           | e1593359497fdfe6befbb86304b8f4e09a656d14 |                                |
| 06 June 2019      | 2.6.2            | 1.5.2             |   1.0.0           | 3e7eac9df094c22335711a298d359890aed582e8 | first collector release        |

To get the list of commits between two version use git command
```bash
git log --oneline hashbefore currenthash
```

# How to manually release packages to nuget.org

This is the steps to release new packages to nuget.org

1. Update projects version in file `version.json` in root of repo (remove `-preview.{height}` and adjust version)

Update core lib project file version https://github.com/coverlet-coverage/coverlet/blob/master/src/coverlet.core/coverlet.core.csproj.  

Do a PR and merge to master.

2. Clone repo, **remember to build packages from master and not from your fork or metadata links will point to your forked repo.** . Run `git log -5` from repo root to verify last commit.

3. From new cloned, aligned and versions updated repo root run pack command

  ```
  dotnet pack -c release /p:TF_BUILD=true /p:PublicRelease=true
  ...
  coverlet.console -> D:\git\coverlet\src\coverlet.console\bin\Release\netcoreapp2.2\coverlet.console.dll
  coverlet.console -> D:\git\coverlet\src\coverlet.console\bin\Release\netcoreapp2.2\publish\
  Successfully created package 'D:\git\coverlet\bin\Release\Packages\coverlet.msbuild.2.8.1.nupkg'.
  Successfully created package 'D:\git\coverlet\bin\Release\Packages\coverlet.msbuild.2.8.1.snupkg'.
  Successfully created package 'D:\git\coverlet\bin\Release\Packages\coverlet.console.1.7.1.nupkg'.
  Successfully created package 'D:\git\coverlet\bin\Release\Packages\coverlet.console.1.7.1.snupkg'.
  Successfully created package 'D:\git\coverlet\bin\Release\Packages\coverlet.collector.1.2.1.nupkg'.
  Successfully created package 'D:\git\coverlet\bin\Release\Packages\coverlet.collector.1.2.1.snupkg'.
  ```

4. Sign the packages using SignClient tool https://www.nuget.org/packages/SignClient

  ```powershell
  ❯ SignClient "Sign" `
  >> --baseDirectory "REPO ROOT DIRECTORY\bin" `
  >> --input "**/*.nupkg" `
  >> --config "ROOT REPO DIRECTORY\eng\signclient.json" `
  >> --user "USER" `
  >> --secret "SECRET" `
  >> --name "Coverlet" `
  >> --description "Coverlet" `
  >> --descriptionUrl "https://github.com/coverlet-coverage/coverlet"
  ```

5. Upload *.nupkg files to Nuget.org site. **Check all metadata(url links, deterministic build etc...) before "Submit"**

6. **On your fork**:
  * Align to master
  * Bump version by one (fix part) and re-add `-preview.{height}`
  * Create release on repo https://github.com/coverlet-coverage/coverlet/releases
  * Update the [Release Plan](https://github.com/coverlet-coverage/coverlet/blob/master/Documentation/ReleasePlan.md)(this document) and [ChangeLog](https://github.com/coverlet-coverage/coverlet/blob/master/Documentation/Changelog.md)
  * Do PR and merge
