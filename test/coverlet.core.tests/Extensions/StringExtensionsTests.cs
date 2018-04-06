using System;
using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace Coverlet.Core.Tests.Extensions
{
    [ExcludeFromCodeCoverage]
    public class StringExtensionsTest
    {
        [Theory]
        [InlineData("SomeAttribute")]
        [InlineData("ExcludeFromCoverageAttribute")]
        public void RemoveAttributeSuffix__CorrectlyRemoveAttributeSuffixInTheEndOfAttributeName(string attributeName)
        {
            // Arrange
            // Act
            var clearedAttributeName = Coverlet.Core.Extensions.StringExtensions.RemoveAttributeSuffix(attributeName);
            // Assert
            var attributeWord = "Attribute";
            var attributeWordLength = attributeWord.Length;
            var validClearedAttributeName =
                attributeName.Remove(attributeName.LastIndexOf(attributeWord, StringComparison.Ordinal), attributeWordLength);
            Assert.Equal(validClearedAttributeName, clearedAttributeName);
        }

        [Theory]
        [InlineData("SomeAttributeWithSomeAdditionalSuffix")]
        [InlineData("Attribute")]
        public void RemoveAttributeSuffix__DoesNotRemoveAttributeSuffixIfItPlacedInMiddleOfWord(string word)
        {
            // Arrange
            // Act
            var clearedAttributeName = Coverlet.Core.Extensions.StringExtensions.RemoveAttributeSuffix(word);
            // Assert
            var validClearedAttributeName = word;
            Assert.Equal(validClearedAttributeName, clearedAttributeName);
        }
    }
}