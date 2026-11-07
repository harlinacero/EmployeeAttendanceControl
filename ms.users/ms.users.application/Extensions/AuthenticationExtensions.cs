using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ms.users.application.Security;

namespace ms.users.application.Extensions
{
    public static class AuthenticationExtensions
    {
        /// <summary>
        /// ms.users validates its own tokens with the key it signs with. No HTTP call to itself.
        /// </summary>
        public static IServiceCollection UseAuthenticationBearer(this IServiceCollection services)
        {
            services.AddAuthentication(option =>
            {
                option.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                option.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer();

            services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
                .Configure<ISigningKeyProvider, IOptions<SettingsOptions>>((options, signingKeyProvider, settings) =>
                {
                    options.RequireHttpsMetadata = false;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = settings.Value.Authentication.JWT.Issuer,
                        ValidateAudience = false,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = signingKeyProvider.Key,
                        ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
                        ValidateLifetime = true,
                        RequireExpirationTime = true,
                        ClockSkew = TimeSpan.Zero,
                    };
                });

            return services;
        }
    }
}
