using ms.assistant.domain.Entities;
using System.Text.Json;

namespace ms.assistant.application.Tools
{
    public interface IAssistantTool
    {
        string Name { get; }
        string Description { get; }
        IReadOnlyDictionary<string, JsonElement> InputSchema { get; }
        IReadOnlyList<string> Required { get; }
        bool AdminOnly { get; }

        Task<ToolResult> ExecuteAsync(JsonElement input, ToolContext context, CancellationToken cancellationToken);
    }
}
