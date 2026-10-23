using System.ComponentModel.DataAnnotations;

namespace ms.gateway.api.Extensions
{
    public sealed class SettingsOptions
    {
        [Required]
        public Authentication Authentication { get; set; } = null!;
    }

    public class Authentication
    {
        [Required]
        public JWT JWT { get; set; } = null!;
    }

    public class JWT
    {
        [Required]
        public string Key { get; set; } = null!;
    }
}
