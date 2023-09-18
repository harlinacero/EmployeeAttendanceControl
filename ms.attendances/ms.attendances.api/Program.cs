using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ms.attendances.api.Consumers;
using ms.attendances.api.Mappers;
using ms.attendances.application.Commands;
using ms.attendances.application.Mappers;
using ms.attendances.domain.Repositories;
using ms.attendances.infraestucture.Data;
using ms.attendances.infraestucture.Mappers;
using ms.attendances.infraestucture.Repositories;
using ms.rabbitmq.Consumers;
using ms.rabbitmq.Middlewares;
using System.Reflection;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddScoped(typeof(IAttendanceContext), typeof(AttendanceMongoContext));
builder.Services.AddTransient(typeof(IAttendanceContext), typeof(AttendanceMongoContext));
builder.Services.AddScoped(typeof(IAttendanceRepository), typeof(AttendanceRepository));
builder.Services.AddTransient(typeof(IAttendanceRepository), typeof(AttendanceRepository));

var automapperConfig = new MapperConfiguration(mapperConfig =>
{
    mapperConfig.AddMaps(typeof(AttendanceProfile).Assembly);
    mapperConfig.AddMaps(typeof(AttendanceMongoProfile).Assembly);
    mapperConfig.AddProfile(typeof(EventMapperProfile));
});
IMapper mapper = automapperConfig.CreateMapper();
builder.Services.AddSingleton(mapper);

builder.Services.AddMediatR(typeof(CreateAttendanceCommand).GetTypeInfo().Assembly);
builder.Services.AddSingleton(typeof(IConsumer), typeof(AttendancesConsumer));

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
        Title = "Attendances Api",
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

//IConsumer consumer = app.ApplicationServices.GetRequiredService<IConsumer>();
var consumer = app.Services.GetRequiredService<IConsumer>();
app.UseRabbitConsumer(consumer);

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Historical Attendance API v1"));
app.UseHttpsRedirection();

app.Run();
