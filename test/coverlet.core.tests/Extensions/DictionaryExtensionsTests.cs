using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Coverlet.Core.Extensions;
using Xunit;

namespace Coverlet.Core.Tests.Extensions
{
    [ExcludeFromCodeCoverage]
    public class DictionaryExtensionsTests
    {
        [Fact]
        public void TestTryAdd()
        {
            var dictionary = new Dictionary<string, string>();
            Assert.True(DictionaryExtensions.TryAdd(dictionary, "a", "b"));
            Assert.Equal("b", dictionary["a"]);
            Assert.False(DictionaryExtensions.TryAdd(dictionary, "a", "c"));
        }
    }
}