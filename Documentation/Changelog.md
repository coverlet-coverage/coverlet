# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

-Added support for deterministic build for msbuild/collectors driver [#802](https://github.com/tonerdo/coverlet/pull/802)  [#796](https://github.com/tonerdo/coverlet/pull/796) with the help of https://github.com/clairernovotny and https://github.com/tmat

### Improvements

-Fix for code complexity not being generated for methods for cobertura reporter [#738](https://github.com/tonerdo/coverlet/pull/798) by https://github.com/dannyBies

## Release date 2020-04-02
### Packages  
coverlet.msbuild 2.8.1  
coverlet.console 1.7.1  
coverlet.collector 1.2.1  

### Fixed

-Fix ExcludeFromCodeCoverage attribute bugs [#129](https://github.com/tonerdo/coverlet/issues/129) and [#670](https://github.com/tonerdo/coverlet/issues/670) with [#671](https://github.com/tonerdo/coverlet/pull/671) by https://github.com/matteoerigozzi  
-Fix bug with nested types filtering [#689](https://github.com/tonerdo/coverlet/issues/689)  
-Fix Coverage Issue - New Using + Async/Await + ConfigureAwait [#669](https://github.com/tonerdo/coverlet/issues/669)  
-Improve branch detection for lambda functions and async/await statements [#702](https://github.com/tonerdo/coverlet/pull/702) by https://github.com/matteoerigozzi  
-Improve coverage, hide compiler generated branches for try/catch blocks inside async state machine [#716](https://github.com/tonerdo/coverlet/pull/716) by https://github.com/matteoerigozzi  

### Improvements

-Trim whitespace between values when reading from configuration from runsettings [#679](https://github.com/tonerdo/coverlet/pull/679) by https://github.com/EricStG  
-Code improvement, flow ILogger to InstrumentationHelper [#727](https://github.com/tonerdo/coverlet/pull/727) by https://github.com/daveMueller  
-Add support for line branch coverage in OpenCover format [#772](https://github.com/tonerdo/coverlet/pull/772) by https://github.com/costin-zaharia  

## Release date 2020-01-03
### Packages  
coverlet.msbuild 2.8.0  
coverlet.console 1.7.0  
coverlet.collector 1.2.0

### Added
-Add log to tracker [#553](https://github.com/tonerdo/coverlet/pull/553)  
-Exclude by assembly level System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage [#589](https://github.com/tonerdo/coverlet/pull/589)  
-Allow coverlet integration with other MSBuild test strategies[#615](https://github.com/tonerdo/coverlet/pull/615) by https://github.com/sharwell  

### Fixed

-Fix and simplify async coverage [#549](https://github.com/tonerdo/coverlet/pull/549)  
-Improve lambda scenario coverage [#583](https://github.com/tonerdo/coverlet/pull/583)  
-Mitigate issue in case of failure in assembly loading by cecil [#625](https://github.com/tonerdo/coverlet/pull/625)  
-Fix ConfigureAwait state machine generated branches [#634](https://github.com/tonerdo/coverlet/pull/634)  
-Fix coverage overwritten if the project has multiple target frameworks [#636](https://github.com/tonerdo/coverlet/issues/177)  
-Fix cobertura Jenkins reporter + source link support [#614](https://github.com/tonerdo/coverlet/pull/614) by https://github.com/daveMueller  
-Fix pdb file locking during instrumentation [#656](https://github.com/tonerdo/coverlet/pull/656)


### Improvements

-Improve exception message for unsupported runtime [#569](https://github.com/tonerdo/
coverlet/pull/569) by https://github.com/daveMueller  
-Improve cobertura absolute/relative path report generation [#661](https://github.com/tonerdo/coverlet/pull/661) by https://github.com/daveMueller

## Release date 2019-09-23
### Packages  
coverlet.msbuild 2.7.0  
coverlet.console 1.6.0  
coverlet.collector 1.1.0

### Added
-Output multiple formats for vstest integration [#533](https://github.com/tonerdo/coverlet/pull/533) by https://github.com/daveMueller  
-Different exit codes to indicate particular failures [#412](https://github.com/tonerdo/coverlet/pull/412) by https://github.com/sasivishnu


### Changed

-Skip instrumentation of module with embedded ppbd without local sources [#510](https://github.com/tonerdo/coverlet/pull/510), with this today xunit will be skipped in automatic way.

### Fixed

-Fix exclude by files [#524](https://github.com/tonerdo/coverlet/pull/524)  
-Changed to calculate based on the average coverage of the module [#479](https://github.com/tonerdo/coverlet/pull/479) by https://github.com/dlplenin  
-Fix property attribute detection [#477](https://github.com/tonerdo/coverlet/pull/477) by https://github.com/amweiss  
-Fix instrumentation serialization bug [#458](https://github.com/tonerdo/coverlet/pull/458)  
-Fix culture for cobertura xml report [#464](https://github.com/tonerdo/coverlet/pull/464)

