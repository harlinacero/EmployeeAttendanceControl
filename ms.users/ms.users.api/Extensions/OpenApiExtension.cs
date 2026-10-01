using Microsoft.OpenApi;

namespace ms.users.api.Extensions
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
                        Title = "Users Authentication Api",
                        Version = "v1",
                        Description = "API de autenticaci�n de usuarios. Proporciona endpoints para el registro, inicio de sesi�n y gesti�n de usuarios."
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

                    var schemeRef = new OpenApiSecuritySchemeReference("Bearer", document, null);
                    var securityRequirement = new OpenApiSecurityRequirement
                    {
                        [schemeRef] = []
                    };
                    document.Security =
                    [
                        securityRequirement
                    ];

                    return Task.CompletedTask;
                });
            });

            return services;
        }
    }

}
