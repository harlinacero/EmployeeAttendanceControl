using System.ComponentModel.DataAnnotations;

namespace ms.assistant.application.Requests
{
    public class ChatRequest : IValidatableObject
    {
        public const int MaxQuestionLength = 2000;
        public const int MaxHistoryMessages = 20;

        [Required]
        [MaxLength(MaxQuestionLength)]
        public string Question { get; set; }

        public List<ChatMessageRequest> History { get; set; } = [];

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var history = History ?? [];

            if (history.Count > MaxHistoryMessages)
            {
                yield return new ValidationResult($"The conversation cannot have more than {MaxHistoryMessages} previous messages.", [nameof(History)]);
            }

            if (history.Any(message => message is null
                || (message.Role != ChatMessageRequest.UserRole && message.Role != ChatMessageRequest.AssistantRole)))
            {
                yield return new ValidationResult($"Each previous message must have the role '{ChatMessageRequest.UserRole}' or '{ChatMessageRequest.AssistantRole}'.", [nameof(History)]);
            }

            if (history.Any(message => message is not null && string.IsNullOrWhiteSpace(message.Content)))
            {
                yield return new ValidationResult("Previous messages cannot be empty.", [nameof(History)]);
            }
        }
    }
}
