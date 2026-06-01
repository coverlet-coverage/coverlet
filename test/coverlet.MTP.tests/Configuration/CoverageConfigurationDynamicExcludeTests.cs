// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Coverlet.Core.Abstractions;
using Coverlet.MTP.CommandLine;
using Microsoft.Testing.Platform.CommandLine;
using Moq;
using Xunit;

namespace Coverlet.MTP.Configuration.Tests;

/// <summary>
/// Tests for the dynamic exclude-filter behaviour introduced by IProcessAssemblyHelper injection.
/// Dynamic defaults are built from the test module's deps.json rather than from controller-process
/// assemblies, so test-host infrastructure loaded after the controller starts is also excluded.
/// </summary>
public sealed class CoverageConfigurationDynamicExcludeTests
{
  // Fake module path used across tests — directory portion must exist conceptually for deps.json look-up.
  private const string FakeModulePath = "/fake/path/MyTests.dll";
  // Derived at runtime so the separator always matches what Path.GetDirectoryName produces on the current OS.
  private static readonly string s_fakeModuleDir = Path.GetDirectoryName(FakeModulePath)!;
  private const string TestAssemblyName = "MyTests";

  private readonly Mock<Microsoft.Testing.Platform.Logging.ILogger> _mockLogger;
  private readonly Mock<ICommandLineOptions> _mockCommandLineOptions;
  private readonly Mock<IProcessAssemblyHelper> _mockProcessAssemblyHelper;

  public CoverageConfigurationDynamicExcludeTests()
  {
    _mockCommandLineOptions = new Mock<ICommandLineOptions>();
    _mockLogger = new Mock<Microsoft.Testing.Platform.Logging.ILogger>();
    _mockProcessAssemblyHelper = new Mock<IProcessAssemblyHelper>();

    // Default: no CLI --exclude flag
    _mockCommandLineOptions
      .Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.Exclude, out It.Ref<string[]?>.IsAny))
#pragma warning disable IDE0350 // Use implicitly typed lambda
      .Returns(new TryGetOptionArgumentListDelegate((string _, out string[]? v) => { v = null; return false; }));
#pragma warning restore IDE0350 // Use implicitly typed lambda
  }

  #region Priority 3: Dynamic defaults from deps.json

  [Fact]
  public void WhenNoConfigFileThenDepsJsonFiltersApplied()
  {
    _mockProcessAssemblyHelper
      .Setup(x => x.GetDepsJsonAssemblyNames(s_fakeModuleDir, TestAssemblyName))
      .Returns(["xunit.core", "ReportGenerator.Mtp"]);
    _mockProcessAssemblyHelper
      .Setup(x => x.GetLoadedAssemblyNames(TestAssemblyName))
      .Returns([]);

    var config = new CoverageConfiguration(
      _mockCommandLineOptions.Object,
      configFileSettings: null,
      testModulePath: FakeModulePath,
      logger: _mockLogger.Object,
      processAssemblyHelper: _mockProcessAssemblyHelper.Object);

    string[] result = config.GetExcludeFilters();

    Assert.Contains("[xunit.core]*", result);
    Assert.Contains("[ReportGenerator.Mtp]*", result);
  }

  [Fact]
  public void WhenNoConfigFileThenBaselineFilterAlwaysPresent()
  {
    _mockProcessAssemblyHelper
      .Setup(x => x.GetDepsJsonAssemblyNames(It.IsAny<string>(), It.IsAny<string>()))
      .Returns(["xunit.core"]);
    _mockProcessAssemblyHelper
      .Setup(x => x.GetLoadedAssemblyNames(It.IsAny<string>()))
      .Returns([]);

    var config = new CoverageConfiguration(
      _mockCommandLineOptions.Object,
      configFileSettings: null,
      testModulePath: FakeModulePath,
      logger: _mockLogger.Object,
      processAssemblyHelper: _mockProcessAssemblyHelper.Object);

    string[] result = config.GetExcludeFilters();

    Assert.Contains("[coverlet.*]*", result);
  }

  [Fact]
  public void WhenDepsJsonHelperReturnsEmptyListThenBaselineFallbackUsed()
  {
    _mockProcessAssemblyHelper
      .Setup(x => x.GetDepsJsonAssemblyNames(It.IsAny<string>(), It.IsAny<string>()))
      .Returns([]);
    _mockProcessAssemblyHelper
      .Setup(x => x.GetLoadedAssemblyNames(It.IsAny<string>()))
      .Returns([]);

    var config = new CoverageConfiguration(
      _mockCommandLineOptions.Object,
      configFileSettings: null,
      testModulePath: FakeModulePath,
      logger: _mockLogger.Object,
      processAssemblyHelper: _mockProcessAssemblyHelper.Object);

    string[] result = config.GetExcludeFilters();

    Assert.Equal(["[coverlet.*]*", "[Microsoft.VisualStudio.TestPlatform.*]*", "[testhost*]*"], result);
  }

  [Fact]
  public void WhenDepsJsonHelperThrowsThenBaselineFallbackUsed()
  {
    // Throwing from GetDepsJsonAssemblyNames propagates out of the try block,
    // so GetLoadedAssemblyNames is never reached — no extra stub needed.
    _mockProcessAssemblyHelper
      .Setup(x => x.GetDepsJsonAssemblyNames(It.IsAny<string>(), It.IsAny<string>()))
      .Throws(new InvalidOperationException("Simulated deps.json read failure"));

    var config = new CoverageConfiguration(
      _mockCommandLineOptions.Object,
      configFileSettings: null,
      testModulePath: FakeModulePath,
      logger: _mockLogger.Object,
      processAssemblyHelper: _mockProcessAssemblyHelper.Object);

    string[] result = config.GetExcludeFilters();

    Assert.Equal(["[coverlet.*]*", "[Microsoft.VisualStudio.TestPlatform.*]*", "[testhost*]*"], result);
  }

  [Fact]
  public void WhenDepsJsonHelperReturnsAssemblyNamesThenFiltersHaveCorrectFormat()
  {
    _mockProcessAssemblyHelper
      .Setup(x => x.GetDepsJsonAssemblyNames(s_fakeModuleDir, TestAssemblyName))
      .Returns(["some.assembly", "another.lib"]);
    _mockProcessAssemblyHelper
      .Setup(x => x.GetLoadedAssemblyNames(TestAssemblyName))
      .Returns([]);

    var config = new CoverageConfiguration(
      _mockCommandLineOptions.Object,
      configFileSettings: null,
      testModulePath: FakeModulePath,
      logger: _mockLogger.Object,
      processAssemblyHelper: _mockProcessAssemblyHelper.Object);

    string[] result = config.GetExcludeFilters();

    Assert.Contains("[some.assembly]*", result);
    Assert.Contains("[another.lib]*", result);
  }

  [Fact]
  public void WhenNullTestModulePathThenBaselineOnlyReturned()
  {
    // When testModulePath is null the helper must not be called at all
    var config = new CoverageConfiguration(
      _mockCommandLineOptions.Object,
      configFileSettings: null,
      testModulePath: null,
      logger: _mockLogger.Object,
      processAssemblyHelper: _mockProcessAssemblyHelper.Object);

    string[] result = config.GetExcludeFilters();

    Assert.Equal(["[coverlet.*]*", "[Microsoft.VisualStudio.TestPlatform.*]*", "[testhost*]*"], result);
    _mockProcessAssemblyHelper.Verify(
      x => x.GetDepsJsonAssemblyNames(It.IsAny<string>(), It.IsAny<string>()),
      Times.Never);
    _mockProcessAssemblyHelper.Verify(
      x => x.GetLoadedAssemblyNames(It.IsAny<string>()),
      Times.Never);
  }

  [Fact]
  public void WhenDynamicFiltersNoDuplicatesThenResultContainsBaselineOnce()
  {
    // A deps.json entry named "coverlet.*" must not duplicate the baseline "[coverlet.*]*" entry
    _mockProcessAssemblyHelper
      .Setup(x => x.GetDepsJsonAssemblyNames(s_fakeModuleDir, TestAssemblyName))
      .Returns(["coverlet.core", "xunit.core"]);
    _mockProcessAssemblyHelper
      .Setup(x => x.GetLoadedAssemblyNames(TestAssemblyName))
      .Returns([]);

    var config = new CoverageConfiguration(
      _mockCommandLineOptions.Object,
      configFileSettings: null,
      testModulePath: FakeModulePath,
      logger: _mockLogger.Object,
      processAssemblyHelper: _mockProcessAssemblyHelper.Object);

    string[] result = config.GetExcludeFilters();

    // [coverlet.core] maps to [coverlet.core]* (two segments, kept exact),
    // but it is pruned as redundant because [coverlet.*]* (the baseline) already covers it.
    // [xunit.core] maps to [xunit.core]* (two segments, kept exact).
    Assert.Single(result, f => f == "[coverlet.*]*");
    Assert.DoesNotContain("[coverlet.core]*", result);
    Assert.Contains("[xunit.core]*", result);
  }

  [Fact]
  public void WhenLoadedAssemblyReturnsEntryThenFilterApplied()
  {
    // Validates that controller-process assemblies (e.g. build extensions) are also excluded.
    _mockProcessAssemblyHelper
      .Setup(x => x.GetDepsJsonAssemblyNames(s_fakeModuleDir, TestAssemblyName))
      .Returns([]);
    _mockProcessAssemblyHelper
      .Setup(x => x.GetLoadedAssemblyNames(TestAssemblyName))
      .Returns(["Microsoft.Testing.Extensions.MSBuild"]);

    var config = new CoverageConfiguration(
      _mockCommandLineOptions.Object,
      configFileSettings: null,
      testModulePath: FakeModulePath,
      logger: _mockLogger.Object,
      processAssemblyHelper: _mockProcessAssemblyHelper.Object);

    string[] result = config.GetExcludeFilters();

    // Three-segment name → wildcarded to [Microsoft.Testing.*]*
    Assert.Contains("[Microsoft.Testing.*]*", result);
  }

  [Fact]
  public void WhenBothSourcesReturnEntriesThenAllFiltersApplied()
  {
    // Validates the union of deps.json + AppDomain sources.
    _mockProcessAssemblyHelper
      .Setup(x => x.GetDepsJsonAssemblyNames(s_fakeModuleDir, TestAssemblyName))
      .Returns(["xunit.v3.core", "testhost"]);
    _mockProcessAssemblyHelper
      .Setup(x => x.GetLoadedAssemblyNames(TestAssemblyName))
      .Returns(["Microsoft.Testing.Extensions.MSBuild"]);

    var config = new CoverageConfiguration(
      _mockCommandLineOptions.Object,
      configFileSettings: null,
      testModulePath: FakeModulePath,
      logger: _mockLogger.Object,
      processAssemblyHelper: _mockProcessAssemblyHelper.Object);

    string[] result = config.GetExcludeFilters();

    // Three-segment names are wildcarded to their two-segment prefix.
    Assert.Contains("[xunit.v3.*]*", result);
    Assert.Contains("[testhost]*", result);
    Assert.Contains("[Microsoft.Testing.*]*", result);
  }

  #endregion

  #region Priority 2: Config file bypasses dynamic list

  [Fact]
  public void WhenConfigFilePresentThenDynamicFiltersNotApplied()
  {
    var configFileSettings = new CoverletMTPSettings
    {
      IsFromConfigFile = true,
      ExcludeFilters = ["[coverlet.*]*", "[MyApp.Tests]*"]
    };

    var config = new CoverageConfiguration(
      _mockCommandLineOptions.Object,
      configFileSettings,
      testModulePath: FakeModulePath,
      logger: _mockLogger.Object,
      processAssemblyHelper: _mockProcessAssemblyHelper.Object);

    config.GetExcludeFilters();

    // IProcessAssemblyHelper must NOT be called when a config file governs the filters
    _mockProcessAssemblyHelper.Verify(
      x => x.GetDepsJsonAssemblyNames(It.IsAny<string>(), It.IsAny<string>()),
      Times.Never);
    _mockProcessAssemblyHelper.Verify(
      x => x.GetLoadedAssemblyNames(It.IsAny<string>()),
      Times.Never);
  }

  [Fact]
  public void WhenConfigFilePresentThenConfigFileFiltersReturned()
  {
    string[] configFilters = ["[coverlet.*]*", "[MyApp.Tests]*"];
    var configFileSettings = new CoverletMTPSettings
    {
      IsFromConfigFile = true,
      ExcludeFilters = configFilters
    };

    var config = new CoverageConfiguration(
      _mockCommandLineOptions.Object,
      configFileSettings,
      testModulePath: FakeModulePath,
      logger: _mockLogger.Object,
      processAssemblyHelper: _mockProcessAssemblyHelper.Object);

    string[] result = config.GetExcludeFilters();

    Assert.Equal(configFilters, result);
  }

  #endregion

  #region Priority 1: CLI flag bypasses dynamic list

  [Fact]
  public void WhenCliExcludeFlagSetThenDepsJsonHelperNotCalled()
  {
    string[] cliFilters = ["[MyCustomFilter]*"];
#pragma warning disable IDE0350 // Use implicitly typed lambda
    _mockCommandLineOptions
      .Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.Exclude, out It.Ref<string[]?>.IsAny))
      .Returns(new TryGetOptionArgumentListDelegate((string _, out string[]? v) =>
      {
        v = cliFilters;
        return true;
      }));
#pragma warning restore IDE0350 // Use implicitly typed lambda

    var config = new CoverageConfiguration(
      _mockCommandLineOptions.Object,
      configFileSettings: null,
      testModulePath: FakeModulePath,
      logger: _mockLogger.Object,
      processAssemblyHelper: _mockProcessAssemblyHelper.Object);

    config.GetExcludeFilters();

    // Dynamic helper must NOT be called when CLI flag is explicitly set
    _mockProcessAssemblyHelper.Verify(
      x => x.GetDepsJsonAssemblyNames(It.IsAny<string>(), It.IsAny<string>()),
      Times.Never);
    _mockProcessAssemblyHelper.Verify(
      x => x.GetLoadedAssemblyNames(It.IsAny<string>()),
      Times.Never);
  }

  [Fact]
  public void WhenCliExcludeFlagSetThenBaselineFilterIsAlwaysPresent()
  {
    string[] cliFilters = ["[MyCustomFilter]*"];
#pragma warning disable IDE0350 // Use implicitly typed lambda
    _mockCommandLineOptions
      .Setup(x => x.TryGetOptionArgumentList(CoverletOptionNames.Exclude, out It.Ref<string[]?>.IsAny))
      .Returns(new TryGetOptionArgumentListDelegate((string _, out string[]? v) =>
      {
        v = cliFilters;
        return true;
      }));
#pragma warning restore IDE0350 // Use implicitly typed lambda

    var config = new CoverageConfiguration(
      _mockCommandLineOptions.Object,
      configFileSettings: null,
      testModulePath: FakeModulePath,
      logger: _mockLogger.Object,
      processAssemblyHelper: _mockProcessAssemblyHelper.Object);

    string[] result = config.GetExcludeFilters();

    Assert.Contains("[coverlet.*]*", result);
    Assert.Contains("[MyCustomFilter]*", result);
  }

  #endregion

  // Helper delegate for Moq setup — must return bool to match TryGetOptionArgumentList signature
  private delegate bool TryGetOptionArgumentListDelegate(string optionName, out string[]? value);
}

