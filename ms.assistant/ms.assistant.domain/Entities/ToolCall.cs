using System.Text.Json;

namespace ms.assistant.domain.Entities
{
    /// <summary>
    /// A request from the model to run one tool with the given input.
    /// </summary>
    public record ToolCall(string Id, string Name, JsonElement Input);
}
