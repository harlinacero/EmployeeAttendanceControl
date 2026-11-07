using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ms.employees.api.Extensions;
using ms.employees.application.Extensions;

namespace ms.employees.api.test.Extensions
{
    public class OpenApiExtensionTests
    {
        private const string TestAuthority = "http://ms.users.api";

        private static JwtBearerOptions GetBearerOptions()
        {
            var services = new ServiceCollection();
            services.AddOptions<SettingsOptions>().Configure(options =>
                options.Authentication = new Authentication { JWT = new JWT { Authority = TestAuthority } });
            services.UseAuthenticationBearer();

            return services.BuildServiceProvider()
                .GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
                .Get(JwtBearerDefaults.AuthenticationScheme);
        }

        [Fact]
        public void UseAuthenticationBearerShouldUseConfiguredAuthority()
        {
            // Act
            var options = GetBearerOptions();

            // Assert
            Assert.Equal(TestAuthority, options.Authority);
            Assert.Null(options.TokenValidationParameters.IssuerSigningKey);
        }

        [Fact]
        public void UseAuthenticationBearerShouldAllowOnlyRs256()
        {
            // Act
            var options = GetBearerOptions();

            // Assert
            Assert.Equal([SecurityAlgorithms.RsaSha256], options.TokenValidationParameters.ValidAlgorithms);
        }

        [Fact]
        public void UseAuthenticationBearerShouldValidateIssuerAndLifetime()
        {
            // Act
            var parameters = GetBearerOptions().TokenValidationParameters;

            // Assert
            Assert.True(parameters.ValidateIssuer);
            Assert.True(parameters.ValidateIssuerSigningKey);
            Assert.True(parameters.ValidateLifetime);
            Assert.True(parameters.RequireExpirationTime);
            Assert.Equal(TimeSpan.Zero, parameters.ClockSkew);
        }

        [Fact]
        public void UseAuthenticationBearerShouldRejectTokensOfReplacedKeys()
        {
            // Act
            var options = GetBearerOptions();

            // Assert
            var manager = Assert.IsAssignableFrom<BaseConfigurationManager>(options.ConfigurationManager);
            Assert.False(manager.UseLastKnownGoodConfiguration);
        }

        [Fact]
        public void UseAuthenticationBearerShouldRefreshKeysAfterThirtySeconds()
        {
            // Act
            var options = GetBearerOptions();

            // Assert
            Assert.Equal(TimeSpan.FromSeconds(30), options.RefreshInterval);
        }

        [Fact]
        public async Task UseAuthenticationBearerShouldFailInsteadOfThrowingWhenKeysAreUnavailable()
        {
            // Arrange
            var options = GetBearerOptions();
            var context = new AuthenticationFailedContext(new DefaultHttpContext(), new AuthenticationScheme("Bearer", null, typeof(JwtBearerHandler)), options)
            {
                Exception = new InvalidOperationException("IDX20803: Unable to obtain configuration")
            };

            // Act
            await options.Events.AuthenticationFailed(context);

            // Assert
            Assert.NotNull(context.Result?.Failure);
        }
    }
}
