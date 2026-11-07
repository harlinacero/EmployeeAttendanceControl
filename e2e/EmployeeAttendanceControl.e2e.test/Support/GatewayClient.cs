using EmployeeAttendanceControl.e2e.test.Support.Dtos;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace EmployeeAttendanceControl.e2e.test.Support
{
    /// <summary>
    /// Calls the system the way a client does: through the gateway, with no reference to any service.
    /// Created with a service's URL instead, it calls that service directly.
    /// Typed helpers throw when the call fails; SendAsync and the Try methods return the response for negative checks.
    /// </summary>
    public class GatewayClient : IDisposable
    {
        private readonly HttpClient _httpClient;

        public GatewayClient(string baseUrl)
        {
            _httpClient = new HttpClient { BaseAddress = new Uri(baseUrl), Timeout = TimeSpan.FromMinutes(2) };
        }

        public static string ByDateRangePath(string userName, DateOnly from, DateOnly to)
            => $"/Attendances/ByDateRange?userName={Uri.EscapeDataString(userName)}&from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}";

        public Task<HttpResponseMessage> TryLoginAsync(string userName, string password)
            => SendAsync(HttpMethod.Post, "/Authentication", token: null, new { userName, password });

        public async Task<TokenPairDto> LoginAsync(string userName, string password)
        {
            var response = await TryLoginAsync(userName, password);
            await EnsureSuccessAsync(response, $"POST /Authentication as '{userName}'");
            return (await response.Content.ReadFromJsonAsync<TokenPairDto>())!;
        }

        public Task<HttpResponseMessage> TryRefreshAsync(string? refreshToken)
            => SendAsync(HttpMethod.Post, "/Authentication/Refresh", token: null, new { refreshToken });

        public async Task<TokenPairDto> RefreshAsync(string refreshToken)
        {
            var response = await TryRefreshAsync(refreshToken);
            await EnsureSuccessAsync(response, "POST /Authentication/Refresh");
            return (await response.Content.ReadFromJsonAsync<TokenPairDto>())!;
        }

        public Task<HttpResponseMessage> TryLogoutAsync(string? refreshToken)
            => SendAsync(HttpMethod.Post, "/Authentication/Logout", token: null, new { refreshToken });

        public async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, string? token = null, object? body = null)
        {
            using var request = new HttpRequestMessage(method, path);
            if (token is not null)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            if (body is not null)
            {
                request.Content = JsonContent.Create(body, body.GetType());
            }

            return await _httpClient.SendAsync(request);
        }

        public Task CreateEmployeeAsync(string token, CreateEmployeeDto employee)
            => SendExpectingSuccessAsync(HttpMethod.Post, "/Employees", token, employee);

        public Task<List<EmployeeDto>> GetEmployeesAsync(string token)
            => ReadAsync<List<EmployeeDto>>(HttpMethod.Get, "/Employees", token);

        public Task<List<UserDto>> GetUsersAsync(string token)
            => ReadAsync<List<UserDto>>(HttpMethod.Get, "/User", token);

        public Task CreateAccountAsync(string token, AccountDto account)
            => SendExpectingSuccessAsync(HttpMethod.Post, "/User", token, account);

        public Task MarkAttendanceAsync(string token, bool attendance, string notes)
            => SendExpectingSuccessAsync(HttpMethod.Put, $"/Employees/AttendanceState/{attendance.ToString().ToLowerInvariant()}", token, notes);

        public Task<List<AttendanceDto>> GetAttendancesAsync(string token, string userName)
            => ReadAsync<List<AttendanceDto>>(HttpMethod.Get, $"/Attendances?userName={Uri.EscapeDataString(userName)}", token);

        public Task<List<AttendanceDto>> GetAttendancesByDateRangeAsync(string token, string userName, DateOnly from, DateOnly to)
            => ReadAsync<List<AttendanceDto>>(HttpMethod.Get, ByDateRangePath(userName, from, to), token);

        public Task AddAttendanceAsync(string token, CreateAttendanceDto attendance)
            => SendExpectingSuccessAsync(HttpMethod.Post, "/Attendances", token, attendance);

        public Task<ChatAnswerDto> ChatAsync(string token, string question)
            => ReadAsync<ChatAnswerDto>(HttpMethod.Post, "/Assistant/Chat", token, new ChatDto(question));

        public void Dispose() => _httpClient.Dispose();

        private async Task SendExpectingSuccessAsync(HttpMethod method, string path, string token, object body)
        {
            var response = await SendAsync(method, path, token, body);
            await EnsureSuccessAsync(response, $"{method} {path}");
        }

        private async Task<T> ReadAsync<T>(HttpMethod method, string path, string token, object? body = null)
        {
            var response = await SendAsync(method, path, token, body);
            await EnsureSuccessAsync(response, $"{method} {path}");
            return (await response.Content.ReadFromJsonAsync<T>())!;
        }

        private static async Task EnsureSuccessAsync(HttpResponseMessage response, string operation)
        {
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"{operation}: expected 2xx, got {(int)response.StatusCode}. Body: {body}");
            }
        }
    }
}
