using Cassandra.Mapping;
using ms.users.domain.Entities;

namespace ms.users.infraestructure.Mappings
{
    public class CassandraSessionMapping : Cassandra.Mapping.Mappings
    {
        public CassandraSessionMapping()
        {
            For<UserSession>().TableName("user_session").PartitionKey(s => s.SessionId)
                .Column(s => s.SessionId, cc => cc.WithName("session_id"))
                .Column(s => s.UserName, cc => cc.WithName("session_username"))
                .Column(s => s.CreatedAt, cc => cc.WithName("session_created_at"))
                .Column(s => s.LastRenewedAt, cc => cc.WithName("session_last_renewed_at"))
                .Column(s => s.EndedAt, cc => cc.WithName("session_ended_at"))
                .Column(s => s.EndReason, cc => cc.WithName("session_end_reason"));

            For<RefreshToken>().TableName("refresh_token").PartitionKey(t => t.TokenHash)
                .Column(t => t.TokenHash, cc => cc.WithName("token_hash"))
                .Column(t => t.SessionId, cc => cc.WithName("token_session_id"))
                .Column(t => t.SpentAt, cc => cc.WithName("token_spent_at"));
        }
    }
}
