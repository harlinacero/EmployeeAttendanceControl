namespace EmployeeAttendanceControl.e2e.test.Support
{
    /// <summary>
    /// Run settings, set by run-e2e.ps1. The defaults allow running the tests against an e2e stack that is already up.
    /// </summary>
    public static class E2ESettings
    {
        public static readonly string BaseUrl = Read("E2E_BASE_URL", "http://localhost:19000");
        public static readonly string AdminUser = Read("E2E_ADMIN_USER", "admin");
        public static readonly string AdminPassword = Read("E2E_ADMIN_PASSWORD", "1234");
        public static readonly TimeSpan EffectTimeout = TimeSpan.FromSeconds(int.Parse(Read("E2E_EFFECT_TIMEOUT_SECONDS", "60")));
        public static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);

        private static string Read(string name, string defaultValue)
            => Environment.GetEnvironmentVariable(name) is { Length: > 0 } value ? value : defaultValue;
    }
}
