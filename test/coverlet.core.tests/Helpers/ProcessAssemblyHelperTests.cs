// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Coverlet.Core.Helpers;
using Xunit;

namespace Coverlet.Core.Tests.Helpers
{
  public class ProcessAssemblyHelperTests
  {
    private readonly ProcessAssemblyHelper _sut = new();

    [Fact]
    public void WhenGetLoadedAssemblyNamesThenReturnsNonEmptyList()
    {
      IReadOnlyList<string> result = _sut.GetLoadedAssemblyNames("SomeOtherAssembly");

      Assert.NotEmpty(result);
    }

    [Fact]
    public void WhenGetLoadedAssemblyNamesThenExcludesTestAssembly()
    {
      string testAssemblyName = "coverlet.core.tests";

      IReadOnlyList<string> result = _sut.GetLoadedAssemblyNames(testAssemblyName);

      Assert.DoesNotContain(testAssemblyName, result, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void WhenGetLoadedAssemblyNamesThenExcludesTestAssemblyCaseInsensitive()
    {
      IReadOnlyList<string> result = _sut.GetLoadedAssemblyNames("COVERLET.CORE.TESTS");

      Assert.DoesNotContain("coverlet.core.tests", result, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void WhenGetLoadedAssemblyNamesThenNoDuplicates()
    {
      IReadOnlyList<string> result = _sut.GetLoadedAssemblyNames("SomeOtherAssembly");

      var unique = new HashSet<string>(result, StringComparer.OrdinalIgnoreCase);
      Assert.Equal(unique.Count, result.Count);
    }

    [Fact]
    public void WhenGetLoadedAssemblyNamesThenAllNamesAreNonEmpty()
    {
      IReadOnlyList<string> result = _sut.GetLoadedAssemblyNames("SomeOtherAssembly");

      Assert.All(result, name => Assert.False(string.IsNullOrWhiteSpace(name)));
    }

    [Fact]
    public void WhenGetLoadedAssemblyNamesWithEmptyStringThenThrowsArgumentException()
    {
      Assert.Throws<ArgumentException>(() => _sut.GetLoadedAssemblyNames(string.Empty));
    }

    [Fact]
    public void WhenGetLoadedAssemblyNamesWithWhitespaceThenThrowsArgumentException()
    {
      Assert.Throws<ArgumentException>(() => _sut.GetLoadedAssemblyNames("   "));
    }

    [Theory]
    // Single segment — exact
    [InlineData("xunit", "[xunit]*")]
    // Two segments — exact
    [InlineData("ReportGenerator.Mtp", "[ReportGenerator.Mtp]*")]
    // Two segments — exact
    [InlineData("coverlet.core", "[coverlet.core]*")]
    // Three segments — wildcard from third onwards
    [InlineData("xunit.v3.mtp-v2", "[xunit.v3.*]*")]
    [InlineData("xunit.runner.utility", "[xunit.runner.*]*")]
    [InlineData("Microsoft.Testing.Platform", "[Microsoft.Testing.*]*")]
    // Four segments — still uses first-two-segment prefix
    [InlineData("Microsoft.Extensions.Configuration.Binder", "[Microsoft.Extensions.*]*")]
    public void WhenToExcludeFilterThenReturnsCorrectPattern(string assemblyName, string expectedFilter)
    {
      string result = ProcessAssemblyHelper.ToExcludeFilter(assemblyName);

      Assert.Equal(expectedFilter, result);
    }

    [Theory]
    // An exact filter that is already covered by a wildcard should be pruned.
    [InlineData(new[] { "[coverlet.*]*", "[coverlet.core]*" }, new[] { "[coverlet.*]*" })]
    // A wildcard is not pruned by itself.
    [InlineData(new[] { "[coverlet.*]*" }, new[] { "[coverlet.*]*" })]
    // An exact filter not covered by any wildcard is kept.
    [InlineData(new[] { "[coverlet.*]*", "[Serilog]*" }, new[] { "[coverlet.*]*", "[Serilog]*" })]
    // Deeper exact filter covered by parent wildcard.
    [InlineData(new[] { "[Microsoft.Extensions.*]*", "[Microsoft.Extensions.Logging]*" }, new[] { "[Microsoft.Extensions.*]*" })]
    // Exact filter where no matching wildcard exists is kept.
    [InlineData(new[] { "[xunit.v3.*]*", "[System.Runtime]*" }, new[] { "[xunit.v3.*]*", "[System.Runtime]*" })]
    public void WhenPruneRedundantFiltersThenRemovesExactFiltersCoveredByWildcard(string[] input, string[] expected)
    {
      IEnumerable<string> result = ProcessAssemblyHelper.PruneRedundantFilters(input);

      Assert.Equal(expected, result);
    }

    [Fact]
    public void WhenGetLoadedAssemblyNamesThenContainsKnownAssembly()
    {
      // Derive the expected name from a type that is definitely in the test process,
      // so this test stays valid if the xUnit package or assembly name ever changes.
      string expectedAssemblyName = typeof(FactAttribute).Assembly.GetName().Name!;

      IReadOnlyList<string> result = _sut.GetLoadedAssemblyNames("SomeOtherAssembly");

      Assert.Contains(expectedAssemblyName, result, StringComparer.OrdinalIgnoreCase);
    }
  }
}
