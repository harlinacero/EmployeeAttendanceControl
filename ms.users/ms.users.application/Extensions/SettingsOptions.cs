using System.ComponentModel.DataAnnotations;

namespace ms.users.application.Extensions
{
    public sealed class SettingsOptions
    {
        [Required]
        public Authentication Authentication { get; set; }

        [Required]
        public Communication Communication { get; set; }
    }


    public class Authentication
    {
        [Required]
        public JWT JWT { get; set; }
    }

    public class JWT
    {
        [Required]
        public string Key { get; set; }

        [Required]
        public int ExpirationHours { get; set; }
    }

    public class Communication
    {
        [Required]
        public EventBus EventBus { get; set; }
    }

    public class EventBus
    {
        [Required]
        public string HostName { get; set; }
    }
}
