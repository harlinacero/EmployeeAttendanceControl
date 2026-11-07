using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ms.users.application.Extensions;
using ms.users.domain.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace ms.users.application.Security
{
    public class JwtAccessTokenIssuer : IAccessTokenIssuer
    {
        private readonly ISigningKeyProvider _signingKeyProvider;
        private readonly JWT _settings;
        private readonly TimeProvider _timeProvider;

        public JwtAccessTokenIssuer(ISigningKeyProvider signingKeyProvider, IOptions<SettingsOptions> settings, TimeProvider timeProvider)
        {
            _signingKeyProvider = signingKeyProvider;
            _settings = settings.Value.Authentication.JWT;
            _timeProvider = timeProvider;
        }

        public int LifetimeSeconds => _settings.AccessTokenMinutes * 60;

        public string Issue(User user)
        {
            var now = _timeProvider.GetUtcNow().UtcDateTime;
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, user.UserName),
                    new Claim(ClaimTypes.Role, user.Role)
                ]),
                Issuer = _settings.Issuer,
                IssuedAt = now,
                NotBefore = now,
                Expires = now.AddMinutes(_settings.AccessTokenMinutes),
                SigningCredentials = new SigningCredentials(_signingKeyProvider.Key, SecurityAlgorithms.RsaSha256)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            return tokenHandler.WriteToken(tokenHandler.CreateToken(tokenDescriptor));
        }
    }
}
