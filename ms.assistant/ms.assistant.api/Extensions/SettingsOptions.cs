using System.ComponentModel.DataAnnotations;

namespace ms.assistant.api.Extensions
{
    public sealed class SettingsOptions
    {
        [Required]
        public Authentication Authentication { get; set; } = default!;

        [Required]
        public Communication Communication { get; set; } = default!;
    }

    public class Authentication
    {
        [Required]
        public JWT JWT { get; set; } = default!;
    }

    public class JWT
    {
        [Required]
        public string Key { get; set; } = default!;

        [Required]
        public int ExpirationHours { get; set; }
    }

    public class Communication
    {
        [Required]
        public External External { get; set; } = default!;
    }

    public class External
    {
        [Required]
        public string AttendanceApiUrl { get; set; } = default!;

        [Required]
        public string EmployeeApiUrl { get; set; } = default!;
    }
}
