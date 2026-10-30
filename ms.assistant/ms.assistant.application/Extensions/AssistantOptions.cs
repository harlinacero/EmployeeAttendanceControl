using System.ComponentModel.DataAnnotations;

namespace ms.assistant.application.Extensions
{
    public class AssistantOptions
    {
        public const string Section = "Assistant";

        [Range(1, 20)]
        public int MaxToolRounds { get; set; } = 5;
    }
}
