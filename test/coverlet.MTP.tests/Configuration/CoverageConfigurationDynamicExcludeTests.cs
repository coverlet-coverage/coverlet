// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Coverlet.Core.Abstractions;
using Coverlet.MTP.CommandLine;
using Microsoft.Testing.Platform.CommandLine;
using Moq;
using Xunit;

namespace Coverlet.MTP.Configuration.Tests;

/// <summary>
/// Tests for the dynamic exclude-filter behaviour introduced by IProcessAssemblyHelper injection.
/// </summary>
public sealed class CoverageConfigurationDynamicExcludeTests
{
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

  #region Priority 3: Dynamic defaults

  [Fact]
  public void WhenNoConfigFileThenDynamicFiltersApplied()
  {
    _mockProcessAssemblyHelper
      .Setup(x => x.GetLoadedAssemblyNames("MyTests"))
      .Returns(["xunit.core", "ReportGenerator.Mtp"]);

    var config = new CoverageConfiguration(
      _mockCommandLineOptions.Object,
      configFileSettings: null,
      testAssemblyName: "MyTests",
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
      .Setup(x => x.GetLoadedAssemblyNames(It.IsAny<string>()))
      .Returns(["xunit.core"]);

    var config = new CoverageConfiguration(
      _mockCommandLineOptions.Object,
      configFileSettings: null,
      testAssemblyName: "MyTests",
      logger: _mockLogger.Object,
      processAssemblyHelper: _mockProcessAssemblyHelper.Object);

    string[] result = config.GetExcludeFilters();

    Assert.Contains("[coverlet.*]*", result);
  }

  [Fact]
  public void WhenDynamicHelperReturnsEmptyListThenBaselineFallbackUsed()
  {
    _mockProcessAssemblyHelper
      .Setup(x => x.GetLoadedAssemblyNames(It.IsAny<string>()))
      .Returns([]);

    var config = new CoverageConfiguration(
      _mockCommandLineOptions.Object,
      configFileSettings: null,
      testAssemblyName: "MyTests",
      logger: _mockLogger.Object,
      processAssemblyHelper: _mockProcessAssemblyHelper.Object);

    string[] result = config.GetExcludeFilters();

    Assert.Equal(["[coverlet.*]*"], result);
  }

  [Fact]
  public void WhenDynamicHelperThrowsThenBaselineFallbackUsed()
  {
    _mockProcessAssemblyHelper
      .Setup(x => x.GetLoadedAssemblyNames(It.IsAny<string>()))
      .Throws(new InvalidOperationException("Simulated reflection failure"));

    var config = new CoverageConfiguration(
      _mockCommandLineOptions.Object,
      configFileSettings: null,
      testAssemblyName: "MyTests",
      logger: _mockLogger.Object,
      processAssemblyHelper: _mockProcessAssemblyHelper.Object);

    string[] result = config.GetExcludeFilters();

    Assert.Equal(["[coverlet.*]*"], result);
  }

  [Fact]
  public void WhenDynamicHelperReturnsAssemblyNamesThenFiltersHaveCorrectFormat()
  {
    _mockProcessAssemblyHelper
      .Setup(x => x.GetLoadedAssemblyNames("MyTests"))
      .Returns(["some.assembly", "another.lib"]);

    var config = new CoverageConfiguration(
      _mockCommandLineOptions.Object,
      configFileSettings: null,
      testAssemblyName: "MyTests",
      logger: _mockLogger.Object,
      processAssemblyHelper: _mockProcessAssemblyHelper.Object);

    string[] result = config.GetExcludeFilters();

    Assert.Contains("[some.assembly]*", result);
    Assert.Contains("[another.lib]*", result);
  }

  [Fact]
  public void WhenNoConfigFileAndNullTestAssemblyNameThenBaselineOnlyReturned()
  {
    // When testAssemblyName is null the helper is not called at all
    var config = new CoverageConfiguration(
      _mockCommandLineOptions.Object,
      configFileSettings: null,
      testAssemblyName: null,
      logger: _mockLogger.Object,
      processAssemblyHelper: _mockProcessAssemblyHelper.Object);

    string[] result = config.GetExcludeFilters();

    Assert.Equal(["[coverlet.*]*"], result);
    _mockProcessAssemblyHelper.Verify(
      x => x.GetLoadedAssemblyNames(It.IsAny<string>()),
      Times.Never);
  }

  [Fact]
  public void WhenDynamicFiltersNoDuplicatesThenResultContainsBaselineOnce()
  {
    // If a loaded assembly happens to be named "coverlet.*" it must not duplicate the baseline
    _mockProcessAssemblyHelper
      .Setup(x => x.GetLoadedAssemblyNames("MyTests"))
      .Returns(["coverlet.core", "xunit.core"]);

    var config = new CoverageConfiguration(
      _mockCommandLineOptions.Object,
      configFileSettings: null,
      testAssemblyName: "MyTests",
      logger: _mockLogger.Object,
      processAssemblyHelper: _mockProcessAssemblyHelper.Object);

    string[] result = config.GetExcludeFilters();

    // "[coverlet.*]*" baseline must not be duplicated by "[coverlet.core]*"
    Assert.Single(result, f => f == "[coverlet.*]*");
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
      testAssemblyName: "MyTests",
      logger: _mockLogger.Object,
      processAssemblyHelper: _mockProcessAssemblyHelper.Object);

    config.GetExcludeFilters();

    // IProcessAssemblyHelper must NOT be called when a config file governs the filters
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
      testAssemblyName: "MyTests",
      logger: _mockLogger.Object,
      processAssemblyHelper: _mockProcessAssemblyHelper.Object);

    string[] result = config.GetExcludeFilters();

    Assert.Equal(configFilters, result);
  }

  #endregion

  #region Priority 1: CLI flag bypasses dynamic list

  [Fact]
  public void WhenCliExcludeFlagSetThenBaselineIsMergedNotDynamic()
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
      testAssemblyName: "MyTests",
      logger: _mockLogger.Object,
      processAssemblyHelper: _mockProcessAssemblyHelper.Object);

    config.GetExcludeFilters();

    // Dynamic helper must NOT be called when CLI flag is explicitly set
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
      testAssemblyName: "MyTests",
      logger: _mockLogger.Object,
      processAssemblyHelper: _mockProcessAssemblyHelper.Object);

    string[] result = config.GetExcludeFilters();

    Assert.Contains("[coverlet.*]*", result);
    Assert.Contains("[MyCustomFilter]*", result);
  }

  #endregion

  // Helper delegate for Moq setup - must return bool to match TryGetOptionArgumentList signature
  private delegate bool TryGetOptionArgumentListDelegate(string optionName, out string[]? value);
}
