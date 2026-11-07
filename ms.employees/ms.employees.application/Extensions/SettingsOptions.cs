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
        /// <summary>
        /// Base URL of ms.users. Its public key is downloaded from there; this service can't issue tokens.
        /// </summary>
        [Required]
        public string Authority { get; set; }
    }


    public class Communication
    {
        [Required]
        public External External { get; set; }
    }

    public class External
    {
        [Required]
        public string AttendanceApiUrl { get; set; }

        [Required]
        public string UsersApiUrl { get; set; }
    }

}
