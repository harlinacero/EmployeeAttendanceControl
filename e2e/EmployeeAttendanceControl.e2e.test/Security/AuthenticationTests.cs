using EmployeeAttendanceControl.e2e.test.Support;
using System.Net;

namespace EmployeeAttendanceControl.e2e.test.Security
{
    [Collection(E2ECollection.Name)]
    public class AuthenticationTests
    {
        private readonly E2EEnvironmentFixture _environment;

        public AuthenticationTests(E2EEnvironmentFixture environment) => _environment = environment;

        [Fact]
        public async Task LoginShouldReturnTokenForSeededAdmin()
        {
            // Act
            var response = await _environment.Client.TryLoginAsync(E2ESettings.AdminUser, E2ESettings.AdminPassword);
            var token = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"POST /Authentication as seeded admin: expected 200, got {(int)response.StatusCode}");
            Assert.Equal(4, token.Split('.').Length);
        }

        [Fact]
        public async Task LoginShouldRejectWrongPassword()
        {
            // Act
            var response = await _environment.Client.TryLoginAsync(E2ESettings.AdminUser, "not-the-password");

            // Assert
            Assert.True(response.StatusCode == HttpStatusCode.Unauthorized, $"POST /Authentication with a wrong password: expected 401, got {(int)response.StatusCode}");
        }

        [Fact]
        public async Task LoginShouldRejectUnknownUser()
        {
            // Act
            var response = await _environment.Client.TryLoginAsync(TestData.NewUserName("unknown"), "any-password");

            // Assert
            Assert.True(response.StatusCode == HttpStatusCode.Unauthorized, $"POST /Authentication with an unknown user: expected 401, got {(int)response.StatusCode}");
        }
    }
}
