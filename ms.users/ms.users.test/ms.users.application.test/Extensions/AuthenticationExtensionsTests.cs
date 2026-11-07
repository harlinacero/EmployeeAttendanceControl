using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Moq;
using ms.users.application.Extensions;
using ms.users.application.Security;
using System.Security.Cryptography;

namespace ms.users.application.test.Extensions
{
    public class AuthenticationExtensionsTests
    {
        private readonly RsaSecurityKey _key = new(RSA.Create(2048)) { KeyId = "kid-1" };

        private TokenValidationParameters GetParameters()
        {
            var keysMock = new Mock<ISigningKeyProvider>();
            keysMock.SetupGet(keys => keys.Key).Returns(_key);

            var services = new ServiceCollection();
            services.AddSingleton(TestSettings.Create());
            services.AddSingleton(keysMock.Object);
            services.UseAuthenticationBearer();

            return services.BuildServiceProvider()
                .GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
                .Get(JwtBearerDefaults.AuthenticationScheme)
                .TokenValidationParameters;
        }

        [Fact]
        public void UseAuthenticationBearerShouldValidateWithLocalPublicKey()
        {
            // Act
            var parameters = GetParameters();

            // Assert
            Assert.Same(_key, parameters.IssuerSigningKey);
            Assert.True(parameters.ValidateIssuer);
            Assert.Equal(TestSettings.Issuer, parameters.ValidIssuer);
            Assert.True(parameters.ValidateLifetime);
            Assert.Equal(TimeSpan.Zero, parameters.ClockSkew);
        }

        [Fact]
        public void UseAuthenticationBearerShouldAllowOnlyRs256()
        {
            // Act
            var parameters = GetParameters();

            // Assert
            Assert.Equal([SecurityAlgorithms.RsaSha256], parameters.ValidAlgorithms);
        }
    }
}
