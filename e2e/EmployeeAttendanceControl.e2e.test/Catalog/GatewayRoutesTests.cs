using System.Text.Json;

namespace EmployeeAttendanceControl.e2e.test.Catalog
{
    public class GatewayRoutesTests
    {
        [Fact]
        public void CatalogShouldMatchGatewayRoutes()
        {
            // Arrange
            using var ocelot = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "ocelot.json")));
            var exposed = ocelot.RootElement.GetProperty("Routes").EnumerateArray()
                .SelectMany(route => route.GetProperty("UpstreamHttpMethod").EnumerateArray()
                    .Select(method => $"{method.GetString()!.ToUpperInvariant()} {route.GetProperty("UpstreamPathTemplate").GetString()}"))
                .ToHashSet();

            // Act
            var catalog = GatewayRoutes.All.Select(route => route.Operation).ToHashSet();

            // Assert
            var withoutCheck = exposed.Except(catalog).ToList();
            var notExposed = catalog.Except(exposed).ToList();
            Assert.True(withoutCheck.Count == 0 && notExposed.Count == 0,
                $"Exposed without a check: [{string.Join(", ", withoutCheck)}]. In the catalog but not exposed: [{string.Join(", ", notExposed)}].");
        }
    }
}
