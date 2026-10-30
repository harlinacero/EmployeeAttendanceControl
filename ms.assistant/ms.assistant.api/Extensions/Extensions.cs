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
using System.Text;

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
                options.RequireHttpsMetadata = false;
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
