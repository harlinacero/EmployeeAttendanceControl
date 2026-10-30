using Microsoft.Extensions.Logging;
using ms.assistant.domain.Entities;
using Refit;
using System.Text.Json;

namespace ms.assistant.application.Tools
{
    /// <summary>
    /// Shared behavior for tools that read data from other services: serialize the data as JSON for the model,
    /// and turn a service failure into an error result the model can explain, instead of an exception.
    /// </summary>
    public abstract class AssistantToolBase<TTool> : IAssistantTool
    {
        public const string UnavailableMessage = "The information could not be retrieved right now.";

        protected static readonly IReadOnlyDictionary<string, JsonElement> NoInputs = new Dictionary<string, JsonElement>();
        private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

        private readonly ILogger<TTool> _logger;

        protected AssistantToolBase(ILogger<TTool> logger) => _logger = logger;

        public abstract string Name { get; }
        public abstract string Description { get; }
        public abstract IReadOnlyDictionary<string, JsonElement> InputSchema { get; }
        public virtual IReadOnlyList<string> Required => [];
        public abstract bool AdminOnly { get; }

        public abstract Task<ToolResult> ExecuteAsync(JsonElement input, ToolContext context, CancellationToken cancellationToken);

        protected async Task<ToolResult> RetrieveAsync<T>(Func<Task<T>> retrieve, ToolContext context)
        {
            try
            {
                return ToolResult.Ok(JsonSerializer.Serialize(await retrieve(), SerializerOptions));
            }
            catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                _logger.LogWarning("Tool {Tool} input rejected by the service for {UserName}", Name, context.UserName);
                return ToolResult.Error($"The request was rejected as invalid: {ex.Content}");
            }
            catch (Exception ex) when (ex is ApiException or HttpRequestException or TaskCanceledException)
            {
                _logger.LogError(ex, "Tool {Tool} failed for {UserName}", Name, context.UserName);
                return ToolResult.Error(UnavailableMessage);
            }
        }

        protected static bool TryGetString(JsonElement input, string name, out string value)
        {
            value = null;
            if (input.ValueKind == JsonValueKind.Object
                && input.TryGetProperty(name, out var property)
                && property.ValueKind == JsonValueKind.String
                && !string.IsNullOrWhiteSpace(property.GetString()))
            {
                value = property.GetString();
                return true;
            }
            return false;
        }

        protected static JsonElement Property(string type, string description)
            => JsonSerializer.SerializeToElement(new { type, description });
    }
}
