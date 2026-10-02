using ms.rabbitmq.Settings;
using System.ComponentModel.DataAnnotations;

namespace ms.employees.application.Extensions
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

        [Required]
        public External External { get; set; }
    }

    public class External
    {
        [Required]
        public string AttendanceApiUrl { get; set; }
    }

}
