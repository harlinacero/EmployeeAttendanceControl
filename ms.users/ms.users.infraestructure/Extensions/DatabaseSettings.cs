using System.ComponentModel.DataAnnotations;

namespace ms.users.infraestructure.Extensions
{
    public class DatabaseSettings
    {
        [Required]
        public string Hostname { get; set; }
        [Required]
        public int Port { get; set; }
        [Required]
        public string Keyspace { get; set; }
    }
}
