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

        // The services themselves, published by docker-compose.e2e.yml to prove that each one validates tokens on its own
        public static readonly string UsersUrl = Read("E2E_USERS_URL", "http://localhost:19009");
        public static readonly string EmployeesUrl = Read("E2E_EMPLOYEES_URL", "http://localhost:19010");
        public static readonly string AttendancesUrl = Read("E2E_ATTENDANCES_URL", "http://localhost:19020");
        public static readonly string AssistantUrl = Read("E2E_ASSISTANT_URL", "http://localhost:19030");

        private static string Read(string name, string defaultValue)
            => Environment.GetEnvironmentVariable(name) is { Length: > 0 } value ? value : defaultValue;
    }
}
