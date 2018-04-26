using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Bogus;
using Xunit;
using Coverlet.Core.Helpers;

namespace Coverlet.Core.Helpers.Tests
{
    [ExcludeFromCodeCoverage]
    public class RetryHelperTests
    {
        private readonly Faker _faker = new Faker();

        [Fact]
        public void Retry__WithFixedRetryBackoff__CallsEqualsMaxAttemptsCount()
        {
            // Arrange
            var maxAttemptCount = _faker.Random.Int(min: 1, max: 30);
            Func<TimeSpan> retryStrategy = () => TimeSpan.FromMilliseconds(1);
            var target = new RetryTarget();
            try
            {
                // Act
                RetryHelper.Retry(() => target.TargetActionThrows(), retryStrategy, maxAttemptCount);
            }
            catch
            {
                // Assert
                Assert.Equal(maxAttemptCount, target.Calls);
            }
        }

        [Fact]
        public void Retry__WithExponentialRetryBackoff__CurrentSleepEquals24IfMaxAttemptsCountEquals3()
        {
            // Arrange
            var currentSleep = 6;
            var maxAttemptCount = 3;
            Func<TimeSpan> retryStrategy = () => {
                var sleep = TimeSpan.FromMilliseconds(currentSleep);
                currentSleep *= 2;
                return sleep;
            };
            var target = new RetryTarget();
            try 
            {
                // Act
                RetryHelper.Retry(() => target.TargetActionThrows(), retryStrategy, maxAttemptCount);
            }
            catch
            {
                // Assert
                var validSleepsCount = 24;
                Assert.Equal(validSleepsCount, currentSleep);
            }
        }

        [Fact]
        public void Retry__RetryingFinishesIfSuccessful()
        {
            // Arrange
            var maxAttemptCount = _faker.Random.Int(min: 6, max: 20);
            Func<TimeSpan> retryStrategy = () => TimeSpan.FromMilliseconds(1);
            var target = new RetryTarget();
            // Act
            RetryHelper.Retry(() => target.TargetActionThrows5Times(), retryStrategy, maxAttemptCount);
            // Assert
            Assert.Equal(6, target.Calls);
        }
    }
}