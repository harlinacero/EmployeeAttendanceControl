using ms.assistant.domain.Entities;

namespace ms.assistant.domain.Services
{
    /// <summary>
    /// Port to the AI model. One call is one model turn: it either answers or asks for tool calls.
    /// </summary>
    public interface IAssistantModel
    {
        Task<ModelReply> SendAsync(string systemPrompt, IReadOnlyList<ChatMessage> messages, IReadOnlyList<ToolDefinition> tools, CancellationToken cancellationToken);
    }
}
