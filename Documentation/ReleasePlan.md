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

## Release Calendar

We release 3 components as nuget packages:  

**coverlet.msbuild.nupkg**  
**coverlet.console.nupkg**  
**coverlet.collector.nupkg**  

We plan 1 release [once per quarter](https://en.wikipedia.org/wiki/Calendar_year) if there is *at least* 1 new commit of source code on master. This release may be a major, minor, or patch version upgrade from the previous release depending on impact to consumers. 
**We release intermediate packages in case of severe bug or to unblock users.**

### Current versions

| Package        | **coverlet.msbuild** |
| :-------------: |:-------------:|
|**coverlet.msbuild**      | 2.8.1  |  
|**coverlet.console**      | 1.7.1  |
|**coverlet.collector**      | 1.2.1 |  

### Proposed next versions  

We bump version based on Semantic Versioning 2.0.0 spec.  
If we add features to **coverlet.core.dll** we bump MINOR version of all packages.  
If we do breaking changes on **coverlet.core.dll** we bump MAJOR version of all packages.  
We MANUALLY bump versions on production release, so we have different release plan between prod and nigntly packages.

| Release Date        | **coverlet.msbuild**           | **coverlet.console**  | **coverlet.collector** | **commit hash**| **notes** |
| :-------------: |:-------------:|:-------------:|:-------------:|:-------------:|:-------------:|
| <01 August 2020>      | 2.9.0 | 1.7.2 |   1.3.0 | | deterministic build support
| 04 April 2020      | 2.8.1 | 1.7.1 |   1.2.1 | 3f81828821d07d756e02a4105b2533cedf0b543c
| 03 January 2019      | 2.8.0 | 1.7.0 |   1.2.0 | 72a688f1c47fa92059540d5fbb1c4b0b4bf0dc8c |  |
| 23 September 2019      | 2.7.0 | 1.6.0 |   1.1.0 | 4ca01eb239038808739699470a61fad675af6c79 |  |
| 01 July 2019      | 2.6.3 | 1.5.3 |   1.0.1 | e1593359497fdfe6befbb86304b8f4e09a656d14 |  |
| 06 June 2019      | 2.6.2 | 1.5.2 |   1.0.0 | 3e7eac9df094c22335711a298d359890aed582e8 | first collector release |

*< date >  Expected next release date

To get the list of commits between two version use git command
```bash
 git log --oneline hashbefore currenthash
```

# How to manually release packages to Nuget.org

This is the steps to do to release new packages to Nuget.org

1) Clone repo, **remember to build packages from master and not from your fork or metadata links will point to your forked repo.**  
Run `git log -5` from repo root to verify last commit.

2) Update project versions in file:

Collector 
https://github.com/tonerdo/coverlet/blob/master/src/coverlet.collector/version.json  
.NET tool
https://github.com/tonerdo/coverlet/blob/master/src/coverlet.console/version.json  
Msbuild tasks
https://github.com/tonerdo/coverlet/blob/master/src/coverlet.msbuild.tasks/version.json  

Core lib project file https://github.com/tonerdo/coverlet/blob/master/src/coverlet.core/coverlet.core.csproj.
The version of core lib project file is the version we'll report on github repo releases https://github.com/tonerdo/coverlet/releases


Sample of updated version PR https://github.com/tonerdo/coverlet/pull/675/files  

3) From new cloned, aligned and versions updated repo root run pack command
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

4) Upload *.nupkg files to Nuget.org site. **Check all metadata(url links, deterministic build etc...) before "Submit"**

5) **On your fork**:
*   Align to master
*   Update versions in files accordingly to new release and commit/merge to master
*   Create release on repo https://github.com/tonerdo/coverlet/releases using https://github.com/tonerdo/coverlet/blob/master/src/coverlet.core/coverlet.core.csproj assembly version
*   Update the [Release Plan](https://github.com/tonerdo/coverlet/blob/master/Documentation/ReleasePlan.md)(this document) and [ChangeLog](https://github.com/tonerdo/coverlet/blob/master/Documentation/Changelog.md)