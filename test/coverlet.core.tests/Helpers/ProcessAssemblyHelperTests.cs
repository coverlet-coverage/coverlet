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
    [InlineData("xunit.runner.utility", "[xunit.runner.utility]*")]
    [InlineData("ReportGenerator.Mtp", "[ReportGenerator.Mtp]*")]
    [InlineData("Microsoft.Testing.Platform", "[Microsoft.Testing.Platform]*")]
    public void WhenToExcludeFilterThenReturnsCorrectPattern(string assemblyName, string expectedFilter)
    {
      string result = ProcessAssemblyHelper.ToExcludeFilter(assemblyName);

      Assert.Equal(expectedFilter, result);
    }

    [Fact]
    public void WhenGetLoadedAssemblyNamesThenContainsKnownAssembly()
    {
      // xunit itself must be loaded in the test process
      IReadOnlyList<string> result = _sut.GetLoadedAssemblyNames("SomeOtherAssembly");

      Assert.Contains("xunit.v3.core", result, StringComparer.OrdinalIgnoreCase);
    }
  }
}
