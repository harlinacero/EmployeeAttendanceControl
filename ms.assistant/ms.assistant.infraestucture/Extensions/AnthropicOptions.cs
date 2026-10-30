using System.ComponentModel.DataAnnotations;

namespace ms.assistant.infraestucture.Extensions
{
    public class AnthropicOptions
    {
        public const string Section = "Anthropic";

        [Required]
        public string Model { get; set; } = "claude-opus-5-5";

        [Required]
        public string Effort { get; set; } = "low";

        [Range(256, 64000)]
        public int MaxTokens { get; set; } = 4096;

        [Range(5, 600)]
        public int TimeoutSeconds { get; set; } = 60;

        [Required]
        public string ApiKey { get; set; }
    }
}
