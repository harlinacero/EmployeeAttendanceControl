using EmployeeAttendanceControl.e2e.test.Support;
using EmployeeAttendanceControl.e2e.test.Support.Dtos;
using System.Net;
using System.Net.Http.Json;

namespace EmployeeAttendanceControl.e2e.test.Security
{
    /// <summary>
    /// Sessions: renewing with single-use refresh tokens, detecting their reuse, and signing out of one session.
    /// </summary>
    [Collection(E2ECollection.Name)]
    public class SessionTests
    {
        private readonly E2EEnvironmentFixture _environment;

        public SessionTests(E2EEnvironmentFixture environment) => _environment = environment;

        private GatewayClient Client => _environment.Client;

        private async Task<(TestEmployee Employee, TokenPairDto Tokens)> SignInNewEmployeeAsync(string tag)
        {
            var employee = await _environment.CreateUsableEmployeeAsync(tag);
            return (employee, await Client.LoginAsync(employee.UserName, E2EEnvironmentFixture.TestPassword));
        }

        private async Task AssertRefreshRejectedAsync(string? refreshToken, string what)
        {
            var response = await Client.TryRefreshAsync(refreshToken);
            Assert.True(response.StatusCode == HttpStatusCode.Unauthorized, $"POST /Authentication/Refresh with {what}: expected 401, got {(int)response.StatusCode}");
        }

        private async Task AssertAccessTokenWorksAsync(TestEmployee employee, string accessToken)
        {
            var response = await Client.SendAsync(HttpMethod.Get, $"/Attendances?userName={employee.UserName}", accessToken);
            Assert.True(response.IsSuccessStatusCode, $"GET /Attendances with the access token: expected 2xx, got {(int)response.StatusCode}");
        }

        [Fact]
        public async Task RefreshShouldRotateTokens()
        {
            // Arrange
            var (employee, tokens) = await SignInNewEmployeeAsync("rotate");

            // Act
            var renewed = await Client.RefreshAsync(tokens.RefreshToken);
            var renewedAgain = await Client.RefreshAsync(renewed.RefreshToken);

            // Assert
            Assert.NotEqual(tokens.RefreshToken, renewed.RefreshToken);
            Assert.NotEqual(renewed.RefreshToken, renewedAgain.RefreshToken);
            await AssertAccessTokenWorksAsync(employee, renewed.AccessToken);
            await AssertAccessTokenWorksAsync(employee, renewedAgain.AccessToken);
        }

        [Fact]
        public async Task SecondLoginShouldKeepFirstSessionUsable()
        {
            // Arrange
            var (employee, first) = await SignInNewEmployeeAsync("twosessions");

            // Act
            var second = await Client.LoginAsync(employee.UserName, E2EEnvironmentFixture.TestPassword);

            // Assert
            await Client.RefreshAsync(first.RefreshToken);
            await Client.RefreshAsync(second.RefreshToken);
        }

        [Fact]
        public async Task RefreshWithUnknownTokenShouldBeRejected()
        {
            // Act & Assert
            await AssertRefreshRejectedAsync("not-a-refresh-token", "an unknown token");
            await AssertRefreshRejectedAsync(null, "no token");
        }

        [Fact]
        public async Task ReusedTokenShouldRevokeOnlyItsSession()
        {
            // Arrange
            var (employee, stolen) = await SignInNewEmployeeAsync("reuse");
            var otherSession = await Client.LoginAsync(employee.UserName, E2EEnvironmentFixture.TestPassword);
            var legitimate = await Client.RefreshAsync(stolen.RefreshToken);

            // Act
            await AssertRefreshRejectedAsync(stolen.RefreshToken, "a spent token");

            // Assert
            await AssertRefreshRejectedAsync(legitimate.RefreshToken, "the newest token of a revoked session");
            await Client.RefreshAsync(otherSession.RefreshToken);
        }

        [Fact]
        public async Task ConcurrentRefreshShouldSucceedAtMostOnce()
        {
            // Arrange
            var (_, tokens) = await SignInNewEmployeeAsync("concurrent");

            // Act
            var responses = await Task.WhenAll(Client.TryRefreshAsync(tokens.RefreshToken), Client.TryRefreshAsync(tokens.RefreshToken));

            // Assert
            var succeeded = responses.Where(response => response.StatusCode == HttpStatusCode.OK).ToList();
            Assert.True(succeeded.Count <= 1, $"Two simultaneous renewals with the same token: {succeeded.Count} succeeded");
            Assert.All(responses.Except(succeeded), response => Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode));
            foreach (var response in succeeded)
            {
                var issued = await response.Content.ReadFromJsonAsync<TokenPairDto>();
                await AssertRefreshRejectedAsync(issued!.RefreshToken, "a token of a session revoked for reuse");
            }
        }

        [Fact]
        public async Task LogoutShouldEndOnlyCurrentSession()
        {
            // Arrange
            var (employee, current) = await SignInNewEmployeeAsync("logout");
            var other = await Client.LoginAsync(employee.UserName, E2EEnvironmentFixture.TestPassword);

            // Act
            var response = await Client.TryLogoutAsync(current.RefreshToken);

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            await AssertRefreshRejectedAsync(current.RefreshToken, "the token of a signed-out session");
            await Client.RefreshAsync(other.RefreshToken);
            await AssertAccessTokenWorksAsync(employee, current.AccessToken);
        }

        [Fact]
        public async Task LogoutWithUnknownTokenShouldSucceed()
        {
            // Act
            var response = await Client.TryLogoutAsync("not-a-refresh-token");

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task LogoutWithSpentTokenShouldEndSession()
        {
            // Arrange
            var (_, tokens) = await SignInNewEmployeeAsync("logoutspent");
            var renewed = await Client.RefreshAsync(tokens.RefreshToken);

            // Act
            var response = await Client.TryLogoutAsync(tokens.RefreshToken);

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            await AssertRefreshRejectedAsync(renewed.RefreshToken, "the newest token of a signed-out session");
        }

        [Fact]
        public async Task LogoutWithoutTokenShouldBeRejectedAsInvalid()
        {
            // Act
            var response = await Client.SendAsync(HttpMethod.Post, "/Authentication/Logout", token: null, new { });

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task AccessTokenShouldNotRenew()
        {
            // Arrange
            var (_, tokens) = await SignInNewEmployeeAsync("accessasrefresh");

            // Act & Assert
            await AssertRefreshRejectedAsync(tokens.AccessToken, "an access token");
        }
    }
}
