using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

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
                    options.Authority = authentication.JWT.Authority;
                    options.RequireHttpsMetadata = false;
                    // After a key change, an unknown key id makes it download ms.users' keys again, at most every 30 s
                    options.RefreshInterval = TimeSpan.FromSeconds(30);
                    options.MapInboundClaims = false;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = false,
                        ValidateIssuerSigningKey = true,
                        ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
                        ValidateLifetime = true,
                        RequireExpirationTime = true,
                        ClockSkew = TimeSpan.Zero
                    };
                    options.Events = new JwtBearerEvents
                    {
                        // Without this, failing to download the keys becomes a 500 instead of a rejected token
                        OnAuthenticationFailed = context =>
                        {
                            context.Fail(context.Exception);
                            return Task.CompletedTask;
                        }
                    };
                });

            // Without this, a token signed with a replaced key keeps being accepted for up to an hour,
            // validated against the previous keys ("last known good" configuration)
            services.PostConfigure<JwtBearerOptions>(GatewayAuthenticationScheme, options =>
            {
                if (options.ConfigurationManager is BaseConfigurationManager configurationManager)
                {
                    configurationManager.UseLastKnownGoodConfiguration = false;
                }
            });

            return services;
        }
    }
}
