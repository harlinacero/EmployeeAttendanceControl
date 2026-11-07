using EmployeeAttendanceControl.e2e.test.Support;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace EmployeeAttendanceControl.e2e.test.Security
{
    /// <summary>
    /// Only ms.users can issue tokens. Each check runs through the gateway and directly against every service,
    /// because each one validates tokens on its own.
    /// </summary>
    [Collection(E2ECollection.Name)]
    public class SigningTests
    {
        // The shared secret every service used to hold until this feature. It's in the Git history, so tokens made with it must fail.
        private const string RetiredSharedSecret = "491a2a0099884cb79d2cd64018c910a5asdfasdfasdfasdfasdfasdfasdf";

        private readonly E2EEnvironmentFixture _environment;

        public SigningTests(E2EEnvironmentFixture environment) => _environment = environment;

        // One protected call per entry point. The assistant is sent an empty question: a 400 proves the token was accepted, without paying for a chat.
        public static TheoryData<string, string, string, string> ProtectedCalls => new()
        {
            { "gateway", E2ESettings.BaseUrl, "GET", "/User" },
            { "ms.users", E2ESettings.UsersUrl, "GET", "/User" },
            { "ms.employees", E2ESettings.EmployeesUrl, "GET", "/Employees" },
            { "ms.attendances", E2ESettings.AttendancesUrl, "GET", "/Attendances?userName=admin" },
            { "ms.assistant", E2ESettings.AssistantUrl, "POST", "/Assistant/Chat" }
        };

        private static async Task<HttpResponseMessage> CallAsync(string baseUrl, string method, string path, string token)
        {
            using var client = new GatewayClient(baseUrl);
            return await client.SendAsync(new HttpMethod(method), path, token, method == "POST" ? new { } : null);
        }

        private static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        private static string Base64Url(object value) => Base64Url(JsonSerializer.SerializeToUtf8Bytes(value));

        private static byte[] FromBase64Url(string value)
        {
            var padded = value.Replace('-', '+').Replace('_', '/');
            return Convert.FromBase64String(padded.PadRight(padded.Length + (4 - padded.Length % 4) % 4, '='));
        }

        [Theory]
        [MemberData(nameof(ProtectedCalls))]
        public async Task ValidTokenShouldWorkThroughGatewayAndDirectly(string entryPoint, string baseUrl, string method, string path)
        {
            // Act
            var response = await CallAsync(baseUrl, method, path, _environment.AdminToken);

            // Assert
            Assert.True(response.StatusCode is not HttpStatusCode.Unauthorized and not HttpStatusCode.Forbidden,
                $"{method} {path} on {entryPoint} with a valid admin token: got {(int)response.StatusCode}");
        }

        [Theory]
        [MemberData(nameof(ProtectedCalls))]
        public async Task TokenForgedWithOldSecretShouldBeRejectedEverywhere(string entryPoint, string baseUrl, string method, string path)
        {
            // Arrange
            var header = Base64Url(new { alg = "HS256", typ = "JWT" });
            var payload = Base64Url(new { nameid = "admin", role = "admin", iss = "ms.users", exp = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds() });
            var signature = Base64Url(HMACSHA256.HashData(Encoding.UTF8.GetBytes(RetiredSharedSecret), Encoding.ASCII.GetBytes($"{header}.{payload}")));

            // Act
            var response = await CallAsync(baseUrl, method, path, $"{header}.{payload}.{signature}");

            // Assert
            Assert.True(response.StatusCode == HttpStatusCode.Unauthorized, $"{method} {path} on {entryPoint} with a token forged with the old secret: expected 401, got {(int)response.StatusCode}");
        }

        [Theory]
        [MemberData(nameof(ProtectedCalls))]
        public async Task AlteredTokenShouldBeRejectedEverywhere(string entryPoint, string baseUrl, string method, string path)
        {
            // Arrange: a regular user's real token, promoted to admin without signing it again
            var parts = _environment.RegularEmployee.Token.Split('.');
            var claims = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(FromBase64Url(parts[1]))!;
            var promoted = claims.ToDictionary(claim => claim.Key, claim => (object)claim.Value);
            promoted["role"] = "admin";
            var altered = $"{parts[0]}.{Base64Url(promoted)}.{parts[2]}";

            // Act
            var response = await CallAsync(baseUrl, method, path, altered);

            // Assert
            Assert.True(response.StatusCode == HttpStatusCode.Unauthorized, $"{method} {path} on {entryPoint} with an altered token: expected 401, got {(int)response.StatusCode}");
        }

        [Fact]
        public async Task RefreshTokenShouldNotWorkAsBearer()
        {
            // Arrange
            var tokens = await _environment.Client.LoginAsync(E2ESettings.AdminUser, E2ESettings.AdminPassword);

            // Act
            var response = await _environment.Client.SendAsync(HttpMethod.Get, "/User", tokens.RefreshToken);

            // Assert
            Assert.True(response.StatusCode == HttpStatusCode.Unauthorized, $"GET /User with a refresh token as bearer: expected 401, got {(int)response.StatusCode}");
        }
    }
}
