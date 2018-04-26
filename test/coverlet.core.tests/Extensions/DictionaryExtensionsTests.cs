using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Bogus;
using Coverlet.Core.Extensions;
using Xunit;

namespace Coverlet.Core.Tests.Extensions
{
    [ExcludeFromCodeCoverage]
    public class DictionaryExtensionsTests
    {
        private readonly Faker _faker = new Faker();

        [Fact]
        public void TryAdd__AtFirstTime__ReturnTrue()
        {
            // Arrange
            var key = _faker.Random.Word();
            var value = _faker.Random.Word();
            var dictionary = new Dictionary<string, string>();
            // Act
            var wasAdded = DictionaryExtensions.TryAdd(dictionary, key, value);
            // Assert
            Assert.True(wasAdded);
        }
        [Fact]
        public void TryAdd_AtFirstTime__AddValueForKey()
        {
            // Arrange
            var key = _faker.Random.Word();
            var value = _faker.Random.Word();
            var dictionary = new Dictionary<string, string>();
            // Act
            DictionaryExtensions.TryAdd(dictionary, key, value);
            // Assert
            Assert.Equal(value, dictionary[key]);
        }
        [Fact]
        public void TryAdd__AtSecondTime__ReturnFalse()
        {
            // Arrange
            var key = _faker.Random.Word();
            var value = _faker.Random.Word();
            var dictionary = new Dictionary<string, string>();
            DictionaryExtensions.TryAdd(dictionary, key, value);
            // Act
            var wasAdded = DictionaryExtensions.TryAdd(dictionary, key, value);
            // Assert
            Assert.False(wasAdded);
        }
    }
}