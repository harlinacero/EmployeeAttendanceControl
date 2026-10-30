namespace ms.assistant.domain.Entities
{
    public record ModelReply(
        ModelStopReason StopReason,
        string Text,
        IReadOnlyList<ToolCall> ToolCalls,
        ChatMessage AssistantMessage,
        int InputTokens,
        int OutputTokens);
}
