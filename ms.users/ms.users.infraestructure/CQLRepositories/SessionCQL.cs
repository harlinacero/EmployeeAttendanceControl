namespace ms.users.infraestructure.CQLRepositories
{
    /// <summary>
    /// Every write carries a time to live, so sessions and tokens disappear on their own after the session's absolute limit.
    /// </summary>
    public static class SessionCQL
    {
        public const string InsertSessionCql =
            "INSERT INTO user_session (session_id, session_username, session_created_at, session_last_renewed_at) VALUES (?, ?, ?, ?) USING TTL ?";

        public const string InsertTokenCql = "INSERT INTO refresh_token (token_hash, token_session_id) VALUES (?, ?) USING TTL ?";

        public const string RenewSessionCql = "UPDATE user_session USING TTL ? SET session_last_renewed_at = ? WHERE session_id = ?";

        // Lightweight transaction: of two concurrent uses of the same token, only one is applied.
        // The mapper's UpdateIf prepends "UPDATE refresh_token", so the statement starts after the table name
        public const string SpendTokenCql = "USING TTL ? SET token_spent_at = ? WHERE token_hash = ? IF token_spent_at = null";

        public const string EndSessionCql = "UPDATE user_session USING TTL ? SET session_ended_at = ?, session_end_reason = ? WHERE session_id = ?";

        public const string GetTokenCql = "SELECT * FROM refresh_token WHERE token_hash = ?";

        public const string GetSessionCql = "SELECT * FROM user_session WHERE session_id = ?";
    }
}
