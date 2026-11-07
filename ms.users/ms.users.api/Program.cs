using MediatR;
using ms.users.api.Extensions;
using ms.users.application.Commands;
using ms.users.application.Extensions;
using ms.users.application.Mappers;
using ms.users.application.Queries.Handlers;
using ms.users.application.Security;
using ms.users.domain.Interfaces;
using ms.users.infraestructure.Data;
using ms.users.infraestructure.Extensions;
using ms.users.infraestructure.Mappings;
using ms.users.infraestructure.Repositories;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

[assembly: ExcludeFromCodeCoverage]
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddSingleton(typeof(CassandraUserMapping));
builder.Services.AddSingleton(typeof(CassandraSessionMapping));

// One Cassandra connection for the whole service: the context connects on first use and is disposed on shutdown
builder.Services.AddSingleton(typeof(CassandraCluster));
builder.Services.AddSingleton(typeof(IUsersContext), typeof(UsersContext));
builder.Services.AddTransient(typeof(IUserRepository), typeof(UserRepository));
builder.Services.AddTransient(typeof(ISessionRepository), typeof(SessionRepository));

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
builder.Services.AddSingleton<ISigningKeyProvider, RsaSigningKeyProvider>();
builder.Services.AddSingleton<IAccessTokenIssuer, JwtAccessTokenIssuer>();
builder.Services.AddSingleton<IRefreshTokenGenerator, RefreshTokenGenerator>();

builder.Services.AddAutoMapper(mapperConfig => mapperConfig.AddMaps(typeof(UsersMapperProfile).Assembly));

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(GetAllUsersQueryHandler).GetTypeInfo().Assembly);
});

builder.Services.AddOptions<SettingsOptions>().Bind(builder.Configuration).ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddOptions<DatabaseSettings>().Bind(builder.Configuration.GetSection(nameof(DatabaseSettings)));
builder.Services.UseAuthenticationBearer();

builder.Services.AddExceptionHandler<DataUnavailableExceptionHandler>();
builder.Services.AddProblemDetails();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddSwaggerOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

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

app.MapOpenApi();
app.UseSwaggerUI(c => c.SwaggerEndpoint("../swagger/v1/swagger.json", "Users Authentication API v1"));
app.UseHttpsRedirection();

// No request is accepted while a readable password is stored. On failure the process ends, and the restart policy tries again
using (var scope = app.Services.CreateScope())
{
    try
    {
        await scope.ServiceProvider.GetRequiredService<IMediator>().Send(new ProtectStoredPasswordsCommand());
    }
    catch (Exception exception)
    {
        app.Logger.LogCritical(exception, "Stored passwords could not be protected. The service will not accept requests until they are");
        throw;
    }
}

app.Run();
