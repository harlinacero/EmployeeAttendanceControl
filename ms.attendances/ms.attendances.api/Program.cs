using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using ms.attendances.api.Consumers;
using ms.attendances.api.Mappers;
using ms.attendances.application.Commands.Handlers;
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
builder.Services.AddTransient(typeof(IAttendanceContext), typeof(AttendanceMongoContext));
builder.Services.AddTransient(typeof(IAttendanceRepository), typeof(AttendanceRepository));


builder.Services.AddAutoMapper(mapperConfig =>
{
    mapperConfig.AddMaps(typeof(AttendanceProfile).Assembly);
    mapperConfig.AddMaps(typeof(AttendanceMongoProfile).Assembly);
    mapperConfig.AddProfile(typeof(EventMapperProfile));
});

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(CreateAttendanceCommandHandler).GetTypeInfo().Assembly);
});
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
            Title = "Attendances Api",
            Version = "v1",
            Description = "API para la gesti�n de asistencia de empleados."
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
            [schemeRef] = Array.Empty<string>().ToList()
        });

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

//IConsumer consumer = app.ApplicationServices.GetRequiredService<IConsumer>();
var consumer = app.Services.GetRequiredService<IConsumer>();
app.UseRabbitConsumer(consumer);

app.MapOpenApi();
app.UseSwaggerUI(c => c.SwaggerEndpoint("../swagger/v1/swagger.json", "Historical Attendance API v1"));
app.UseHttpsRedirection();

app.Run();
