using ms.gateway.api.Extensions;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Routes: ocelot.{Environment}.json when it exists (e.g. Local), otherwise ocelot.json (containers).
builder.Configuration.AddOcelotRoutes(builder.Environment);

builder.Services.AddOptions<SettingsOptions>().Bind(builder.Configuration);

builder.Services.AddGatewayAuthentication();
builder.Services.AddOcelot(builder.Configuration);

var app = builder.Build();

app.UseAuthentication();
await app.UseOcelot();

app.Run();
