namespace ms.assistant.domain.Entities
{
    /// <summary>
    /// One message of the conversation sent to the model: text, tool calls made by the model, or tool results sent back to it.
    /// </summary>
    public class ChatMessage
    {
        public ChatRole Role { get; init; }
        public string Text { get; init; }
        public IReadOnlyList<ToolCall> ToolCalls { get; init; } = [];
        public IReadOnlyList<ToolResult> ToolResults { get; init; } = [];

        /// <summary>
        /// The provider's raw content for an assistant message (e.g. thinking blocks). The model adapter sends it back unchanged.
        /// </summary>
        public object ProviderContent { get; init; }

        public static ChatMessage User(string text) => new() { Role = ChatRole.User, Text = text };

        public static ChatMessage Assistant(string text) => new() { Role = ChatRole.Assistant, Text = text };

        public static ChatMessage AssistantToolCalls(string text, IReadOnlyList<ToolCall> toolCalls, object providerContent) =>
            new() { Role = ChatRole.Assistant, Text = text, ToolCalls = toolCalls, ProviderContent = providerContent };

        public static ChatMessage Results(IReadOnlyList<ToolResult> toolResults) =>
            new() { Role = ChatRole.User, ToolResults = toolResults };
    }
}
