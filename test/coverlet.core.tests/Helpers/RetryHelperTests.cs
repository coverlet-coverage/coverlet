using System;

using Xunit;

namespace Coverlet.Core.Helpers.Tests
{
    public class RetryHelperTests
    {
        [Fact]
        public void TestRetryWithFixedRetryBackoff()
        {
            Func<TimeSpan> retryStrategy = () =>
            {
                return TimeSpan.FromMilliseconds(1);
            };

            var target = new RetryTarget();
            try
            {
                new RetryHelper().Retry(() => target.TargetActionThrows(), retryStrategy, 7);
            }
            catch
            {
                Assert.Equal(7, target.Calls);
            }
        }

        [Fact]
        public void TestRetryWithExponentialRetryBackoff()
        {
            var currentSleep = 6;
            Func<TimeSpan> retryStrategy = () =>
            {
                var sleep = TimeSpan.FromMilliseconds(currentSleep);
                currentSleep *= 2;
                return sleep;
            };

            var target = new RetryTarget();
            try
            {
                new RetryHelper().Retry(() => target.TargetActionThrows(), retryStrategy, 3);
            }
            catch
            {
                Assert.Equal(3, target.Calls);
                Assert.Equal(24, currentSleep);
            }
        }

        [Fact]
        public void TestRetryFinishesIfSuccessful()
        {
            Func<TimeSpan> retryStrategy = () =>
            {
                return TimeSpan.FromMilliseconds(1);
            };

            var target = new RetryTarget();
            new RetryHelper().Retry(() => target.TargetActionThrows5Times(), retryStrategy, 20);
            Assert.Equal(6, target.Calls);
        }

    }

    public class RetryTarget
    {
        public int Calls { get; set; }
        public void TargetActionThrows()
        {
            Calls++;
            throw new Exception("Simulating Failure");
        }
        public void TargetActionThrows5Times()
        {
            Calls++;
            if (Calls < 6) throw new Exception("Simulating Failure");
        }
    }
}