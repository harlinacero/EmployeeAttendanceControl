using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using ms.rabbitmq.Consumers;
using ms.rabbitmq.Middlewares;
using ms.users.api.Consumers;
using ms.users.api.Extensions;
using ms.users.api.Mappers;
using ms.users.application.Extensions;
using ms.users.application.Mappers;
using ms.users.application.Queries.Handlers;
using ms.users.domain.Interfaces;
using ms.users.infraestructure.Data;
using ms.users.infraestructure.Extensions;
using ms.users.infraestructure.Mappings;
using ms.users.infraestructure.Repositories;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;

[assembly: ExcludeFromCodeCoverage]
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


builder.Services.AddOptions<SettingsOptions>().Bind(builder.Configuration);
builder.Services.AddOptions<DatabaseSettings>().Bind(builder.Configuration.GetSection("DatabaseSettings"));
builder.Services.UseAuthenticationBearer();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddSwaggerOpenApi();

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
