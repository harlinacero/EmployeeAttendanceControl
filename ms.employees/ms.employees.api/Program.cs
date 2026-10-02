using ms.employees.api.Extensions;
using ms.employees.application.Extensions;
using ms.employees.infraestucture.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddDataBaseExtension();
builder.Services.AddComunicationsSettings();


// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddOptions<SettingsOptions>().Bind(builder.Configuration);
builder.Services.UseAuthenticationBearer();
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

app.MapOpenApi();
app.UseSwaggerUI(c => c.SwaggerEndpoint("../swagger/v1/swagger.json", "Employees Attendance API v1"));

app.UseHttpsRedirection();

app.Run();
