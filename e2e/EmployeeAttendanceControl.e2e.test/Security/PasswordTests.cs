using EmployeeAttendanceControl.e2e.test.Support;
using EmployeeAttendanceControl.e2e.test.Support.Dtos;
using System.Net;

namespace EmployeeAttendanceControl.e2e.test.Security
{
    /// <summary>
    /// Passwords are never returned, and accounts are created together with their employee.
    /// </summary>
    [Collection(E2ECollection.Name)]
    public class PasswordTests
    {
        private readonly E2EEnvironmentFixture _environment;

        public PasswordTests(E2EEnvironmentFixture environment) => _environment = environment;

        private GatewayClient Client => _environment.Client;

        private async Task AssertCannotSignInAsync(string userName, string password, string what)
        {
            var response = await Client.TryLoginAsync(userName, password);
            Assert.True(response.StatusCode == HttpStatusCode.Unauthorized, $"POST /Authentication {what}: expected 401, got {(int)response.StatusCode}");
        }

        [Fact]
        public async Task UserListShouldNotExposePasswords()
        {
            // Act
            var response = await Client.SendAsync(HttpMethod.Get, "/User", _environment.AdminToken);
            var body = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.DoesNotContain("password", body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("pbkdf2", body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(E2EEnvironmentFixture.TestPassword, body);
        }

        [Fact]
        public async Task AccountWithoutPasswordShouldBeRejected()
        {
            // Arrange
            var userName = TestData.NewUserName("nopassaccount");

            // Act
            var response = await Client.SendAsync(HttpMethod.Post, "/User", _environment.AdminToken, new { userName, role = "user" });

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var users = await Client.GetUsersAsync(_environment.AdminToken);
            Assert.DoesNotContain(users, user => user.UserName == userName);
        }

        [Fact]
        public async Task EmployeeWithoutPasswordShouldBeRejected()
        {
            // Arrange
            var userName = TestData.NewUserName("nopassemployee");

            // Act
            var response = await Client.SendAsync(HttpMethod.Post, "/Employees", _environment.AdminToken,
                new { userName, firstName = "No", lastName = "Password", role = "user" });

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var employees = await Client.GetEmployeesAsync(_environment.AdminToken);
            Assert.DoesNotContain(employees, employee => employee.UserName == userName);
        }

        [Fact]
        public async Task EmployeeForExistingAccountShouldKeepItsPassword()
        {
            // Arrange
            var userName = TestData.NewUserName("existing");
            await Client.CreateAccountAsync(_environment.AdminToken, new AccountDto(userName, "first-password", "user"));

            // Act
            await Client.CreateEmployeeAsync(_environment.AdminToken, new CreateEmployeeDto(userName, "Existing", "Account", "second-password", "user"));

            // Assert
            var employees = await Client.GetEmployeesAsync(_environment.AdminToken);
            Assert.Contains(employees, employee => employee.UserName == userName);
            await Client.LoginAsync(userName, "first-password");
            await AssertCannotSignInAsync(userName, "second-password", "with the password given when creating the employee");
        }

        [Fact]
        public async Task RegularUserShouldNotCreateEmployeeOrAccount()
        {
            // Arrange
            var userName = TestData.NewUserName("forbidden");

            // Act
            var response = await Client.SendAsync(HttpMethod.Post, "/Employees", _environment.RegularEmployee.Token,
                new CreateEmployeeDto(userName, "Not", "Allowed", E2EEnvironmentFixture.TestPassword, "user"));

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            await AssertCannotSignInAsync(userName, E2EEnvironmentFixture.TestPassword, "as a user whose creation was forbidden");
            var employees = await Client.GetEmployeesAsync(_environment.AdminToken);
            Assert.DoesNotContain(employees, employee => employee.UserName == userName);
        }
    }
}
