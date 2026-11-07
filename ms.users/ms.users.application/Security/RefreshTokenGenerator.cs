using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Text;

namespace ms.users.application.Security
{
    /// <summary>
    /// 256 random bits. A fast hash is enough to store them: unlike passwords, they can't be guessed.
    /// </summary>
    public class RefreshTokenGenerator : IRefreshTokenGenerator
    {
        private const int TokenSize = 32;

        public string Generate() => Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(TokenSize));

        public string Hash(string refreshToken) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
    }
}
