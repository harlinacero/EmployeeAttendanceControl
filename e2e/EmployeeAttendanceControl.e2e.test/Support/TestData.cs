namespace EmployeeAttendanceControl.e2e.test.Support
{
    /// <summary>
    /// Identifiers unique to this run. They are lower case because sign-in lowercases the user name before looking it up.
    /// </summary>
    public static class TestData
    {
        private static int _sequence;

        public static string RunId { get; } = $"{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..6]}";

        public static string NewUserName(string tag)
            => $"e2e-{RunId}-{tag.ToLowerInvariant()}{Interlocked.Increment(ref _sequence)}";
    }
}
