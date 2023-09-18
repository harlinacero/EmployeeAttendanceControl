using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
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

builder.Services.AddScoped(typeof(IUsersContext), typeof(UsersContext));
builder.Services.AddScoped(typeof(IUserRepository), typeof(UserRepository));
builder.Services.AddTransient(typeof(IUsersContext), typeof(UsersContext));
builder.Services.AddTransient(typeof(IUserRepository), typeof(UserRepository));

var automapperConfig = new MapperConfiguration(mapperConfig =>
{
    mapperConfig.AddMaps(typeof(UsersMapperProfile).Assembly);
    mapperConfig.AddProfile(typeof(EventMapperProfile));
});
IMapper mapper = automapperConfig.CreateMapper();
builder.Services.AddSingleton(mapper);

builder.Services.AddMediatR(typeof(GetAllUsersQueryHandler).GetTypeInfo().Assembly);
builder.Services.AddSingleton<IConsumer, UserConsumer>();

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
builder.Services.AddSwaggerGen(swagger =>
{
    swagger.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Users Authentication Api",
        Version = "v1"
    });
    swagger.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
    {
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Name = "Authorization",
        Description = "Cabecera de autorización JWT. \r\n Introduzca ['Bearer'] [espacio] [Token]"
    });
    swagger.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            }, new string[] {}
        }
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

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Users Authentication API v1"));

app.UseHttpsRedirection();


//app.MapControllers();

app.Run();
