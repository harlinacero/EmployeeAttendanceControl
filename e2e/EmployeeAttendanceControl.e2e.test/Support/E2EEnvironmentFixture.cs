using EmployeeAttendanceControl.e2e.test.Support.Dtos;
using System.Net;

namespace EmployeeAttendanceControl.e2e.test.Support
{
    /// <summary>
    /// Once per run: signs in the seeded admin and creates the regular employee shared by the access checks.
    /// If either fails, every check in the collection fails with that reason, and none is reported as passed.
    /// </summary>
    public class E2EEnvironmentFixture : IAsyncLifetime
    {
        public const string TestPassword = "e2e-password";

        public GatewayClient Client { get; } = new(E2ESettings.BaseUrl);

        public string AdminToken { get; private set; } = string.Empty;

        public TestEmployee RegularEmployee { get; private set; } = null!;

        public async Task InitializeAsync()
        {
            var login = await Client.TryLoginAsync(E2ESettings.AdminUser, E2ESettings.AdminPassword);
            if (login.StatusCode != HttpStatusCode.OK)
            {
                throw new InvalidOperationException($"Seeded admin could not sign in: {(int)login.StatusCode}");
            }

            AdminToken = await login.Content.ReadAsStringAsync();
            RegularEmployee = await CreateUsableEmployeeAsync("regular");
        }

        public Task DisposeAsync()
        {
            Client.Dispose();
            return Task.CompletedTask;
        }

        public async Task<TestEmployee> CreateUsableEmployeeAsync(string tag)
        {
            var userName = TestData.NewUserName(tag);
            await Client.CreateEmployeeAsync(AdminToken, new CreateEmployeeDto(userName, "Test", tag, TestPassword, "user"));

            await WaitUntilAsync(
                () => Client.TryLoginAsync(userName, TestPassword),
                response => response.StatusCode == HttpStatusCode.OK,
                $"The account of new employee '{userName}' (created from EmployeeCreateEvent)");

            return new TestEmployee(userName, "Test", tag, await Client.LoginAsync(userName, TestPassword));
        }

        public Task<T> WaitUntilAsync<T>(Func<Task<T>> probe, Func<T, bool> done, string what)
            => Eventually.UntilAsync(probe, done, what, E2ESettings.EffectTimeout, E2ESettings.PollInterval);
    }
}
