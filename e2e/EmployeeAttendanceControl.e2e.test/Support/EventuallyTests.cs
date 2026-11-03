using System.Diagnostics;

namespace EmployeeAttendanceControl.e2e.test.Support
{
    public class EventuallyTests
    {
        private static readonly TimeSpan Interval = TimeSpan.FromMilliseconds(10);

        [Fact]
        public async Task UntilAsyncShouldReturnValueWhenProbeIsDone()
        {
            // Arrange
            static Task<int> Probe() => Task.FromResult(42);

            // Act
            var result = await Eventually.UntilAsync(Probe, value => value == 42, "the value", TimeSpan.FromSeconds(5), Interval);

            // Assert
            Assert.Equal(42, result);
        }

        [Fact]
        public async Task UntilAsyncShouldKeepPollingUntilDone()
        {
            // Arrange
            var calls = 0;
            Task<int> Probe() => Task.FromResult(++calls);

            // Act
            var result = await Eventually.UntilAsync(Probe, value => value == 3, "the third call", TimeSpan.FromSeconds(5), Interval);

            // Assert
            Assert.Equal(3, result);
            Assert.Equal(3, calls);
        }

        [Fact]
        public async Task UntilAsyncShouldThrowNamingTheEffectWhenTimeoutPasses()
        {
            // Arrange
            var timeout = TimeSpan.FromMilliseconds(200);
            var stopwatch = Stopwatch.StartNew();

            // Act
            var exception = await Assert.ThrowsAsync<TimeoutException>(() =>
                Eventually.UntilAsync(() => Task.FromResult(false), done => done, "the account of user x", timeout, Interval));

            // Assert
            Assert.Contains("the account of user x", exception.Message);
            Assert.InRange(stopwatch.Elapsed, timeout, TimeSpan.FromSeconds(5));
        }
    }
}
