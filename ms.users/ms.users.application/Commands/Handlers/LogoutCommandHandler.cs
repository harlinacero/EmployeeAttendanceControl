using MediatR;
using Microsoft.Extensions.Options;
using ms.users.application.Extensions;
using ms.users.application.Security;
using ms.users.domain.Interfaces;

namespace ms.users.application.Commands.Handlers
{
    /// <summary>
    /// Ends the session of the presented refresh token, spent or not. An unknown token or an ended session changes nothing.
    /// </summary>
    public class LogoutCommandHandler : IRequestHandler<LogoutCommand>
    {
        public const string LogoutReason = "logout";

        private readonly ISessionRepository _sessionRepository;
        private readonly IRefreshTokenGenerator _refreshTokenGenerator;
        private readonly TimeProvider _timeProvider;
        private readonly SessionLimits _limits;

        public LogoutCommandHandler(ISessionRepository sessionRepository, IRefreshTokenGenerator refreshTokenGenerator, TimeProvider timeProvider, IOptions<SettingsOptions> settings)
        {
            _sessionRepository = sessionRepository;
            _refreshTokenGenerator = refreshTokenGenerator;
            _timeProvider = timeProvider;
            _limits = settings.Value.Authentication.Session;
        }

        public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
        {
            var token = await _sessionRepository.GetToken(_refreshTokenGenerator.Hash(request.RefreshToken));
            var session = token == null ? null : await _sessionRepository.GetSession(token.SessionId);
            if (session == null || session.EndedAt != null)
            {
                return;
            }

            var now = _timeProvider.GetUtcNow();
            await _sessionRepository.EndSession(session.SessionId, now, LogoutReason, session.RemainingLifetime(now, _limits.Absolute));
        }
    }
}
