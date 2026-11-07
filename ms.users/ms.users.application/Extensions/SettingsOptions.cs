using System.ComponentModel.DataAnnotations;

namespace ms.users.application.Extensions
{
    public sealed class SettingsOptions
    {
        [Required]
        public Authentication Authentication { get; set; }
    }

    public class Authentication
    {
        [Required]
        public JWT JWT { get; set; }

        [Required]
        public SessionLimits Session { get; set; }
    }

    public class JWT
    {
        [Required]
        public string Issuer { get; set; }

        /// <summary>
        /// PEM file with the RSA private key. Generated on first start when missing.
        /// </summary>
        [Required]
        public string PrivateKeyPath { get; set; }

        [Required]
        public int AccessTokenMinutes { get; set; }
    }

    public class SessionLimits
    {
        [Required]
        public int IdleDays { get; set; }

        [Required]
        public int AbsoluteDays { get; set; }

        public TimeSpan Idle => TimeSpan.FromDays(IdleDays);

        public TimeSpan Absolute => TimeSpan.FromDays(AbsoluteDays);
    }
}
