using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using ms.assistant.application.Commands.Handlers;
using ms.assistant.application.HttpComunications;
using ms.assistant.application.Tools;
using ms.assistant.infraestucture.Extensions;
using Refit;
using System.Reflection;

namespace ms.assistant.api.Extensions
{
    public static class Extensions
    {
        public static IServiceCollection AddSwaggerOpenApi(this IServiceCollection services)
        {
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(options =>
            {
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Name = "Authorization",
                    Description = "Enter only the JWT token. Swagger adds the Bearer prefix."
                });
                options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("Bearer", document, null)] = []
                });
            });
            services.AddOpenApi(options =>
            {
                options.AddDocumentTransformer((document, context, cancellationToken) =>
                {
                    document.Info = new OpenApiInfo
                    {
                        Title = "Assistant Api",
                        Version = "v1",
                        Description = "AI assistant that answers attendance questions in natural language."
                    };
                    return Task.CompletedTask;
                });
            });

            return services;
        }

        public static IServiceCollection AddAuthenticationSettings(this IServiceCollection services)
        {
            var options = services.BuildServiceProvider().GetRequiredService<IOptions<SettingsOptions>>();
            var authentication = options.Value.Authentication;

            services.AddAuthentication(option =>
            {
                option.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                option.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options =>
            {
                options.Authority = authentication.JWT.Authority;
                options.RequireHttpsMetadata = false;
                // After a key change, an unknown key id makes it download ms.users' keys again, at most every 30 s
                options.RefreshInterval = TimeSpan.FromSeconds(30);
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
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                if (options.ConfigurationManager is BaseConfigurationManager configurationManager)
                {
                    configurationManager.UseLastKnownGoodConfiguration = false;
                }
            });

            return services;
        }

        public static IServiceCollection AddAssistant(this IServiceCollection services)
        {
            var settings = services.BuildServiceProvider().GetRequiredService<IOptions<SettingsOptions>>();
            var external = settings.Value.Communication.External;

            // Service-to-service calls go directly to the services (not through the gateway), forwarding the caller's token.
            services.AddRefitClient<IAttendanceApiCommunication>().ConfigureHttpClient(c => c.BaseAddress = new Uri(external.AttendanceApiUrl));
            services.AddRefitClient<IEmployeeApiCommunication>().ConfigureHttpClient(c => c.BaseAddress = new Uri(external.EmployeeApiUrl));

            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ChatCommandHandler).GetTypeInfo().Assembly));

            services.AddScoped<IAssistantTool, GetMyAttendancesTool>();
            services.AddScoped<IAssistantTool, GetAttendancesByDateRangeTool>();
            services.AddScoped<IAssistantTool, ListEmployeesTool>();
            services.AddScoped<IAssistantToolCatalog, AssistantToolCatalog>();
            services.AddSingleton(TimeProvider.System);

            services.AddAssistantModel();

            services.AddExceptionHandler<AssistantUnavailableExceptionHandler>();
            services.AddProblemDetails();

            return services;
        }
    }
}
