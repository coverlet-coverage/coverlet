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
|**coverlet.msbuild**      | 2.6.3  |  
|**coverlet.console**      | 1.5.3  |
|**coverlet.collector**      | 1.0.1 |  

### Proposed next versions  

We bump version based on Semantic Versioning 2.0.0 spec.  
If we add features to **coverlet.core.dll** we bump MINOR version of all packages.  
If we do breaking changes on **coverlet.core.dll** we bump MAJOR version of all packages.  
We MANUALLY bump versions on production release, so we have different release plan between prod and nigntly packages.

| Release Date        | **coverlet.msbuild**           | **coverlet.console**  | **coverlet.collector** | **commit hash**| **notes** |
| :-------------: |:-------------:|:-------------:|:-------------:|:-------------:|:-------------:|
| 1 October 2019      | 2.6.4 | 1.5.4 |   1.0.2 | |  |
| 1 July 2019      | 2.6.3 | 1.5.3 |   1.0.1 | e1593359497fdfe6befbb86304b8f4e09a656d14 |  |
| 6 June 2019      | 2.6.2 | 1.5.2 |   1.0.0 | 3e7eac9df094c22335711a298d359890aed582e8 | first collector release |
