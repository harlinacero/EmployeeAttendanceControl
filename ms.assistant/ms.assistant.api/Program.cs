using ms.assistant.api.Extensions;
using ms.assistant.application.Extensions;
using ms.assistant.infraestucture.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddOptions<SettingsOptions>().Bind(builder.Configuration);
builder.Services.AddOptions<AssistantOptions>().Bind(builder.Configuration.GetSection(AssistantOptions.Section)).ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddOptions<AnthropicOptions>().Bind(builder.Configuration.GetSection(AnthropicOptions.Section)).ValidateDataAnnotations().ValidateOnStart();

builder.Services.AddAuthenticationSettings();
builder.Services.AddAssistant();
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
app.UseSwaggerUI(c => c.SwaggerEndpoint("../swagger/v1/swagger.json", "Assistant API v1"));
app.UseHttpsRedirection();

app.Run();
