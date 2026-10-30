namespace ms.assistant.application.Tools
{
    /// <summary>
    /// Identity of the caller, taken from the JWT. Tools use it instead of anything the model writes.
    /// </summary>
    public record ToolContext(string UserName, string Token);
}
