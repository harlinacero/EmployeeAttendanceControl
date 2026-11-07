using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Moq;
using ms.gateway.api.Extensions;
using Microsoft.AspNetCore.Http;
using GatewayExtensions = ms.gateway.api.Extensions.Extensions;

namespace ms.gateway.api.test.Extensions
{
    public class ExtensionsTests : IDisposable
    {
        private const string TestAuthority = "http://ms.users.api";

        private readonly Mock<IHostEnvironment> _hostEnvironmentMock;
        private readonly string _contentRoot;

        public ExtensionsTests()
        {
            _contentRoot = Directory.CreateTempSubdirectory("ms.gateway.api.test-").FullName;
            File.WriteAllText(Path.Combine(_contentRoot, "ocelot.json"),
                """{ "GlobalConfiguration": { "BaseUrl": "http://localhost:9000" }, "OnlyInDefaultFile": "yes" }""");
            File.WriteAllText(Path.Combine(_contentRoot, "ocelot.Local.json"),
                """{ "GlobalConfiguration": { "BaseUrl": "http://localhost:9001" } }""");

            _hostEnvironmentMock = new Mock<IHostEnvironment>();
            _hostEnvironmentMock.SetupGet(e => e.ContentRootPath).Returns(_contentRoot);
        }

        public void Dispose()
        {
            Directory.Delete(_contentRoot, recursive: true);
            GC.SuppressFinalize(this);
        }

        private static ServiceCollection CreateServicesWithSettings()
        {
            var services = new ServiceCollection();
            services.AddOptions<SettingsOptions>().Configure(options =>
                options.Authentication = new Authentication { JWT = new JWT { Authority = TestAuthority } });
            return services;
        }

        private static ServiceCollection CreateServicesWithGatewayAuthentication()
        {
            var services = CreateServicesWithSettings();
            services.AddGatewayAuthentication();
            return services;
        }

        private static JwtBearerOptions GetBearerOptions(IServiceCollection services)
            => services.BuildServiceProvider()
                       .GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
                       .Get(GatewayExtensions.GatewayAuthenticationScheme);

        [Fact]
        public void AddOcelotRoutesShouldLoadLocalFileWhenEnvironmentIsLocal()
        {
            // Arrange
            _hostEnvironmentMock.SetupGet(e => e.EnvironmentName).Returns("Local");
            var configuration = new ConfigurationManager();

            // Act
            configuration.AddOcelotRoutes(_hostEnvironmentMock.Object);

            // Assert
            Assert.Equal("http://localhost:9001", configuration["GlobalConfiguration:BaseUrl"]);
        }

        [Fact]
        public void AddOcelotRoutesShouldLoadDefaultFileWhenNoEnvironmentFileExists()
        {
            // Arrange
            _hostEnvironmentMock.SetupGet(e => e.EnvironmentName).Returns("Development");
            var configuration = new ConfigurationManager();

            // Act
            configuration.AddOcelotRoutes(_hostEnvironmentMock.Object);

            // Assert
            Assert.Equal("http://localhost:9000", configuration["GlobalConfiguration:BaseUrl"]);
        }

        [Fact]
        public void AddOcelotRoutesShouldNotMergeBothFiles()
        {
            // Arrange
            _hostEnvironmentMock.SetupGet(e => e.EnvironmentName).Returns("Local");
            var configuration = new ConfigurationManager();

            // Act
            configuration.AddOcelotRoutes(_hostEnvironmentMock.Object);

            // Assert
            Assert.Null(configuration["OnlyInDefaultFile"]);
        }

        [Fact]
        public async Task AddGatewayAuthenticationShouldRegisterBearerScheme()
        {
            // Arrange
            var services = CreateServicesWithSettings();

            // Act
            services.AddGatewayAuthentication();

            // Assert
            var schemes = services.BuildServiceProvider().GetRequiredService<IAuthenticationSchemeProvider>();
            Assert.NotNull(await schemes.GetSchemeAsync("Bearer"));
        }

        [Fact]
        public void AddGatewayAuthenticationShouldUseConfiguredAuthority()
        {
            // Arrange
            var services = CreateServicesWithSettings();

            // Act
            services.AddGatewayAuthentication();

            // Assert
            var options = GetBearerOptions(services);
            Assert.Equal(TestAuthority, options.Authority);
            Assert.Null(options.TokenValidationParameters.IssuerSigningKey);
        }

        [Fact]
        public void AddGatewayAuthenticationShouldAllowOnlyRs256()
        {
            // Arrange
            var services = CreateServicesWithSettings();

            // Act
            services.AddGatewayAuthentication();

            // Assert
            Assert.Equal([SecurityAlgorithms.RsaSha256], GetBearerOptions(services).TokenValidationParameters.ValidAlgorithms);
        }

        [Fact]
        public void AddGatewayAuthenticationShouldValidateIssuer()
        {
            // Arrange
            var services = CreateServicesWithSettings();

            // Act
            services.AddGatewayAuthentication();

            // Assert
            var parameters = GetBearerOptions(services).TokenValidationParameters;
            Assert.True(parameters.ValidateIssuer);
            Assert.True(parameters.ValidateIssuerSigningKey);
            Assert.False(parameters.ValidateAudience);
        }

        [Fact]
        public void AddGatewayAuthenticationShouldRejectExpiredTokens()
        {
            // Arrange
            var services = CreateServicesWithSettings();

            // Act
            services.AddGatewayAuthentication();

            // Assert
            var parameters = GetBearerOptions(services).TokenValidationParameters;
            Assert.True(parameters.ValidateLifetime);
            Assert.True(parameters.RequireExpirationTime);
            Assert.Equal(TimeSpan.Zero, parameters.ClockSkew);
        }

        [Fact]
        public void AddGatewayAuthenticationShouldRejectTokensOfReplacedKeys()
        {
            // Act
            var options = GetBearerOptions(CreateServicesWithGatewayAuthentication());

            // Assert
            var manager = Assert.IsAssignableFrom<BaseConfigurationManager>(options.ConfigurationManager);
            Assert.False(manager.UseLastKnownGoodConfiguration);
        }

        [Fact]
        public void AddGatewayAuthenticationShouldRefreshKeysAfterThirtySeconds()
        {
            // Arrange
            var services = CreateServicesWithSettings();

            // Act
            services.AddGatewayAuthentication();

            // Assert
            Assert.Equal(TimeSpan.FromSeconds(30), GetBearerOptions(services).RefreshInterval);
        }

        [Fact]
        public async Task AddGatewayAuthenticationShouldFailInsteadOfThrowingWhenKeysAreUnavailable()
        {
            // Arrange
            var services = CreateServicesWithSettings();
            services.AddGatewayAuthentication();
            var options = GetBearerOptions(services);
            var context = new AuthenticationFailedContext(new DefaultHttpContext(), new AuthenticationScheme("Bearer", null, typeof(JwtBearerHandler)), options)
            {
                Exception = new InvalidOperationException("IDX20803: Unable to obtain configuration")
            };

            // Act
            await options.Events.AuthenticationFailed(context);

            // Assert
            Assert.NotNull(context.Result?.Failure);
        }

        [Fact]
        public void AddGatewayAuthenticationShouldKeepShortClaimNames()
        {
            // Arrange
            var services = CreateServicesWithSettings();

            // Act
            services.AddGatewayAuthentication();

            // Assert
            Assert.False(GetBearerOptions(services).MapInboundClaims);
        }
    }
}
