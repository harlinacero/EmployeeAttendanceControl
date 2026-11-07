using MediatR;
using Microsoft.Extensions.Options;
using ms.users.application.Extensions;
using ms.users.application.Responses;
using ms.users.application.Security;
using ms.users.domain.Entities;
using ms.users.domain.Exceptions;
using ms.users.domain.Interfaces;

namespace ms.users.application.Commands.Handlers
{
    public class LoginCommandHandler : IRequestHandler<LoginCommand, TokenResponse>
    {
        // Verified when the user doesn't exist, so the response time doesn't reveal which accounts exist
        private const string UnknownUserHash = "pbkdf2-sha256$600000$AAAAAAAAAAAAAAAAAAAAAA==$AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=";

        private readonly IUserRepository _userRepository;
        private readonly ISessionRepository _sessionRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IAccessTokenIssuer _accessTokenIssuer;
        private readonly IRefreshTokenGenerator _refreshTokenGenerator;
        private readonly TimeProvider _timeProvider;
        private readonly SessionLimits _limits;

        public LoginCommandHandler(IUserRepository userRepository, ISessionRepository sessionRepository, IPasswordHasher passwordHasher,
            IAccessTokenIssuer accessTokenIssuer, IRefreshTokenGenerator refreshTokenGenerator, TimeProvider timeProvider, IOptions<SettingsOptions> settings)
        {
            _userRepository = userRepository;
            _sessionRepository = sessionRepository;
            _passwordHasher = passwordHasher;
            _accessTokenIssuer = accessTokenIssuer;
            _refreshTokenGenerator = refreshTokenGenerator;
            _timeProvider = timeProvider;
            _limits = settings.Value.Authentication.Session;
        }

        public async Task<TokenResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.UserName) || string.IsNullOrEmpty(request.Password))
            {
                throw new InvalidCredentialsException();
            }

            var user = await _userRepository.GetUserByUserName(request.UserName);
            var passwordMatches = _passwordHasher.Verify(request.Password, user?.PasswordHash ?? UnknownUserHash);
            if (user == null || !passwordMatches)
            {
                throw new InvalidCredentialsException();
            }

            var now = _timeProvider.GetUtcNow();
            var session = new UserSession { SessionId = Guid.NewGuid(), UserName = user.UserName, CreatedAt = now, LastRenewedAt = now };
            var refreshToken = _refreshTokenGenerator.Generate();
            await _sessionRepository.CreateSession(
                session,
                new RefreshToken { TokenHash = _refreshTokenGenerator.Hash(refreshToken), SessionId = session.SessionId },
                _limits.Absolute);

            return new TokenResponse(_accessTokenIssuer.Issue(user), refreshToken, _accessTokenIssuer.LifetimeSeconds);
        }
    }
}
