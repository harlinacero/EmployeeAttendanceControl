using System.Diagnostics;

namespace EmployeeAttendanceControl.e2e.test.Support
{
    /// <summary>
    /// Waits for an effect that arrives with a delay, because services talk to each other through messages.
    /// </summary>
    public static class Eventually
    {
        public static async Task<T> UntilAsync<T>(Func<Task<T>> probe, Func<T, bool> done, string what, TimeSpan timeout, TimeSpan interval)
        {
            var stopwatch = Stopwatch.StartNew();

            while (true)
            {
                var value = await probe();
                if (done(value))
                {
                    return value;
                }

                if (stopwatch.Elapsed >= timeout)
                {
                    throw new TimeoutException($"{what} did not happen within {timeout}");
                }

                await Task.Delay(interval);
            }
        }
    }
}
