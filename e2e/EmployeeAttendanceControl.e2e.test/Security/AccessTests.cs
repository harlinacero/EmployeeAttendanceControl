using EmployeeAttendanceControl.e2e.test.Catalog;
using EmployeeAttendanceControl.e2e.test.Support;
using EmployeeAttendanceControl.e2e.test.Support.Dtos;
using System.Net;

namespace EmployeeAttendanceControl.e2e.test.Security
{
    [Collection(E2ECollection.Name)]
    public class AccessTests
    {
        private readonly E2EEnvironmentFixture _environment;

        public AccessTests(E2EEnvironmentFixture environment) => _environment = environment;

        [Theory]
        [MemberData(nameof(GatewayRoutes.Protected), MemberType = typeof(GatewayRoutes))]
        public async Task ProtectedRouteShouldRejectAnonymousCaller(string operation)
        {
            // Arrange
            var route = GatewayRoutes.Find(operation);

            // Act
            var response = await _environment.Client.SendAsync(route.Method, route.SamplePath, token: null, route.SampleBody);

            // Assert
            Assert.True(response.StatusCode == HttpStatusCode.Unauthorized, $"{operation} as anonymous caller: expected 401, got {(int)response.StatusCode}");
        }

        [Theory]
        [MemberData(nameof(GatewayRoutes.AdminOnly), MemberType = typeof(GatewayRoutes))]
        public async Task AdminRouteShouldRejectRegularUser(string operation)
        {
            // Arrange
            var route = GatewayRoutes.Find(operation);

            // Act
            var response = await _environment.Client.SendAsync(route.Method, route.SamplePath, _environment.RegularEmployee.Token, route.SampleBody);

            // Assert
            Assert.True(response.StatusCode == HttpStatusCode.Forbidden, $"{operation} as regular user: expected 403, got {(int)response.StatusCode}");
        }

        [Theory]
        [MemberData(nameof(GatewayRoutes.OpenToAnyUser), MemberType = typeof(GatewayRoutes))]
        public async Task IdentifiedRouteShouldAcceptRegularUser(string operation)
        {
            // Arrange
            var route = GatewayRoutes.Find(operation);

            // Act
            var response = await _environment.Client.SendAsync(route.Method, route.SamplePath, _environment.RegularEmployee.Token, route.SampleBody);

            // Assert
            Assert.True(response.IsSuccessStatusCode, $"{operation} as regular user: expected 2xx, got {(int)response.StatusCode}");
        }

        [Fact]
        public async Task HistoryShouldBeReadableByAnotherUser()
        {
            // Arrange
            var owner = await _environment.CreateUsableEmployeeAsync("owner");
            var added = new CreateAttendanceDto(owner.FullName, DateTime.UtcNow, true, "history of the owner");
            await _environment.Client.AddAttendanceAsync(owner.Token, added);

            // Act
            var history = await _environment.Client.GetAttendancesAsync(_environment.RegularEmployee.Token, owner.UserName);

            // Assert
            Assert.Contains(history, record => record.Notes == added.Notes);
        }
    }
}
