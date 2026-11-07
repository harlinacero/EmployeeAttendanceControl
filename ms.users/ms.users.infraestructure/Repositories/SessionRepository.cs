using Cassandra.Mapping;
using ms.users.domain.Entities;
using ms.users.domain.Interfaces;
using ms.users.infraestructure.CQLRepositories;
using ms.users.infraestructure.Data;

namespace ms.users.infraestructure.Repositories
{
    public class SessionRepository : ISessionRepository
    {
        private readonly IUsersContext _usersContext;

        public SessionRepository(IUsersContext usersContext) => _usersContext = usersContext;

        public Task CreateSession(UserSession session, RefreshToken token, TimeSpan timeToLive) => CassandraCall.Run(async mapper =>
        {
            await mapper.ExecuteAsync(Cql.New(SessionCQL.InsertSessionCql,
                session.SessionId, session.UserName, session.CreatedAt, session.LastRenewedAt, Seconds(timeToLive)));
            await mapper.ExecuteAsync(Cql.New(SessionCQL.InsertTokenCql, token.TokenHash, token.SessionId, Seconds(timeToLive)));
        }, _usersContext);

        public Task<RefreshToken> GetToken(string tokenHash) =>
            CassandraCall.Run(mapper => mapper.FirstOrDefaultAsync<RefreshToken>(SessionCQL.GetTokenCql, tokenHash), _usersContext);

        public Task<UserSession> GetSession(Guid sessionId) =>
            CassandraCall.Run(mapper => mapper.FirstOrDefaultAsync<UserSession>(SessionCQL.GetSessionCql, sessionId), _usersContext);

        public Task AddToken(RefreshToken token, DateTimeOffset renewedAt, TimeSpan timeToLive) => CassandraCall.Run(async mapper =>
        {
            await mapper.ExecuteAsync(Cql.New(SessionCQL.InsertTokenCql, token.TokenHash, token.SessionId, Seconds(timeToLive)));
            await mapper.ExecuteAsync(Cql.New(SessionCQL.RenewSessionCql, Seconds(timeToLive), renewedAt, token.SessionId));
        }, _usersContext);

        public Task<bool> TrySpendToken(string tokenHash, DateTimeOffset spentAt, TimeSpan timeToLive) => CassandraCall.Run(async mapper =>
        {
            var appliedInfo = await mapper.UpdateIfAsync<RefreshToken>(Cql.New(SessionCQL.SpendTokenCql, Seconds(timeToLive), spentAt, tokenHash));
            return appliedInfo.Applied;
        }, _usersContext);

        public Task EndSession(Guid sessionId, DateTimeOffset endedAt, string reason, TimeSpan timeToLive) =>
            CassandraCall.Run(mapper => mapper.ExecuteAsync(Cql.New(SessionCQL.EndSessionCql, Seconds(timeToLive), endedAt, reason, sessionId)), _usersContext);

        private static int Seconds(TimeSpan timeToLive) => (int)timeToLive.TotalSeconds;
    }
}
