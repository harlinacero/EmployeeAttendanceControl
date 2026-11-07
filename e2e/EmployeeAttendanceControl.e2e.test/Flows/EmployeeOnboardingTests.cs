using EmployeeAttendanceControl.e2e.test.Support;
using EmployeeAttendanceControl.e2e.test.Support.Dtos;
using System.Net;

namespace EmployeeAttendanceControl.e2e.test.Flows
{
    [Collection(E2ECollection.Name)]
    public class EmployeeOnboardingTests
    {
        private readonly E2EEnvironmentFixture _environment;

        public EmployeeOnboardingTests(E2EEnvironmentFixture environment) => _environment = environment;

        [Fact]
        public async Task CreateEmployeeShouldMakeAccountUsable()
        {
            // Arrange
            var employee = new CreateEmployeeDto(TestData.NewUserName("onboard"), "Onboarding", "Check", E2EEnvironmentFixture.TestPassword, "user");

            // Act
            await _environment.Client.CreateEmployeeAsync(_environment.AdminToken, employee);

            // Assert
            var employees = await _environment.Client.GetEmployeesAsync(_environment.AdminToken);
            Assert.Contains(employees, listed => listed.UserName == employee.UserName && listed.FirstName == employee.FirstName && listed.LastName == employee.LastName);

            // No waiting: the account is created in the same operation as the employee
            var login = await _environment.Client.TryLoginAsync(employee.UserName, employee.Password);
            Assert.True(login.StatusCode == HttpStatusCode.OK, $"POST /Authentication right after creating the employee: expected 200, got {(int)login.StatusCode}");

            var users = await _environment.Client.GetUsersAsync(_environment.AdminToken);
            Assert.Contains(users, user => user.UserName == employee.UserName && user.Role == "user");
        }

        [Fact]
        public async Task CreateAccountShouldAllowSignIn()
        {
            // Arrange
            var account = new AccountDto(TestData.NewUserName("account"), E2EEnvironmentFixture.TestPassword, "user");

            // Act
            await _environment.Client.CreateAccountAsync(_environment.AdminToken, account);
            var response = await _environment.Client.TryLoginAsync(account.UserName, account.Password);

            // Assert
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"POST /Authentication as new account: expected 200, got {(int)response.StatusCode}");
        }
    }
}
