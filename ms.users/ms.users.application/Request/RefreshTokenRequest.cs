namespace ms.users.application.Request
{
    /// <summary>
    /// Not marked as required: a missing token gets the same 401 as any other rejected one.
    /// </summary>
    public class RefreshTokenRequest
    {
        public string RefreshToken { get; set; }
    }
}
