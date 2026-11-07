using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ms.users.application.Extensions;
using ms.users.application.Responses;
using ms.users.application.Security;
using ms.users.domain.Entities;
using ms.users.domain.Exceptions;
using ms.users.domain.Interfaces;

namespace ms.users.application.Commands.Handlers
{
    /// <summary>
    /// Rotates a refresh token. Spending the presented token is the last step, so any earlier failure leaves it usable.
    /// A token presented after being spent means it was stolen: the whole session is revoked.
    /// </summary>
    public class RefreshSessionCommandHandler : IRequestHandler<RefreshSessionCommand, TokenResponse>
    {
        public const string ReuseReason = "reuse";
        public const string AccountMissingReason = "account-missing";

        private readonly IUserRepository _userRepository;
        private readonly ISessionRepository _sessionRepository;
        private readonly IAccessTokenIssuer _accessTokenIssuer;
        private readonly IRefreshTokenGenerator _refreshTokenGenerator;
        private readonly TimeProvider _timeProvider;
        private readonly SessionLimits _limits;
        private readonly ILogger<RefreshSessionCommandHandler> _logger;

        public RefreshSessionCommandHandler(IUserRepository userRepository, ISessionRepository sessionRepository, IAccessTokenIssuer accessTokenIssuer,
            IRefreshTokenGenerator refreshTokenGenerator, TimeProvider timeProvider, IOptions<SettingsOptions> settings, ILogger<RefreshSessionCommandHandler> logger)
        {
            _userRepository = userRepository;
            _sessionRepository = sessionRepository;
            _accessTokenIssuer = accessTokenIssuer;
            _refreshTokenGenerator = refreshTokenGenerator;
            _timeProvider = timeProvider;
            _limits = settings.Value.Authentication.Session;
            _logger = logger;
        }

        public async Task<TokenResponse> Handle(RefreshSessionCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.RefreshToken))
            {
                throw new InvalidCredentialsException();
            }

            var presentedHash = _refreshTokenGenerator.Hash(request.RefreshToken);
            var presented = await _sessionRepository.GetToken(presentedHash) ?? throw new InvalidCredentialsException();
            var session = await _sessionRepository.GetSession(presented.SessionId) ?? throw new InvalidCredentialsException();
            var now = _timeProvider.GetUtcNow();

            if (presented.SpentAt != null)
            {
                await RevokeForReuse(session, now);
            }

            if (!session.IsActive(now, _limits.Idle, _limits.Absolute))
            {
                throw new InvalidCredentialsException();
            }

            var user = await _userRepository.GetUserByUserName(session.UserName);
            if (user == null)
            {
                await _sessionRepository.EndSession(session.SessionId, now, AccountMissingReason, session.RemainingLifetime(now, _limits.Absolute));
                throw new InvalidCredentialsException();
            }

            var newToken = _refreshTokenGenerator.Generate();
            var remaining = session.RemainingLifetime(now, _limits.Absolute);
            await _sessionRepository.AddToken(new RefreshToken { TokenHash = _refreshTokenGenerator.Hash(newToken), SessionId = session.SessionId }, now, remaining);

            if (!await _sessionRepository.TrySpendToken(presentedHash, now, remaining))
            {
                await RevokeForReuse(session, now);
            }

            return new TokenResponse(_accessTokenIssuer.Issue(user), newToken, _accessTokenIssuer.LifetimeSeconds);
        }

        private async Task RevokeForReuse(UserSession session, DateTimeOffset now)
        {
            if (session.EndedAt == null)
            {
                await _sessionRepository.EndSession(session.SessionId, now, ReuseReason, session.RemainingLifetime(now, _limits.Absolute));
                _logger.LogWarning("Refresh token reused: session {SessionId} of user {UserName} was revoked", session.SessionId, session.UserName);
            }

            throw new InvalidCredentialsException();
        }
    }
}
