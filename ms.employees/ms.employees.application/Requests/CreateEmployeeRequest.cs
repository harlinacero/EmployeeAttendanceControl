using System.ComponentModel.DataAnnotations;

namespace ms.employees.application.Requests
{
    public class CreateEmployeeRequest
    {
        [Required]
        public string UserName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }

        [Required]
        public string Password { get; set; }
        [Required]
        public string Role { get; set; }
    }
}
