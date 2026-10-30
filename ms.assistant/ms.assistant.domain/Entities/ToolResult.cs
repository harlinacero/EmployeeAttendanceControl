namespace ms.assistant.domain.Entities
{
    public record ToolResult(string ToolCallId, string Content, bool IsError)
    {
        public static ToolResult Ok(string content) => new(null, content, false);

        public static ToolResult Error(string message) => new(null, message, true);
    }
}
