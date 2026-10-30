using System.Text.Json;

namespace ms.assistant.domain.Entities
{
    public record ToolDefinition(string Name, string Description, IReadOnlyDictionary<string, JsonElement> InputSchema, IReadOnlyList<string> Required);
}
