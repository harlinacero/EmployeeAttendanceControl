namespace ms.users.application.Security
{
    public interface IRefreshTokenGenerator
    {
        string Generate();

        string Hash(string refreshToken);
    }
}
