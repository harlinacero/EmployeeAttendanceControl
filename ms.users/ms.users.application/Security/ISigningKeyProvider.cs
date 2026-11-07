using Microsoft.IdentityModel.Tokens;

namespace ms.users.application.Security
{
    /// <summary>
    /// The RSA key that signs access tokens. Only ms.users has it; other services get the public part from the JWKS.
    /// </summary>
    public interface ISigningKeyProvider
    {
        RsaSecurityKey Key { get; }
    }
}
