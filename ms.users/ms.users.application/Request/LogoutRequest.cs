using System.ComponentModel.DataAnnotations;

namespace ms.users.application.Request
{
    public class LogoutRequest
    {
        [Required]
        public string RefreshToken { get; set; }
    }
}
