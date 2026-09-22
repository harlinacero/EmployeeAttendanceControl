namespace ms.users.infraestructure.CQLRepositories
{
    public static class UserCQL
    {
        public const string GetUserByUserNameCql = "SELECT * FROM user WHERE user_username = ?";
    }
}
