using EmployeeAttendanceControl.e2e.test.Support;
using EmployeeAttendanceControl.e2e.test.Support.Dtos;
using System.Net;
using System.Net.Http.Json;

namespace EmployeeAttendanceControl.e2e.test.Security
{
    [Collection(E2ECollection.Name)]
    public class AuthenticationTests
    {
        private readonly E2EEnvironmentFixture _environment;

        public AuthenticationTests(E2EEnvironmentFixture environment) => _environment = environment;

        [Fact]
        public async Task LoginShouldReturnTokenPairForSeededAdmin()
        {
            // Act
            var response = await _environment.Client.TryLoginAsync(E2ESettings.AdminUser, E2ESettings.AdminPassword);

            // Assert
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"POST /Authentication as seeded admin: expected 200, got {(int)response.StatusCode}");
            var tokens = await response.Content.ReadFromJsonAsync<TokenPairDto>();
            Assert.NotNull(tokens);
            Assert.Equal(3, tokens.AccessToken.Split('.').Length);
            Assert.False(string.IsNullOrEmpty(tokens.RefreshToken));
            Assert.Equal(900, tokens.ExpiresIn);
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

        [Fact]
        public async Task LoginShouldRejectMissingDataLikeWrongPassword()
        {
            // Arrange
            var wrongPassword = await _environment.Client.TryLoginAsync(E2ESettings.AdminUser, "not-the-password");

            // Act
            var missingPassword = await _environment.Client.SendAsync(HttpMethod.Post, "/Authentication", token: null, new { userName = E2ESettings.AdminUser });

            // Assert
            Assert.True(missingPassword.StatusCode == HttpStatusCode.Unauthorized, $"POST /Authentication without password: expected 401, got {(int)missingPassword.StatusCode}");
            Assert.Equal(await wrongPassword.Content.ReadAsStringAsync(), await missingPassword.Content.ReadAsStringAsync());
        }
    }
}
