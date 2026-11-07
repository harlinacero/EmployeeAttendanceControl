namespace ms.users.application.Responses
{
    /// <param name="ExpiresIn">Lifetime of the access token, in seconds.</param>
    public record TokenResponse(string AccessToken, string RefreshToken, int ExpiresIn);
}
