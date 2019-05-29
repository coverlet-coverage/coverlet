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
|**coverlet.msbuild**      | 2.6.1  |  
|**coverlet.console**      | 1.5.1  |
|**coverlet.collector**      | not yet released |  

### Proposed next versions  

We bump version based on Semantic Versioning 2.0.0 spec(PATCH is handled by Nerdbank.GitVersioning).  
If we add features to **coverlet.core.dll** we bump MINOR version of all packages.  
If we do breaking changes on **coverlet.core.dll** we bump MAJOR version of all packages.


| Release Date        | **coverlet.msbuild**           | **coverlet.console**  | **coverlet.collector** | **notes** |
| :-------------: |:-------------:|:-------------:|:-------------:|:-------------:|
| 1 July 2019      | 2.6 | 1.5 |   1.0 |               |
