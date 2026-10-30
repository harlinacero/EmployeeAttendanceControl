namespace ms.assistant.domain.Entities
{
    public enum ModelStopReason
    {
        EndTurn,
        ToolUse,
        MaxTokens,
        Refusal
    }
}
