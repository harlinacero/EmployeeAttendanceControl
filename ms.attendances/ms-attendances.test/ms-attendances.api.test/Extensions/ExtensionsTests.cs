using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ms.attendances.api.Extensions;
using ms.attendances.application.Extensions;

namespace ms_attendances.api.test.Extensions
{
    public class ExtensionsTests
    {
        private const string TestAuthority = "http://ms.users.api";

        private static JwtBearerOptions GetBearerOptions()
        {
            var services = new ServiceCollection();
            services.AddOptions<SettingsOptions>().Configure(options =>
                options.Authentication = new Authentication { JWT = new JWT { Authority = TestAuthority } });
            services.AddAuthenticationSettings();

            return services.BuildServiceProvider()
                .GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
                .Get(JwtBearerDefaults.AuthenticationScheme);
        }

        [Fact]
        public void AddAuthenticationSettingsShouldUseConfiguredAuthority()
        {
            // Act
            var options = GetBearerOptions();

            // Assert
            Assert.Equal(TestAuthority, options.Authority);
            Assert.Null(options.TokenValidationParameters.IssuerSigningKey);
        }

        [Fact]
        public void AddAuthenticationSettingsShouldAllowOnlyRs256()
        {
            // Act
            var options = GetBearerOptions();

            // Assert
            Assert.Equal([SecurityAlgorithms.RsaSha256], options.TokenValidationParameters.ValidAlgorithms);
        }

        [Fact]
        public void AddAuthenticationSettingsShouldValidateIssuerAndLifetime()
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
        public void AddAuthenticationSettingsShouldRejectTokensOfReplacedKeys()
        {
            // Act
            var options = GetBearerOptions();

            // Assert
            var manager = Assert.IsAssignableFrom<BaseConfigurationManager>(options.ConfigurationManager);
            Assert.False(manager.UseLastKnownGoodConfiguration);
        }

        [Fact]
        public void AddAuthenticationSettingsShouldRefreshKeysAfterThirtySeconds()
        {
            // Act
            var options = GetBearerOptions();

            // Assert
            Assert.Equal(TimeSpan.FromSeconds(30), options.RefreshInterval);
        }

        [Fact]
        public async Task AddAuthenticationSettingsShouldFailInsteadOfThrowingWhenKeysAreUnavailable()
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
