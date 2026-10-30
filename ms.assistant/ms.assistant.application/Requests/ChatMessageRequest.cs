namespace ms.assistant.application.Requests
{
    public class ChatMessageRequest
    {
        public const string UserRole = "user";
        public const string AssistantRole = "assistant";

        public string Role { get; set; }
        public string Content { get; set; }
    }
}
