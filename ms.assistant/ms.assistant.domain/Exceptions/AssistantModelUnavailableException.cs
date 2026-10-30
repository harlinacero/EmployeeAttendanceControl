namespace ms.assistant.domain.Exceptions
{
    public class AssistantModelUnavailableException : Exception
    {
        public AssistantModelUnavailableException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
