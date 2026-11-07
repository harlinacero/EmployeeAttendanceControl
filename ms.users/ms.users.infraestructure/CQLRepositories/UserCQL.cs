namespace ms.users.infraestructure.CQLRepositories
{
    public static class UserCQL
    {
        public const string GetUserByUserNameCql = "SELECT * FROM user WHERE user_username = ?";

        // Conditional: only replaces the value that was read, so a concurrent change is never overwritten.
        // The mapper's UpdateIf prepends "UPDATE user", so the statement starts after the table name
        public const string ProtectPasswordCql = "SET user_pasword = ? WHERE user_username = ? IF user_pasword = ?";
    }
}
