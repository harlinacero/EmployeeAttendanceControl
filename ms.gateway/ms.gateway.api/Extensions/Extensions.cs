using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace ms.gateway.api.Extensions
{
    public static class Extensions
    {
        public const string OcelotRoutesFile = "ocelot.json";
        public const string GatewayAuthenticationScheme = "Bearer";

        public static IConfigurationManager AddOcelotRoutes(this IConfigurationManager configuration, IHostEnvironment environment)
        {
            var environmentRoutesFile = $"ocelot.{environment.EnvironmentName}.json";
            var routesFile = File.Exists(Path.Combine(environment.ContentRootPath, environmentRoutesFile))
                ? environmentRoutesFile
                : OcelotRoutesFile;

            configuration.AddJsonFile(Path.Combine(environment.ContentRootPath, routesFile), optional: false, reloadOnChange: true);
            return configuration;
        }

        public static IServiceCollection AddGatewayAuthentication(this IServiceCollection services)
        {
            var options = services.BuildServiceProvider().GetRequiredService<IOptions<SettingsOptions>>();
            var authentication = options.Value.Authentication;

            services.AddAuthentication()
                .AddJwtBearer(GatewayAuthenticationScheme, options =>
                {
                    options.RequireHttpsMetadata = false;
                    options.MapInboundClaims = false;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = false,
                        ValidateAudience = false,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(authentication.JWT.Key)),
                        ValidateLifetime = true,
                        RequireExpirationTime = true,
                        ClockSkew = TimeSpan.Zero
                    };
                });

            return services;
        }
    }
}
