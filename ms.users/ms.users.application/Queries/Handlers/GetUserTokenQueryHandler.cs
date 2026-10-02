using MediatR;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ms.users.application.Extensions;
using ms.users.domain.Entities;
using ms.users.domain.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ms.users.application.Queries.Handlers
{
    public class GetUserTokenQueryHandler : IRequestHandler<GetUserTokenQuery, string>
    {
        private readonly IUserRepository _userRepository;
        private readonly SettingsOptions _configuration;

        public GetUserTokenQueryHandler(IUserRepository userRepository, IOptions<SettingsOptions> configuration)
        {
            _userRepository = userRepository;
            _configuration = configuration.Value;
        }

        public async Task<string> Handle(GetUserTokenQuery request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetUser(request.UserName, request.Password);
            return CreateToken(user);
        }

        private string CreateToken(User user)
        {
            var claims = new List<Claim>()
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserName.ToString()),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var privateKey = _configuration.Authentication.JWT.Key;
            var symetricSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(privateKey));
            var signingCredentials = new SigningCredentials(symetricSigningKey, SecurityAlgorithms.HmacSha256Signature);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(_configuration.Authentication.JWT.ExpirationHours),
                SigningCredentials = signingCredentials
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}
