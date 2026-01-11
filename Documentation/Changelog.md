# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## Unreleased

### Fixed
- Fix System.CommandLine 2.0 release is available [#1776](https://github.com/coverlet-coverage/coverlet/issues/1776)
- Fix Excluding From Coverage bad defaults from given example [#1764](https://github.com/coverlet-coverage/coverlet/issues/1764)
- Fix branchpoint exclusion for sdk 8.0.407 [#1741](https://github.com/coverlet-coverage/coverlet/issues/1741)

### Improvements

- Coverlet MTP extension feature [#1788](https://github.com/coverlet-coverage/coverlet/pull/1788)
- Use multi targets projects for coverlet.collector, coverlet.msbuild.tasks packages [#1742](https://github.com/coverlet-coverage/coverlet/pull/1742)
- Use .NET 8.0 target framework for coverlet.core and remove Newtonsoft.Json [#1733](https://github.com/coverlet-coverage/coverlet/pull/1733)
- Use latest System.CommandLine version [#1660](https://github.com/coverlet-coverage/coverlet/pull/1660)
- Upgraded minimum required .NET SDK and runtime to .NET 8.0 LTS (Long Term Support) (**Breaking Change**)
- Use [xunit.v3](https://xunit.net/docs/getting-started/v3/whats-new) for tests and example code

## Release date 2025-01-20
### Packages
coverlet.msbuild 6.0.4
coverlet.console 6.0.4
coverlet.collector 6.0.4

### Fixed
- Fix empty coverage report when using include and exclude filters [#1726](https://github.com/coverlet-coverage/coverlet/issues/1726)

## Release date 2024-12-31
### Packages
coverlet.msbuild 6.0.3
coverlet.console 6.0.3
coverlet.collector 6.0.3

### Fixed
- Fix RuntimeConfigurationReader to support self-contained builds [#1705](https://github.com/coverlet-coverage/coverlet/pull/1705) by <https://github.com/pfeigl>
- Fix inconsistent filenames with UseSourceLink after .NET 8 [#1679](https://github.com/coverlet-coverage/coverlet/issues/1679)
- Fix hanging tests [#989](https://github.com/coverlet-coverage/coverlet/issues/989)
- Fix coverlet instrumentation becomes slow after installing dotnet sdk 8.0.200 [#1620](https://github.com/coverlet-coverage/coverlet/issues/1620)
- Fix upgrading v6.0.1 to v6.0.2 increases instrumentation time [#1649](https://github.com/coverlet-coverage/coverlet/issues/1649)
- Fix Unable to instrument module - NET 8 [#1631](https://github.com/coverlet-coverage/coverlet/issues/1631)
- Fix slow modules filtering process [#1646](https://github.com/coverlet-coverage/coverlet/issues/1646) by <https://github.com/BlackGad>
- Fix incorrect coverage await using in generic method [#1490](https://github.com/coverlet-coverage/coverlet/issues/1490)

### Improvements
- Cache the regex used in InstrumentationHelper [#1693](https://github.com/coverlet-coverage/coverlet/issues/1693)
- Enable dotnetTool integration tests for linux [#660](https://github.com/coverlet-coverage/coverlet/issues/660)

## Release date 2024-03-13
### Packages
coverlet.msbuild 6.0.2
coverlet.console 6.0.2
coverlet.collector 6.0.2

### Fixed
- Threshold-stat triggers error [#1634](https://github.com/coverlet-coverage/coverlet/issues/1634)
- Fixed coverlet collector 6.0.1 requires dotnet sdk 8 [#1625](https://github.com/coverlet-coverage/coverlet/issues/1625)
- Type initializer errors after updating from 6.0.0 to 6.0.1 [#1629](https://github.com/coverlet-coverage/coverlet/issues/1629)
- Exception when multiple exclude-by-attribute filters specified [#1624](https://github.com/coverlet-coverage/coverlet/issues/1624)

### Improvements
- More concise options to specify multiple parameters in coverlet.console [#1624](https://github.com/coverlet-coverage/coverlet/issues/1624)

## Release date 2024-02-19
### Packages
coverlet.msbuild 6.0.1
coverlet.console 6.0.1
coverlet.collector 6.0.1

### Fixed
- Uncovered lines in .NET 8 for inheriting records [#1555](https://github.com/coverlet-coverage/coverlet/issues/1555)
- Fix record constructors not covered when SkipAutoProps is true [#1561](https://github.com/coverlet-coverage/coverlet/issues/1561)
- Fix .NET 7 Method Group branch coverage issue [#1447](https://github.com/coverlet-coverage/coverlet/issues/1447)
- Fix ExcludeFromCodeCoverage does not exclude method in a partial class [#1548](https://github.com/coverlet-coverage/coverlet/issues/1548)
- Fix ExcludeFromCodeCoverage does not exclude F# task [#1547](https://github.com/coverlet-coverage/coverlet/issues/1547)
- Fix issues where ExcludeFromCodeCoverage ignored [#1431](https://github.com/coverlet-coverage/coverlet/issues/1431)
- Fix issues with ExcludeFromCodeCoverage attribute [#1484](https://github.com/coverlet-coverage/coverlet/issues/1484)
- Fix broken links in documentation [#1514](https://github.com/coverlet-coverage/coverlet/issues/1514)
- Fix problem with coverage for .net5 WPF application [#1221](https://github.com/coverlet-coverage/coverlet/issues/1221) by <https://github.com/lg2de>
- Fix unable to instrument module for Microsoft.AspNetCore.Mvc.Razor [#1459](https://github.com/coverlet-coverage/coverlet/issues/1459) by <https://github.com/lg2de>

### Improvements
- Extended exclude by attribute feature to work with fully qualified name [#1589](https://github.com/coverlet-coverage/coverlet/issues/1589)
- Use System.CommandLine instead of McMaster.Extensions.CommandLineUtils [#1474](https://github.com/coverlet-coverage/coverlet/issues/1474) by <https://github.com/Bertk>
- Fix deadlock in Coverlet.Integration.Tests.BaseTest [#1541](https://github.com/coverlet-coverage/coverlet/pull/1541) by <https://github.com/Bertk>
- Add coverlet.msbuild.tasks unit tests [#1534](https://github.com/coverlet-coverage/coverlet/pull/1534) by <https://github.com/Bertk>

## Release date 2023-05-21
### Packages
coverlet.msbuild 6.0.0
coverlet.console 6.0.0
coverlet.collector 6.0.0

### Fixed
- Could not write lines to file CoverletSourceRootsMapping - in use by another process [#1155](https://github.com/coverlet-coverage/coverlet/issues/1155)
- Incorrect coverage for methods returning IAsyncEnumerable in generic classes [#1383](https://github.com/coverlet-coverage/coverlet/issues/1383)
- Wrong branch coverage for async methods .NET Standard 1.x [#1376](https://github.com/coverlet-coverage/coverlet/issues/1376)
- Empty path exception in visual basic projects [#775](https://github.com/coverlet-coverage/coverlet/issues/775)
- Align published nuget package version to github release version [#1413](https://github.com/coverlet-coverage/coverlet/issues/1413)
- Sync nuget and github release versions [#1122](https://github.com/coverlet-coverage/coverlet/issues/1122)

### Improvements
- Migration of the project to .NET 6.0 [#1473](https://github.com/coverlet-coverage/coverlet/pull/1473)

### Breaking changes
- New parameter `ExcludeAssembliesWithoutSources` to control automatic assembly exclusion [1164](https://github.com/coverlet-coverage/coverlet/issues/1164). The parameter `InstrumentModulesWithoutLocalSources` has been removed. since it can be handled by setting `ExcludeAssembliesWithoutSources` to `None`.
- The default heuristics for determining whether to instrument an assembly has been changed. In previous versions any missing source file was taken as a signal that it was a third-party project that shouldn't be instrumented, with exceptions for some common file name patterns for source generators. Now only assemblies where no source files at all can be found are excluded from instrumentation, and the code for detecting source generator files have been removed. To get back to the behavior that at least one missing file is sufficient to exclude an assembly, set `ExcludeAssembliesWithoutSources` to `MissingAny`, or use assembly exclusion filters for more fine-grained control.

## Release date 2022-10-29
### Packages
coverlet.msbuild 3.2.0
coverlet.console 3.2.0
coverlet.collector 3.2.0

### Fixed
- Fix TypeLoadException when referencing Microsoft.Extensions.DependencyInjection v6.0.1 [#1390](https://github.com/coverlet-coverage/coverlet/issues/1390)
- Source Link for code generators fails [#1322](https://github.com/coverlet-coverage/coverlet/issues/1322)
- Await foreach has wrong branch coverage when method is generic [#1210](https://github.com/coverlet-coverage/coverlet/issues/1210)
- ExcludeFromCodeCoverage attribute on local functions ignores lambda expression [#1302](https://github.com/coverlet-coverage/coverlet/issues/1302)

### Added
- Added InstrumentModulesWithoutLocalSources setting [#1360](https://github.com/coverlet-coverage/coverlet/pull/1360) by @TFTomSun

## Release date 2022-02-06
### Packages
coverlet.msbuild 3.1.2
coverlet.console 3.1.2
coverlet.collector 3.1.2

### Fixed
- Fix CoreLib's coverage measurement is broken [#1286](https://github.com/coverlet-coverage/coverlet/pull/1286)
- Fix UnloadModule injection [1291](https://github.com/coverlet-coverage/coverlet/pull/1291)

## Release date 2022-01-30
### Packages
coverlet.msbuild 3.1.1
coverlet.console 3.1.1
coverlet.collector 3.1.1

### Fixed
- Fix wrong branch coverage with EnumeratorCancellation attribute [#1275](https://github.com/coverlet-coverage/coverlet/issues/1275)
- Fix negative coverage exceeding int.MaxValue [#1266](https://github.com/coverlet-coverage/coverlet/issues/1266)
- Fix summary output format for culture de-DE [#1263](https://github.com/coverlet-coverage/coverlet/issues/1263)
- Fix branch coverage issue for finally block with await [#1233](https://github.com/coverlet-coverage/coverlet/issues/1233)
- Fix threshold doesn't work when coverage empty [#1205](https://github.com/coverlet-coverage/coverlet/issues/1205)
- Fix branch coverage issue for il switch [#1177](https://github.com/coverlet-coverage/coverlet/issues/1177)
- Fix branch coverage with using statement and several awaits[#1176](https://github.com/coverlet-coverage/coverlet/issues/1176)
- Fix `CopyCoverletDataCollectorFiles` to avoid to override user dlls for `dotnet publish` scenario  [#1243](https://github.com/coverlet-coverage/coverlet/pull/1243)

### Improvements
- Improve logging in case of exception inside static ctor of NetstandardAwareAssemblyResolver [#1230](https://github.com/coverlet-coverage/coverlet/pull/1230)
- When collecting open the hitfile with read access [#1214](https://github.com/coverlet-coverage/coverlet/pull/1214) by <https://github.com/JamesWTruher>
- Add CompilerGenerated attribute to the tracker [#1229](https://github.com/coverlet-coverage/coverlet/pull/1229)

## Release date 2021-07-19
### Packages
coverlet.msbuild 3.1.0
coverlet.console 3.1.0
coverlet.collector 3.1.0

### Fixed
- Fix branch coverage for targetframework net472 [#1167](https://github.com/coverlet-coverage/coverlet/issues/1167)
- Fix F# projects with `unknown` source [#1145](https://github.com/coverlet-coverage/coverlet/issues/1145)
- Fix SkipAutoProps for inline assigned properties [#1139](https://github.com/coverlet-coverage/coverlet/issues/1139)
- Fix partially covered throw statement [#1144](https://github.com/coverlet-coverage/coverlet/pull/1144)
- Fix coverage threshold not failing when no coverage [#1115](https://github.com/coverlet-coverage/coverlet/pull/1115)
- Fix partially covered `await foreach` statement [#1107](https://github.com/coverlet-coverage/coverlet/pull/1107) by <https://github.com/alexthornton1>
- Fix `System.MissingMethodException`(TryGetIntArgFromDict) [#1101](https://github.com/coverlet-coverage/coverlet/pull/1101)
- Fix ExcludeFromCodeCoverage on props [#1114](https://github.com/coverlet-coverage/coverlet/pull/1114)
- Fix incorrect branch coverage with await using [#1111](https://github.com/coverlet-coverage/coverlet/pull/1111) by <https://github.com/alexthornton1>

### Added
- Support deterministic reports [#1113](https://github.com/coverlet-coverage/coverlet/pull/1113)
- Specifying threshold level for each threshold type  [#1123](https://github.com/coverlet-coverage/coverlet/pull/1123) by <https://github.com/pbmiguel>

### Improvements
- Implementation of Npath complexity for the OpenCover reports [#1058](https://github.com/coverlet-coverage/coverlet/pull/1058) by <https://github.com/benjaminZale>

## Release date 2021-02-21
### Packages
coverlet.msbuild 3.0.3
coverlet.console 3.0.3
coverlet.collector 3.0.3

### Fixed

- Fix code coverage stops working if assembly contains source generators generated file [#1091](https://github.com/coverlet-coverage/coverlet/pull/1091)

## Release date 2021-01-24
### Packages
coverlet.msbuild 3.0.2
coverlet.console 3.0.2
coverlet.collector 3.0.2

### Fixed

- Fix multi-line lambda coverage regression [#1060](https://github.com/coverlet-coverage/coverlet/pull/1060)
- Opt-in reachability helper to mitigate resolution issue [#1061](https://github.com/coverlet-coverage/coverlet/pull/1061)

## Release date 2021-01-16
### Packages
coverlet.msbuild 3.0.1
coverlet.console 3.0.1
coverlet.collector 3.0.1

### Fixed

- Fix severe loss in coverage [#1043](https://github.com/coverlet-coverage/coverlet/pull/1043) by <https://github.com/daveMueller>

## Release date 2021-01-09
### Packages
coverlet.msbuild 3.0.0
coverlet.console 3.0.0
coverlet.collector 3.0.0

### Fixed
- Attribute exclusion does not work if attribute name does not end with "Attribute" [#884](https://github.com/coverlet-coverage/coverlet/pull/884) by <https://github.com/bddckr>
- Fix deterministic build+source link bug [#895](https://github.com/coverlet-coverage/coverlet/pull/895)
- Fix anonymous delegate compiler generate bug [#896](https://github.com/coverlet-coverage/coverlet/pull/896)
- Fix incorrect branch coverage with await ValueTask [#949](https://github.com/coverlet-coverage/coverlet/pull/949) by <https://github.com/alexthornton1>
- Fix switch pattern coverage [#1006](https://github.com/coverlet-coverage/coverlet/pull/1006)

### Added
- Skip autoprops feature [#912](https://github.com/coverlet-coverage/coverlet/pull/912)
- Exclude code that follows [DoesNotReturn] from code coverage [#904](https://github.com/coverlet-coverage/coverlet/pull/904) by <https://github.com/kevin-montrose>
- `CoverletReport` MSBuild variable containing coverage filenames [#932](https://github.com/coverlet-coverage/coverlet/pull/932) by <https://github.com/0xced>
- Add Visual Studio Add-In [#954](https://github.com/coverlet-coverage/coverlet/pull/954) by <https://github.com/FortuneN>
- Remove workaround for deterministic build for sdk >= 3.1.100 [#965](https://github.com/coverlet-coverage/coverlet/pull/965)
- Allow standalone coverlet usage for integration/end-to-end tests using .NET tool driver [#991](https://github.com/coverlet-coverage/coverlet/pull/991)
- Support .NET Framework(>= net461) for in-process data collectors [#970](https://github.com/coverlet-coverage/coverlet/pull/970)

## Release date 2020-05-30
### Packages
coverlet.msbuild 2.9.0
coverlet.console 1.7.2
coverlet.collector 1.3.0

### Fixed

- Fix for code complexity not being generated for methods for cobertura reporter [#738](https://github.com/tonerdo/coverlet/pull/798) by <https://github.com/dannyBies>
- Fix coverage, skip branches in generated `MoveNext()` for singleton iterators [#813](https://github.com/coverlet-coverage/coverlet/pull/813) by <https://github.com/bert2>
- Fix 'The process cannot access the file...because it is being used by another process' due to double flush for collectors driver [#https://github.com/coverlet-coverage/coverlet/pull/835](https://github.com/coverlet-coverage/coverlet/pull/835)
- Fix skip [ExcludefromCoverage] for generated async state machine [#849](https://github.com/coverlet-coverage/coverlet/pull/849)

### Added

- Added support for deterministic build for msbuild/collectors driver [#802](https://github.com/tonerdo/coverlet/pull/802)  [#796](https://github.com/tonerdo/coverlet/pull/796) with the help of <https://github.com/clairernovotny> and <https://github.com/tmat>

### Improvements

- Refactor DependencyInjection [#728](https://github.com/coverlet-coverage/coverlet/pull/768) by <https://github.com/daveMueller>

## Release date 2020-04-02
### Packages
coverlet.msbuild 2.8.1
coverlet.console 1.7.1
coverlet.collector 1.2.1

### Fixed

- Fix ExcludeFromCodeCoverage attribute bugs [#129](https://github.com/tonerdo/coverlet/issues/129) and [#670](https://github.com/tonerdo/coverlet/issues/670) with [#671](https://github.com/tonerdo/coverlet/pull/671) by <https://github.com/matteoerigozzi>
- Fix bug with nested types filtering [#689](https://github.com/tonerdo/coverlet/issues/689)
- Fix Coverage Issue - New Using + Async/Await + ConfigureAwait [#669](https://github.com/tonerdo/coverlet/issues/669)
- Improve branch detection for lambda functions and async/await statements [#702](https://github.com/tonerdo/coverlet/pull/702) by <https://github.com/matteoerigozzi>
- Improve coverage, hide compiler generated branches for try/catch blocks inside async state machine [#716](https://github.com/tonerdo/coverlet/pull/716) by <https://github.com/matteoerigozzi>
- Improve coverage, skip lambda cached field [#753](https://github.com/tonerdo/coverlet/pull/753)

### Improvements

- Trim whitespace between values when reading from configuration from runsettings [#679](https://github.com/tonerdo/coverlet/pull/679) by <https://github.com/EricStG>
- Code improvement, flow ILogger to InstrumentationHelper [#727](https://github.com/tonerdo/coverlet/pull/727) by <https://github.com/daveMueller>
- Add support for line branch coverage in OpenCover format [#772](https://github.com/tonerdo/coverlet/pull/772) by <https://github.com/costin-zaharia>

## Release date 2020-01-03
### Packages
coverlet.msbuild 2.8.0
coverlet.console 1.7.0
coverlet.collector 1.2.0

### Added
- Add log to tracker [#553](https://github.com/tonerdo/coverlet/pull/553)
- Exclude by assembly level System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage [#589](https://github.com/tonerdo/coverlet/pull/589)
- Allow coverlet integration with other MSBuild test strategies[#615](https://github.com/tonerdo/coverlet/pull/615) by <https://github.com/sharwell>

### Fixed

- Fix and simplify async coverage [#549](https://github.com/tonerdo/coverlet/pull/549)
- Improve lambda scenario coverage [#583](https://github.com/tonerdo/coverlet/pull/583)
- Mitigate issue in case of failure in assembly loading by cecil [#625](https://github.com/tonerdo/coverlet/pull/625)
- Fix ConfigureAwait state machine generated branches [#634](https://github.com/tonerdo/coverlet/pull/634)
- Fix coverage overwritten if the project has multiple target frameworks [#636](https://github.com/tonerdo/coverlet/issues/177)
- Fix cobertura Jenkins reporter + source link support [#614](https://github.com/tonerdo/coverlet/pull/614) by <https://github.com/daveMueller>
- Fix pdb file locking during instrumentation [#656](https://github.com/tonerdo/coverlet/pull/656)

### Improvements

- Improve exception message for unsupported runtime [#569](https://github.com/tonerdo/coverlet/pull/569) by <https://github.com/daveMueller>
- Improve cobertura absolute/relative path report generation [#661](https://github.com/tonerdo/coverlet/pull/661) by <https://github.com/daveMueller>

## Release date 2019-09-23
### Packages
coverlet.msbuild 2.7.0
coverlet.console 1.6.0
coverlet.collector 1.1.0

### Added
- Output multiple formats for vstest integration [#533](https://github.com/tonerdo/coverlet/pull/533) by <https://github.com/daveMueller>
- Different exit codes to indicate particular failures [#412](https://github.com/tonerdo/coverlet/pull/412) by <https://github.com/sasivishnu>

### Changed

- Skip instrumentation of module with embedded pdb without local sources [#510](https://github.com/tonerdo/coverlet/pull/510), with this today xunit will be skipped in automatic way.

### Fixed

- Fix exclude by files [#524](https://github.com/tonerdo/coverlet/pull/524)
- Changed to calculate based on the average coverage of the module [#479](https://github.com/tonerdo/coverlet/pull/479) by <https://github.com/dlplenin>
- Fix property attribute detection [#477](https://github.com/tonerdo/coverlet/pull/477) by <https://github.com/amweiss>
- Fix instrumentation serialization bug [#458](https://github.com/tonerdo/coverlet/pull/458)
- Fix culture for cobertura xml report [#464](https://github.com/tonerdo/coverlet/pull/464)

<!-- markdownlint-configure-file { "MD022": false, "MD024": false, "MD030": false, "MD032": false} -->
