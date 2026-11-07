namespace ms.users.domain.Entities
{
    public class User
    {
        public string UserName { get; set; }

        /// <summary>
        /// Protected form of the password (see IPasswordHasher). Never the readable password.
        /// </summary>
        public string PasswordHash { get; set; }

        public string Role { get; set; }
    }
}
