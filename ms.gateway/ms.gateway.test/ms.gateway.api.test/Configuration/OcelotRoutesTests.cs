using Newtonsoft.Json;
using Ocelot.Configuration.File;

namespace ms.gateway.api.test.Configuration
{
    public class OcelotRoutesTests
    {
        private const string ContainerRoutesFile = "ocelot.json";
        private const string LocalRoutesFile = "ocelot.Local.json";

        private const string UsersService = "ms.users.api";
        private const string EmployeesService = "ms.employees.api";
        private const string AttendancesService = "ms.attendances.api";

        // Operation catalog (business spec BR-01, tech spec §3): method, path, owning service.
        private static readonly (string Method, string Path, string Service)[] Catalog =
        [
            ("POST", "/Authentication", UsersService),
            ("GET", "/User", UsersService),
            ("POST", "/User", UsersService),
            ("GET", "/Employees", EmployeesService),
            ("POST", "/Employees", EmployeesService),
            ("PUT", "/Employees/AttendanceState/{attendance}", EmployeesService),
            ("GET", "/Attendances", AttendancesService),
            ("GET", "/Attendances/ByDateRange", AttendancesService),
            ("POST", "/Attendances", AttendancesService)
        ];

        private const string BearerScheme = "Bearer";
        private static readonly string LoginRoute = Key("POST", "/Authentication");
        private static readonly string[] AdminRoutes =
        [
            Key("GET", "/User"),
            Key("POST", "/User"),
            Key("GET", "/Employees"),
            Key("POST", "/Employees"),
            Key("GET", "/Attendances/ByDateRange")
        ];
        private static readonly string[] IdentifiedUserRoutes =
        [
            Key("PUT", "/Employees/AttendanceState/{attendance}"),
            Key("GET", "/Attendances"),
            Key("POST", "/Attendances")
        ];

        private static FileConfiguration LoadRoutes(string fileName)
        {
            var path = Path.Combine(AppContext.BaseDirectory, fileName);
            Assert.True(File.Exists(path), $"Routes file '{fileName}' was not found in the test output.");
            var configuration = JsonConvert.DeserializeObject<FileConfiguration>(File.ReadAllText(path));
            Assert.NotNull(configuration);
            return configuration;
        }

        private static string Key(string method, string path) => $"{method.ToUpperInvariant()} {path}";

        private static string Key(FileRoute route) => Key(route.UpstreamHttpMethod.Single(), route.UpstreamPathTemplate);

        [Fact]
        public void RoutesShouldExposeExactlyTheCatalogOperations()
        {
            // Arrange
            var expected = Catalog.Select(operation => Key(operation.Method, operation.Path)).OrderBy(key => key);

            // Act
            var routes = LoadRoutes(ContainerRoutesFile).Routes;

            // Assert
            Assert.Equal(Catalog.Length, routes.Count);
            Assert.Equal(expected, routes.Select(Key).OrderBy(key => key));
        }

        [Fact]
        public void RoutesShouldForwardToTheSameDownstreamPath()
        {
            // Arrange & Act
            var routes = LoadRoutes(ContainerRoutesFile).Routes;

            // Assert
            Assert.All(routes, route =>
            {
                Assert.Equal(route.UpstreamPathTemplate, route.DownstreamPathTemplate);
                Assert.Equal("http", route.DownstreamScheme);
            });
        }

        [Fact]
        public void RoutesShouldTargetTheOwningServiceContainer()
        {
            // Arrange
            var expectedService = Catalog.ToDictionary(operation => Key(operation.Method, operation.Path), operation => operation.Service);

            // Act
            var routes = LoadRoutes(ContainerRoutesFile).Routes;

            // Assert
            Assert.All(routes, route =>
            {
                var hostAndPort = Assert.Single(route.DownstreamHostAndPorts);
                Assert.Equal(expectedService[Key(route)], hostAndPort.Host);
                Assert.Equal(80, hostAndPort.Port);
            });
        }

        [Fact]
        public void RoutesShouldDeclareExactlyOneMethodAndNoCatchAll()
        {
            // Arrange & Act
            var routes = LoadRoutes(ContainerRoutesFile).Routes;

            // Assert
            Assert.All(routes, route =>
            {
                Assert.Single(route.UpstreamHttpMethod);
                Assert.DoesNotContain("{everything}", route.UpstreamPathTemplate);
                Assert.False(route.UpstreamPathTemplate.StartsWith("/{"), $"Route '{route.UpstreamPathTemplate}' is a catch-all.");
            });
        }

        [Fact]
        public void RoutesShouldDeclareContainerModeBaseUrl()
        {
            // Arrange & Act
            var configuration = LoadRoutes(ContainerRoutesFile);

            // Assert
            Assert.Equal("http://localhost:9000", configuration.GlobalConfiguration.BaseUrl);
        }

        [Fact]
        public void LoginRouteShouldNotRequireAuthentication()
        {
            // Arrange & Act
            var login = LoadRoutes(ContainerRoutesFile).Routes.Single(route => Key(route) == LoginRoute);

            // Assert
            Assert.Empty(login.AuthenticationOptions?.AuthenticationProviderKeys ?? []);
            Assert.Empty(login.RouteClaimsRequirement ?? []);
        }

        [Fact]
        public void ProtectedRoutesShouldRequireBearerAuthentication()
        {
            // Arrange & Act
            var protectedRoutes = LoadRoutes(ContainerRoutesFile).Routes.Where(route => Key(route) != LoginRoute).ToList();

            // Assert
            Assert.Equal(Catalog.Length - 1, protectedRoutes.Count);
            Assert.All(protectedRoutes, route =>
                Assert.Equal([BearerScheme], route.AuthenticationOptions.AuthenticationProviderKeys));
        }

        [Fact]
        public void AdminRoutesShouldRequireAdminRole()
        {
            // Arrange & Act
            var routes = LoadRoutes(ContainerRoutesFile).Routes.Where(route => AdminRoutes.Contains(Key(route))).ToList();

            // Assert
            Assert.Equal(AdminRoutes.Length, routes.Count);
            Assert.All(routes, route =>
            {
                var requirement = Assert.Single(route.RouteClaimsRequirement);
                Assert.Equal("role", requirement.Key);
                Assert.Equal("admin", requirement.Value);
            });
        }

        [Fact]
        public void IdentifiedUserRoutesShouldNotRequireAnyClaim()
        {
            // Arrange & Act
            var routes = LoadRoutes(ContainerRoutesFile).Routes.Where(route => IdentifiedUserRoutes.Contains(Key(route))).ToList();

            // Assert
            Assert.Equal(IdentifiedUserRoutes.Length, routes.Count);
            Assert.All(routes, route => Assert.Empty(route.RouteClaimsRequirement ?? []));
        }

        [Fact]
        public void LocalRoutesShouldMatchContainerRoutesExceptHosts()
        {
            // Arrange
            var containerRoutes = LoadRoutes(ContainerRoutesFile).Routes;

            // Act
            var localRoutes = LoadRoutes(LocalRoutesFile).Routes;

            // Assert
            Assert.Equal(containerRoutes.Count, localRoutes.Count);
            for (var i = 0; i < containerRoutes.Count; i++)
            {
                var container = containerRoutes[i];
                var local = localRoutes[i];
                Assert.Equal(Key(container), Key(local));
                Assert.Equal(container.DownstreamPathTemplate, local.DownstreamPathTemplate);
                Assert.Equal(container.DownstreamScheme, local.DownstreamScheme);
                Assert.Equal(container.AuthenticationOptions?.AuthenticationProviderKeys ?? [], local.AuthenticationOptions?.AuthenticationProviderKeys ?? []);
                Assert.Equal(container.RouteClaimsRequirement ?? [], local.RouteClaimsRequirement ?? []);
            }
        }

        [Fact]
        public void LocalRoutesShouldTargetPublishedPortsOnLocalhost()
        {
            // Arrange
            var publishedPorts = new Dictionary<string, int>
            {
                [UsersService] = 9009,
                [EmployeesService] = 9010,
                [AttendancesService] = 9020
            };
            var expectedService = Catalog.ToDictionary(operation => Key(operation.Method, operation.Path), operation => operation.Service);

            // Act
            var configuration = LoadRoutes(LocalRoutesFile);

            // Assert
            Assert.Equal("http://localhost:9001", configuration.GlobalConfiguration.BaseUrl);
            Assert.All(configuration.Routes, route =>
            {
                var hostAndPort = Assert.Single(route.DownstreamHostAndPorts);
                Assert.Equal("localhost", hostAndPort.Host);
                Assert.Equal(publishedPorts[expectedService[Key(route)]], hostAndPort.Port);
            });
        }
    }
}
