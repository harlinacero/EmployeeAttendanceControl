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
        /// <summary>
        /// Base URL of ms.users. Its public key is downloaded from there; this service can't issue tokens.
        /// </summary>
        [Required]
        public string Authority { get; set; } = null!;
    }
}
