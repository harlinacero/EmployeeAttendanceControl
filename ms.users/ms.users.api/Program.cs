using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using ms.rabbitmq.Consumers;
using ms.rabbitmq.Middlewares;
using ms.users.api.Consumers;
using ms.users.api.Mappers;
using ms.users.application.Mappers;
using ms.users.application.Queries.Handlers;
using ms.users.domain.Interfaces;
using ms.users.infraestructure.Data;
using ms.users.infraestructure.Mappings;
using ms.users.infraestructure.Repositories;
using System.Reflection;
using System.Text;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddSingleton(typeof(CassandraUserMapping));

builder.Services.AddScoped(typeof(CassandraCluster));
builder.Services.AddTransient(typeof(CassandraCluster));

builder.Services.AddTransient(typeof(IUsersContext), typeof(UsersContext));
builder.Services.AddTransient(typeof(IUserRepository), typeof(UserRepository));

builder.Services.AddAutoMapper(mapperConfig =>
{
    mapperConfig.AddMaps(typeof(UsersMapperProfile).Assembly);
    mapperConfig.AddProfile(typeof(EventMapperProfile));
});

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(GetAllUsersQueryHandler).GetTypeInfo().Assembly);
});
builder.Services.AddSingleton(typeof(IConsumer), typeof(UserConsumer));

var privateKey = builder.Configuration.GetValue<string>("Authentication:JWT:Key");
builder.Services.AddAuthentication(option =>
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
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(privateKey)),
        ValidateLifetime = true,
        RequireExpirationTime = true,
        ClockSkew = TimeSpan.Zero
    };
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
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
builder.Services.AddOpenApi(options =>
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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});

var consumer = app.Services.GetRequiredService<IConsumer>();
app.UseRabbitConsumer(consumer);

app.MapOpenApi();
app.UseSwaggerUI(c => c.SwaggerEndpoint("../swagger/v1/swagger.json", "Users Authentication API v1"));
app.UseHttpsRedirection();

app.Run();
