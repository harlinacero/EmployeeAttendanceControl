using ms.users.domain.Entities;

namespace ms.users.domain.Interfaces
{
    public interface ISessionRepository
    {
        Task CreateSession(UserSession session, RefreshToken token, TimeSpan timeToLive);
        Task<RefreshToken> GetToken(string tokenHash);
        Task<UserSession> GetSession(Guid sessionId);
        Task AddToken(RefreshToken token, DateTimeOffset renewedAt, TimeSpan timeToLive);

        /// <summary>
        /// Marks the token as spent only if nobody did before. False means it was already spent.
        /// </summary>
        Task<bool> TrySpendToken(string tokenHash, DateTimeOffset spentAt, TimeSpan timeToLive);

        Task EndSession(Guid sessionId, DateTimeOffset endedAt, string reason, TimeSpan timeToLive);
    }
}
