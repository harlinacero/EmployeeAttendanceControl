using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using ms.employees.application.Extensions;
using System.Text;

namespace ms.employees.api.Extensions
{
    public static class OpenApiExtension
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
                    Description = "Introduzca solo el token JWT. Swagger agregara el prefijo Bearer."
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
                        Title = "Employees Api",
                        Version = "v1",
                        Description = "API para la gesti�n de empleados y asistencia."
                    };

                    // Agregar esquema Bearer JWT
                    var securitySchemes = new Dictionary<string, IOpenApiSecurityScheme>
                    {
                        ["Bearer"] = new OpenApiSecurityScheme
                        {
                            Type = SecuritySchemeType.Http,
                            Scheme = "bearer",
                            In = ParameterLocation.Header,
                            BearerFormat = "JWT",
                            Name = "Authorization",
                            Description = "Cabecera de autorizaci�n JWT. \r\n Introduzca ['Bearer'] [espacio] [Token]"
                        }
                    };

                    document.Components ??= new OpenApiComponents();
                    document.Components.SecuritySchemes = securitySchemes;

                    // Requerimiento global
                    document.Security ??= [];
                    var schemeRef = new OpenApiSecuritySchemeReference("Bearer", document, null);
                    document.Security.Add(new OpenApiSecurityRequirement
                    {
                        [schemeRef] = []
                    });

                    return Task.CompletedTask;
                });
            });

            return services;
        }

        public static IServiceCollection UseAuthenticationBearer(this IServiceCollection services)
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
    }

}
