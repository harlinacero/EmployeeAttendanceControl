using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using ms.attendances.api.Consumers;
using ms.attendances.api.Extensions;
using ms.attendances.api.Mappers;
using ms.attendances.application.Commands.Handlers;
using ms.attendances.application.Extensions;
using ms.attendances.application.Mappers;
using ms.attendances.domain.Repositories;
using ms.attendances.infraestucture.Data;
using ms.attendances.infraestucture.Extensions;
using ms.attendances.infraestucture.Mappers;
using ms.attendances.infraestucture.Repositories;
using ms.rabbitmq.Consumers;
using ms.rabbitmq.Middlewares;
using System.Reflection;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddOptions<ConnectionStrings>().Bind(builder.Configuration.GetSection(ConnectionStrings.ConnectionStringsSection));
builder.Services.AddOptions<SettingsOptions>().Bind(builder.Configuration);

builder.Services.AddAttendanceDbContext();
builder.Services.AddConsummer();
builder.Services.AddAuthenticationSettings();
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

//IConsumer consumer = app.ApplicationServices.GetRequiredService<IConsumer>();
var consumer = app.Services.GetRequiredService<IConsumer>();
app.UseRabbitConsumer(consumer);

app.MapOpenApi();
app.UseSwaggerUI(c => c.SwaggerEndpoint("../swagger/v1/swagger.json", "Historical Attendance API v1"));
app.UseHttpsRedirection();

app.Run();
