using System.ComponentModel.DataAnnotations;

namespace ms.users.application.Request
{
    public class LoginCredentialsRequest
    {
        [Required]
        public string UserName { get; set; }
        [Required]
        public string Password { get; set; }
    }
}
